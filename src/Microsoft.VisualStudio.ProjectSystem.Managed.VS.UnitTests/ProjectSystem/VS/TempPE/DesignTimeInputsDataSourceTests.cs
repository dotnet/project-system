// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.VS.TempPE;
using Xunit;

// Nullable annotations don't add a lot of value to this class, and until https://github.com/dotnet/roslyn/issues/33199 is fixed
// MemberData doesn't work anyway
#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.VS
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
                    @"""CurrentState"": {
                        ""Compile"": {
                            ""Items"": { 
                                ""File1.cs"": {
                                    ""DesignTime"": true
                                }
                            }
                        }
                    }",
                    new string[] { "File1.cs" },
                    new string[] { }
                },

                // A single design time input, and a normal file
                new object[]
                {
                    @"""CurrentState"": {
                        ""Compile"": {
                            ""Items"": { 
                                ""File1.cs"": {
                                    ""DesignTime"": true
                                },
                                ""File2.cs"": { }
                            }
                        }
                    }",
                    new string[] { "File1.cs" },
                    new string[] { }
                },

                // A single design time input, and a single shared design time input
                new object[]
                {
                    @"""CurrentState"": {
                        ""Compile"": {
                            ""Items"": { 
                                ""File1.cs"": {
                                    ""DesignTime"": true
                                },
                                ""File2.cs"": {
                                    ""DesignTimeSharedInput"": true
                                }
                            }
                        }
                    }",
                    new string[] { "File1.cs" },
                    new string[] { "File2.cs" }
                },

                // A file that is both a design time and shared design time input
                new object[]
                {
                    @"""CurrentState"": {
                        ""Compile"": {
                            ""Items"": { 
                                ""File1.cs"": {
                                    ""DesignTime"": true,
                                    ""DesignTimeSharedInput"": true
                                }
                            }
                        }
                    }",
                    new string[] { "File1.cs" },
                    new string[] { "File1.cs" }
                },

                // A design time input that is a linked file, and hence ignored
                new object[]
                {
                    @"""CurrentState"": {
                        ""Compile"": {
                            ""Items"": { 
                                ""File1.cs"": {
                                    ""DesignTime"": true,
                                    ""Link"": ""foo""
                                }
                            }
                        }
                    }",
                    new string[] { },
                    new string[] { }
                },
            };
        }

        [Theory]
        [MemberData(nameof(GetTestCases))]
        public async Task VerifyDesignTimeInputsProcessed(string projectState, string[] designTimeInputs, string[] sharedDesignTimeInputs)
        {
            var projectServices = ProjectServicesFactory.Create(threadingService: IProjectThreadingServiceFactory.Create(), projectLockService: IProjectLockServiceFactory.Create());
            var projectService = IProjectServiceFactory.Create(projectServices);
            var projectSubscriptionService = IProjectSubscriptionServiceFactory.Create();

            var configuredProject = ConfiguredProjectFactory.Create(
                services: ConfiguredProjectServicesFactory.Create(projectService: projectService),
                unconfiguredProject: UnconfiguredProjectFactory.Create());

            // Construct and initialize an instance to test
            using var dataSource = new DesignTimeInputsDataSource(configuredProject, projectSubscriptionService);

            dataSource.Test_Initialize();

            const string defaultProjectConfig = @"""ProjectConfiguration"": {
                                                    ""Name"": ""Debug|AnyCPU"",
                                                    ""Dimensions"": {
                                                        ""Configuration"": ""Debug"",
                                                        ""Platform"": ""AnyCPU""
                                                    }
                                                }";

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
            var sourceItems = (ProjectValueDataSource<IProjectSubscriptionUpdate>)projectSubscriptionService.SourceItemsRuleSource;
            await sourceItems.SendAndCompleteAsync(configUpdate, receiver);

            // Assert
            Assert.NotNull(inputs);
            Assert.Equal(designTimeInputs, inputs.Inputs);
            Assert.Equal(sharedDesignTimeInputs, inputs.SharedInputs);
        }
    }
}
