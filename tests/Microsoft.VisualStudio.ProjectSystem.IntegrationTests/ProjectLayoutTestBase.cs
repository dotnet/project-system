// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Text;
using Microsoft.Test.Apex.VisualStudio.Solution;
using Microsoft.Test.Apex.VisualStudio.Shell.ToolWindows;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Base class for integration tests that start with a specific solution/project layout on disk.
    /// </summary>
    public abstract class ProjectLayoutTestBase : IntegrationTestBase
    {
        /// <summary>
        /// Paths of temporary workspaces to delete when the test fixture completes.
        /// Each path is a root, so should be deleted recursively.
        /// </summary>
        private ImmutableList<string> _rootPaths = ImmutableList<string>.Empty;

        /// <summary>
        /// Verifies that the "Dependencies" node of <paramref name="project"/> has a structure that
        /// matches <paramref name="nodes"/>.
        /// </summary>
        /// <param name="project">The project whose Dependencies node should be inspected.</param>
        /// <param name="nodes">The expected structure for the Dependencies node.</param>
        protected void VerifyDependenciesNode(Project project, params Node[] nodes)
        {
            using (Scope.Enter("Verify dependency nodes"))
            {
                var item = VisualStudio.ObjectModel.Solution.SolutionExplorer.FindItemRecursive(project.ProjectName, expandToFind: true);

                item.Expand();

                var actualDependencies = item.Items.FirstOrDefault(i => i.Name == "Dependencies");

                VerifyDependenciesNode(actualDependencies, nodes);
            }
        }

        /// <summary>
        /// Verifies that the solution's first "Dependencies" node has a structure that
        /// matches <paramref name="nodes"/>.
        /// </summary>
        /// <param name="nodes">The expected structure for the Dependencies node.</param>
        protected void VerifyDependenciesNode(params Node[] nodes)
        {
            using (Scope.Enter("Verify dependency nodes"))
            {
                SolutionExplorerItemTestExtension actualDependencies = VisualStudio.ObjectModel.Solution.SolutionExplorer.FindItemRecursive("Dependencies", expandToFind: true);

                VerifyDependenciesNode(actualDependencies, nodes);
            }
        }

        private static void VerifyDependenciesNode(SolutionExplorerItemTestExtension actualDependencies, Node[] nodes)
        {
            var expectDependencies = new Node("Dependencies", KnownMonikers.ReferenceGroup)
            {
                Children = new List<Node>(nodes)
            };

            var expectOutput = new StringBuilder();
            var actualOutput = new StringBuilder();

            var same = true;

            VerifyNode(expectDependencies, actualDependencies);

            if (!same)
            {
                Assert.Fail($"Incorrect Dependencies tree.\n\nExpected:\n\n{expectOutput}\nActual:\n\n{actualOutput}");
            }

            return;

            void VerifyNode(Node? expect, SolutionExplorerItemTestExtension? actual, int depth = 0)
            {
                Assert.IsTrue(expect is not null || actual is not null);

                var thisSame = true;

                if (actual is not null && expect?.Text is not null && expect.Text != actual.Name)
                {
                    same = false;
                    thisSame = false;
                }

                if (actual is not null && expect?.Icon != null && !AssertExtensions.AreEqual(expect.Icon.Value, actual.ExpandedIconMoniker))
                {
                    same = false;
                    thisSame = false;
                }

                var actualIcon = actual?.ExpandedIconMoniker == null
                    ? "null"
                    : ImageMonikerDebuggerDisplay.FromImageMoniker(actual.ExpandedIconMoniker.Value.ToImageMoniker());

                if (expect is not null)
                {
                    expectOutput
                        .Append(' ', depth * 4)
                        .Append(expect.Text ?? actual!.Name)
                        .Append(' ')
                        .AppendLine(expect.Icon != null
                            ? ImageMonikerDebuggerDisplay.FromImageMoniker(expect.Icon.Value)
                            : actualIcon);
                }

                if (actual is not null)
                {
                    actualOutput
                        .Append(' ', depth * 4)
                        .Append(actual.Name)
                        .Append(' ')
                        .Append(actualIcon)
                        .AppendLine(thisSame ? "" : " 🐛");
                }

                if (expect?.Children is not null)
                {
                    if (actual?.IsExpanded == false && expect.Children is not null && expect.Children.Count != 0)
                    {
                        actual.Expand();
                    }

                    var actualChildren = actual?.Items.ToList() ?? new List<SolutionExplorerItemTestExtension>();

                    if (actualChildren.Count != expect.Children!.Count)
                        same = false;

                    var max = Math.Max(actualChildren.Count, expect.Children.Count);

                    for (int i = 0; i < max; i++)
                    {
                        var expectChild = expect.Children.Count > i ? expect.Children[i] : null;
                        var actualChild = actualChildren.Count > i ? actualChildren[i] : null;

                        VerifyNode(
                            expectChild,
                            actualChild,
                            depth + 1);
                    }
                }
            }
        }

        protected override bool TryShutdownHostInstance()
        {
            bool result = base.TryShutdownHostInstance();

            foreach (var projectPath in _rootPaths)
            {
                try
                {
                    Directory.Delete(projectPath, recursive: true);
                }
                catch
                {
                    continue;
                }

                ImmutableInterlocked.Update(ref _rootPaths, (list, item) => list.Remove(item), projectPath);
            }

            return result;
        }

        private string CreateRootPath()
        {
            string rootPath;

            do
            {
                var name = "IntegrationTest_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                rootPath = Path.Combine(Path.GetTempPath(), name);
            }
            while (Directory.Exists(rootPath));

            Directory.CreateDirectory(rootPath);

            ImmutableInterlocked.Update(ref _rootPaths, (list, path) => list.Add(path), rootPath);

            return rootPath;
        }

        /// <summary>
        /// Creates <paramref name="project"/> on disk and opens it, returning its test extension object.
        /// </summary>
        /// <param name="project"></param>
        protected ProjectTestExtension CreateProject(Project project)
        {
            var rootPath = CreateRootPath();

            project.Save(rootPath);

            ProjectTestExtension projectExtension;

            using (Scope.Enter("Open Project"))
            {
                projectExtension = VisualStudio.ObjectModel.Solution.OpenProject(Path.Combine(rootPath, project.RelativeProjectFilePath));
            }

            using (Scope.Enter("Verify Create Project"))
            {
                VisualStudio.ObjectModel.Solution.Verify.HasProject(projectExtension);
            }

            WaitForIntellisense();
            WaitForDependenciesNodeSync();

            return projectExtension;
        }

        protected void CreateSolution(Solution solution)
        {
            var rootPath = CreateRootPath();
            var solutionFilePath = Path.Combine(rootPath, "Solution.sln");

            solution.Save(solutionFilePath);

            solution.GlobalJson?.Save(rootPath);

            foreach (var project in solution.Projects)
            {
                project.Save(rootPath);
            }

            using (Scope.Enter("Open Solution"))
            {
                VisualStudio.ObjectModel.Solution.Open(solutionFilePath);
            }

            using (Scope.Enter("Verify Create Project"))
            {
                foreach (var project in solution.Projects)
                {
                    project.Extension = VisualStudio.ObjectModel.Solution.GetProjectExtension<ProjectTestExtension>(project.ProjectName);
                }
            }

            WaitForIntellisense();
            WaitForDependenciesNodeSync();
        }

        protected void WaitForIntellisense()
        {
            using (Scope.Enter("Wait for Intellisense"))
            {
                VisualStudio.ObjectModel.Solution.WaitForIntellisenseStage();
            }
        }

        protected void WaitForDependenciesNodeSync()
        {
            using (Scope.Enter("Wait for dependencies node to sync"))
            {
                // Wait for dataflow to update the nodes
                // TODO create a more reliable (and usually faster) way of doing this
                // https://github.com/dotnet/project-system/issues/3426
                Thread.Sleep(5 * 1000);
            }
        }

        /// <summary>
        /// Models the expected state of a node in the "Dependencies" node tree.
        /// </summary>
        /// <remarks>
        /// This type is designed to work with object and collection initializers.
        /// For example:
        /// <example>
        /// <code>new Node(".NETCoreApp 2.1", KnownMonikers.Library)
        /// {
        ///     new Node("SDK", KnownMonikers.SDK)
        /// }</code>
        /// </example>
        /// </remarks>
        protected sealed class Node : IEnumerable
        {
            public List<Node>? Children { get; set; }

            /// <summary>
            /// Gets and sets the expected icon for this node.
            /// A <see langword="null"/> value will disable validation.
            /// </summary>
            public string? Text { get; set; }

            /// <summary>
            /// Gets and sets the expected icon for this node.
            /// A <see langword="null"/> value will disable validation.
            /// </summary>
            public ImageMoniker? Icon { get; set; }

            public Node(string? text = null, ImageMoniker? icon = null)
            {
                Text = text;
                Icon = icon;
            }

            public void Add(Node child)
            {
                Children ??= new List<Node>();
                Children.Add(child);
            }

            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}
