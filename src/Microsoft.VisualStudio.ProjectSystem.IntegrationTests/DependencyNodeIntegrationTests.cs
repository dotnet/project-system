// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                new Node("SDK", ManagedImageMonikers.Sdk));
        }

        [TestMethod]
        public void MultiTarget_NetCoreApp21_Net45()
        {
            CreateProject(new Project("netcoreapp2.1;net45"));

            VerifyDependenciesNode(
                new Node(".NETCoreApp 2.1", ManagedImageMonikers.Library)
                {
                    new Node("SDK", ManagedImageMonikers.Sdk)
                },
                new Node(".NETFramework 4.5", ManagedImageMonikers.Library)
                {
                    new Node("Assemblies", ManagedImageMonikers.Reference)
                });
        }

        [TestMethod]
        public void MultiTarget_NetStandard20_Net461()
        {
            CreateProject(new Project("netstandard2.0;net461"));

            VerifyDependenciesNode(
                new Node(".NETFramework 4.6.1", ManagedImageMonikers.Library)
                {
                    new Node("Assemblies", ManagedImageMonikers.Reference)
                },
                new Node(".NETStandard 2.0", ManagedImageMonikers.Library)
                {
                    new Node("SDK", ManagedImageMonikers.Sdk)
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
                new Node(".NETFramework 4.6.1", ManagedImageMonikers.Library)
                {
                    new Node("Assemblies", ManagedImageMonikers.Reference),
                    new Node("Packages", ManagedImageMonikers.NuGetGrey)
                    {
                        new Node("MetadataExtractor (2.1.0)", ManagedImageMonikers.NuGetGrey)
                    }
                },
                new Node(".NETStandard 2.0", ManagedImageMonikers.Library)
                {
                    new Node("Packages", ManagedImageMonikers.NuGetGrey)
                    {
                        new Node("MetadataExtractor (2.1.0)", ManagedImageMonikers.NuGetGrey)
                    },
                    new Node("SDK", ManagedImageMonikers.Sdk)
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
                new Node("Projects", ManagedImageMonikers.Application)
                {
                    new Node(project2.ProjectName, ManagedImageMonikers.Application)
                },
                new Node("SDK", ManagedImageMonikers.Sdk));

            VerifyDependenciesNode(
                project2,
                new Node("SDK", ManagedImageMonikers.Sdk));
        }
    }
}
