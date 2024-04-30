// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.UpToDate;

[Export(typeof(ICopyItemAggregator))]
[AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
internal class CopyItemAggregator : ICopyItemAggregator
{
    private readonly Dictionary<string, ProjectCopyData> _projectData = new(StringComparers.Paths);

    public void SetProjectData(ProjectCopyData projectCopyData)
    {
        Requires.Argument(!projectCopyData.IsDefault, nameof(projectCopyData), "Must not be default.");

        lock (_projectData)
        {
            _projectData[projectCopyData.TargetPath] = projectCopyData;
        }
    }

    public CopyItemsResult TryGatherCopyItemsForProject(string targetPath, BuildUpToDateCheck.Log logger)
    {
        // Keep track of all projects we've visited to avoid infinite recursion or duplicated results.
        HashSet<string> explored = new(StringComparers.Paths);

        // The queue of projects yet to be visited.
        Queue<string> frontier = new();

        // Start with the requested project.
        frontier.Enqueue(targetPath);

        // If we find a reference to a project that did not call SetProjectData then we will not know the
        // full set of items to copy. Without this information, we cannot safely accelerate the build and
        // will still schedule the build.
        //
        // Note that incomplete data is still useful, and the fast up-to-date check can check those items
        // for copies, which will trigger builds that would otherwise have been skipped. Using incomplete
        // results is strictly an improvement over ignoring what results we do have.
        bool isComplete = true;

        // Lazily populated list of referenced projects not having ProduceReferenceAssembly set to true.
        // The originating project is not included in this check (targetPath).
        List<string>? referencesNotProducingReferenceAssembly = null;

        List<ProjectCopyData>? contributingProjects = null;

        // We must capture the results at this moment of time within the lock, to prevent corrupting collections.
        lock (_projectData)
        {
            while (frontier.Count != 0)
            {
                string project = frontier.Dequeue();

                if (!explored.Add(project))
                {
                    // Already visited this project.
                    continue;
                }

                if (!_projectData.TryGetValue(project, out ProjectCopyData data))
                {
                    // We don't have a list of project references for this project, so we cannot continue
                    // walking the project reference tree. As we might not know all possible copy items,
                    // we disable build acceleration. Note that we still walk the rest of the tree in order
                    // to detect copy items, so that we can decide whether the project is up to date.
                    logger.Verbose(nameof(VSResources.FUTDC_AccelerationDataMissingForProject_1), project);
                    isComplete = false;
                    continue;
                }

                if (!data.ProduceReferenceAssembly && project != targetPath)
                {
                    // One of the referenced projects does not produce a reference assembly.
                    referencesNotProducingReferenceAssembly ??= [];
                    referencesNotProducingReferenceAssembly.Add(data.TargetPath);
                }

                // Breadth-first traversal of the tree.
                foreach (string referencedProjectTargetPath in data.ReferencedProjectTargetPaths)
                {
                    frontier.Enqueue(referencedProjectTargetPath);
                }

                if (!data.CopyItems.IsEmpty)
                {
                    contributingProjects ??= [];
                    contributingProjects.Add(data);
                }
            }
        }

        return new(isComplete, GenerateCopyItems(), GetDuplicateCopyItems(), referencesNotProducingReferenceAssembly);

        IEnumerable<(string Path, ImmutableArray<CopyItem> CopyItems)> GenerateCopyItems()
        {
            if (contributingProjects is null)
            {
                yield break;
            }

            foreach (ProjectCopyData contributingProject in contributingProjects)
            {
                yield return (contributingProject.ProjectFullPath ?? contributingProject.TargetPath, contributingProject.CopyItems);
            }
        }

        IReadOnlyList<string>? GetDuplicateCopyItems()
        {
            HashSet<string>? duplicates = null;

            if (contributingProjects is not null)
            {
                HashSet<string> targetPaths = new(StringComparers.Paths);

                foreach (ProjectCopyData contributingProject in contributingProjects)
                {
                    foreach (CopyItem copyItem in contributingProject.CopyItems)
                    {
                        if (!targetPaths.Add(copyItem.RelativeTargetPath))
                        {
                            duplicates ??= new(StringComparers.Paths);
                            duplicates.Add(copyItem.RelativeTargetPath);
                        }
                    }
                }
            }

            return duplicates?.ToList();
        }
    }
}
