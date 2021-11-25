// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    // TODO test different SDK version
    // TODO review GitHub issues and produce tests for known bugs
    // TODO ensure VS shut down after test fixture completes

    [TestClass]
    public sealed class DependencyNodeIntegrationTests : ProjectLayoutTestBase
    {
        [TestMethod]
        public void SingleTarget_NetCoreApp21()
        {
            CreateProject(new Project("netcoreapp2.1"));

            VerifyDependenciesNode(
                new Node("SDK", KnownMonikers.SDK));
        }

        [TestMethod]
        public void MultiTarget_NetCoreApp21_Net45()
        {
            CreateProject(new Project("netcoreapp2.1;net45"));

            VerifyDependenciesNode(
                new Node("netcoreapp2.1", KnownMonikers.Library)
                {
                    new Node("SDK", KnownMonikers.SDK)
                },
                new Node("net45", KnownMonikers.Library)
                {
                    new Node("Assemblies", KnownMonikers.Reference)
                });
        }

        [TestMethod]
        public void MultiTarget_NetStandard20_Net461()
        {
            CreateProject(new Project("netstandard2.0;net461"));

            VerifyDependenciesNode(
                new Node("net461", KnownMonikers.Library)
                {
                    new Node("Assemblies", KnownMonikers.Reference)
                },
                new Node("netstandard2.0", KnownMonikers.Library)
                {
                    new Node("SDK", KnownMonikers.SDK)
                });
        }

        [TestMethod]
        public void MultiTarget_WithPackageRef()
        {
            var project = new Project("netstandard2.0;net461")
            {
                new PackageReference("MetadataExtractor", "2.1.0")
            };

            CreateProject(project);

            VerifyDependenciesNode(
                new Node("net461", KnownMonikers.Library)
                {
                    new Node("Assemblies", KnownMonikers.Reference),
                    new Node("Packages", KnownMonikers.NuGetNoColor)
                    {
                        new Node("MetadataExtractor (2.1.0)", KnownMonikers.NuGetNoColor)
                    }
                },
                new Node("netstandard2.0", KnownMonikers.Library)
                {
                    new Node("Packages", KnownMonikers.NuGetNoColor)
                    {
                        new Node("MetadataExtractor (2.1.0)", KnownMonikers.NuGetNoColor)
                    },
                    new Node("SDK", KnownMonikers.SDK)
                });
        }

        [TestMethod]
        public void ProjectToProjectReferences()
        {
            var project2 = new Project("netstandard1.6");

            var project1 = new Project("netcoreapp2.1")
            {
                project2
            };

            var solution = new Solution
            {
                new GlobalJson(sdkVersion: "2.1.600"),
                project1,
                project2
            };

            CreateSolution(solution);

            VerifyDependenciesNode(
                project1,
                new Node("Projects", KnownMonikers.Application)
                {
                    new Node(project2.ProjectName, KnownMonikers.Application)
                },
                new Node("SDK", KnownMonikers.SDK));

            VerifyDependenciesNode(
                project2,
                new Node("SDK", KnownMonikers.SDK));
        }

        [TestMethod]
        public void AssemblyReferences()
        {
            var project = new Project("net461")
            {
                new AssemblyReference("System.Windows.Forms")
            };

            CreateProject(project);

            VerifyDependenciesNode(
                project,
                new Node("Assemblies", KnownMonikers.Reference)
                {
                    new Node("System", KnownMonikers.ReferencePrivate),
                    new Node("System.Core", KnownMonikers.ReferencePrivate),
                    new Node("System.Data", KnownMonikers.ReferencePrivate),
                    new Node("System.Drawing", KnownMonikers.ReferencePrivate),
                    new Node("System.IO.Compression.FileSystem", KnownMonikers.ReferencePrivate),
                    new Node("System.Numerics", KnownMonikers.ReferencePrivate),
                    new Node("System.Runtime.Serialization", KnownMonikers.ReferencePrivate),
                    new Node("System.Windows.Forms", KnownMonikers.Reference), // non private as explicitly added
                    new Node("System.Xml", KnownMonikers.ReferencePrivate),
                    new Node("System.Xml.Linq", KnownMonikers.ReferencePrivate)
                });
        }

        [TestMethod]
        [DataRow("net461;netstandard1.3")]
        [DataRow("net461")]
        public void DteFindsReferences(string targetFrameworks)
        {
            // NOTE the dependencies node makes only the first TFM visible via DTE.
            // For example, netstandard1.3 has 49 references while net461 has the 14 shown here.

            CreateProject(new Project(targetFrameworks)
            {
                new PackageReference("MetadataExtractor", "2.1.0"),
                new AssemblyReference("System.Windows.Forms"),
                new CSharpClass("Class1")
            });

            var expected = new[]
            {
                "System.IO.Compression.FileSystem",
                "System.Numerics",
                "System.Xml.Linq",
                "System.Data",
                "System.Core",
                "System",
                "System.Runtime.Serialization",
                "System.Drawing",
                "System.Xml",
                "System.Windows.Forms",
                "mscorlib",
                "MetadataExtractor",
                "Microsoft.CSharp",
                "XmpCore"
            };

            var projects = (Array)VisualStudio.Dte.ActiveSolutionProjects;
            var vsproject = (VSLangProj.VSProject)projects.Cast<EnvDTE.Project>().First().Object;
            var actual = vsproject.References
                .Cast<VSLangProj.Reference>()
                .Select(r => r.Name)
                .ToList();

            CollectionAssert.AreEquivalent(expected, actual);
        }
    }
}
