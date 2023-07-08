// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

// Nullable annotations don't add a lot of value to this class, and until https://github.com/dotnet/roslyn/issues/33199 is fixed
// MemberData doesn't work anyway
#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class DesignTimeInputsDataSourceTests
    {
        public static IEnumerable<object[]> GetTestCases()
        {
            return new[]
            {
                // A single design time input
                new object[]
                {
                    """
                    "CurrentState": {
                        "Compile": {
                            "Items": { 
                                "File1.cs": {
                                    "DesignTime": true,
                                    "FullPath": "C:\\Project\\File1.cs"
                                }
                            }
                        }
                    }
                    """,
                    new string[] { "C:\\Project\\File1.cs" },
                    new string[] { }
                },

                // A single design time input, and a normal file
                new object[]
                {
                    """
                    "CurrentState": {
                        "Compile": {
                            "Items": { 
                                "File1.cs": {
                                    "DesignTime": true,
                                    "FullPath": "C:\\Project\\File1.cs"
                                },
                                "File2.cs": {
                                    "FullPath": "C:\\Project\\File2.cs"
                                }
                            }
                        }
                    }
                    """,
                    new string[] { "C:\\Project\\File1.cs" },
                    new string[] { }
                },

                // A single design time input, and a single shared design time input
                new object[]
                {
                    """
                    "CurrentState": {
                        "Compile": {
                            "Items": { 
                                "File1.cs": {
                                    "DesignTime": true,
                                    "FullPath": "C:\\Project\\File1.cs"
                                },
                                "File2.cs": {
                                    "DesignTimeSharedInput": true,
                                    "FullPath": "C:\\Project\\File2.cs"
                                }
                            }
                        }
                    }
                    """,
                    new string[] { "C:\\Project\\File1.cs" },
                    new string[] { "C:\\Project\\File2.cs" }
                },

                // A file that is both a design time and shared design time input
                new object[]
                {
                    """
                    "CurrentState": {
                        "Compile": {
                            "Items": { 
                                "File1.cs": {
                                    "DesignTime": true,
                                    "DesignTimeSharedInput": true,
                                    "FullPath": "C:\\Project\\File1.cs"
                                }
                            }
                        }
                    }
                    """,
                    new string[] { "C:\\Project\\File1.cs" },
                    new string[] { "C:\\Project\\File1.cs" }
                },

                // A design time input that is a linked file, and hence ignored
                new object[]
                {
                    """
                    "CurrentState": {
                        "Compile": {
                            "Items": { 
                                "File1.cs": {
                                    "DesignTime": true,
                                    "Link": "foo",
                                    "FullPath": "C:\\Project\\File1.cs"
                                }
                            }
                        }
                    }
                    """,
                    new string[] { },
                    new string[] { }
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetTestCases))]
        public async Task VerifyDesignTimeInputsProcessed(string projectState, string[] designTimeInputs, string[] sharedDesignTimeInputs)
        {
            using DesignTimeInputsDataSource dataSource = CreateDesignTimeInputsDataSource(out ProjectValueDataSource<IProjectSubscriptionUpdate> sourceItemsRuleSource);

            const string defaultProjectConfig =
                """
                "ProjectConfiguration": {
                    "Name": "Debug|AnyCPU",
                    "Dimensions": {
                        "Configuration": "Debug",
                        "Platform": "AnyCPU"
                    }
                }
                """;

            // Create a block to receive the results of the block under test
            DesignTimeInputs inputs = null;
            var receiver = DataflowBlockSlim.CreateActionBlock<IProjectVersionedValue<DesignTimeInputs>>(val =>
            {
                inputs = val.Value;
            });
            dataSource.SourceBlock.LinkTo(receiver, DataflowOption.PropagateCompletion);

            // Construct our input value, including a default project config
            var configUpdate = IProjectSubscriptionUpdateFactory.FromJson("{ " + projectState + "," + defaultProjectConfig + " }");

            // Send our input, and wait for our receiver to complete
            await sourceItemsRuleSource.SendAndCompleteAsync(configUpdate, receiver);

            // Assert
            Assert.NotNull(inputs);
            Assert.Equal(designTimeInputs, inputs.Inputs);
            Assert.Equal(sharedDesignTimeInputs, inputs.SharedInputs);
        }

        private static DesignTimeInputsDataSource CreateDesignTimeInputsDataSource(out ProjectValueDataSource<IProjectSubscriptionUpdate> sourceItemsRuleSource)
        {
            var unconfiguredProjectServices = UnconfiguredProjectServicesFactory.Create(
                    projectService: IProjectServiceFactory.Create(
                        services: ProjectServicesFactory.Create(
                            threadingService: IProjectThreadingServiceFactory.Create(),
                            projectLockService: IProjectLockServiceFactory.Create())));

            var unconfiguredProject = UnconfiguredProjectFactory.Create(
                unconfiguredProjectServices: unconfiguredProjectServices,
                fullPath: @"C:\Project\Project.csproj");

            sourceItemsRuleSource = new ProjectValueDataSource<IProjectSubscriptionUpdate>(unconfiguredProjectServices);

            var projectSubscriptionService = IActiveConfiguredProjectSubscriptionServiceFactory.Create(sourceItemsRuleSource: sourceItemsRuleSource);

            var dataSource = new DesignTimeInputsDataSource(unconfiguredProject, unconfiguredProjectServices, projectSubscriptionService);

            return dataSource;
        }
    }
}
