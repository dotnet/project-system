// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate;

[Export(typeof(ICopyItemAggregator))]
[AppliesTo(BuildUpToDateCheck.AppliesToExpression)]
internal class CopyItemAggregator : ICopyItemAggregator
{
    private readonly Dictionary<string, ProjectCopyData> _projectData = new(StringComparers.Paths);

    public void SetProjectData(ProjectCopyData projectCopyData)
    {
        System.Diagnostics.Debug.Assert(Path.IsPathRooted(projectCopyData.TargetPath));
        Requires.Argument(!projectCopyData.IsDefault, nameof(projectCopyData), "Must not be default.");

        lock (_projectData)
        {
            // TODO remove this debug output, or log elsewhere
            Trace.WriteLine($"*********************** CopyItem data for {projectCopyData.TargetPath}:");
            foreach (CopyItem copyItem in projectCopyData.CopyItems)
            {
                Trace.WriteLine($"  {copyItem.AbsoluteSourcePath} -> {copyItem.RelativeTargetPath} ({copyItem.CopyType})");
            }
            foreach (string referencePath in projectCopyData.ReferencedProjectTargetPaths)
            {
                Trace.WriteLine($"  References: {referencePath}");
            }

            _projectData[projectCopyData.TargetPath] = projectCopyData;
        }
    }

    public (IEnumerable<CopyItem> Items, bool IsComplete) TryGatherCopyItemsForProject(string targetPath, BuildUpToDateCheck.Log logger)
    {
        // Keep track of all projects we've visited to avoid infinite recursion or duplicated results.
        HashSet<string> explored = new(StringComparers.Paths);

        // The queue of projects yet to be visited.
        Queue<string> frontier = new();
        frontier.Enqueue(targetPath);

        // If we find a reference to a project that did not call SetProjectData then we will not know the
        // full set of items to copy. Without this information, we cannot safely accelerate the build and
        // will still schedule the build.
        //
        // Note that incomplete data is still useful, and the fast up-to-date check can check those items
        // for copies, which will trigger builds that would otherwise have been skipped. Using incomplete
        // results is strictly an improvement over ignoring what results we do have.
        bool isComplete = true;

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
                    logger.Verbose(nameof(Resources.FUTDC_AccelerationDataMissingForProject_1), project);
                    isComplete = false;
                    continue;
                }

                foreach (string referencedProjectTargetPath in data.ReferencedProjectTargetPaths)
                {
                    frontier.Enqueue(referencedProjectTargetPath);
                }

                if (!data.CopyItems.IsEmpty)
                {
                    contributingProjects ??= new();
                    contributingProjects.Add(data);
                }
            }
        }

        return (GenerateCopyItems(), isComplete);

        IEnumerable<CopyItem> GenerateCopyItems()
        {
            if (contributingProjects is null)
            {
                yield break;
            }

            var set = new HashSet<CopyItem>();

            foreach (ProjectCopyData contributingProject in contributingProjects)
            {
                logger.Verbose(nameof(Resources.FUTDC_CheckingCopyItemsForProject_1), contributingProject.ProjectFullPath ?? contributingProject.TargetPath);
                logger.Indent++;

                foreach (CopyItem copyItem in contributingProject.CopyItems)
                {
                    if (!set.Add(copyItem))
                    {
                        logger.Verbose(nameof(Resources.FUTDC_SkippingDuplicateCopyItem_2), copyItem.AbsoluteSourcePath, copyItem.RelativeTargetPath);
                        continue;
                    }

                    yield return copyItem;
                }

                logger.Indent--;
            }
        }
    }
}
