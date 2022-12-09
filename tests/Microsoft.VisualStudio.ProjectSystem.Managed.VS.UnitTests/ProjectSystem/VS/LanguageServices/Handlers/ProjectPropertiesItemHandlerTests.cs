// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public class ProjectPropertiesItemHandlerTests : EvaluationHandlerTestBase
    {
        [Fact]
        public void Handle_WhenPropertyIsChanged_CallsSetProperty()
        {
            string? nameResult = null;
            string? valueResult = null;
            var context = IWorkspaceProjectContextMockFactory.ImplementSetProperty((name, value) => { nameResult = name; valueResult = value; });

            var handler = CreateInstance();

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": { 
                        "AnyChanges": true,
                        "ChangedProperties": [ "RootNamespace" ]
                    },
                    "After": { 
                        "Properties": {
                            "RootNamespace": "value"
                        }
                    }
                }
                """);

            Handle(context, handler, projectChange);

            Assert.Equal("RootNamespace", nameResult);
            Assert.Equal("value", valueResult);
        }

        [Fact]
        public void Handle_WhenTargetPathIsChanged_SetsBinOutputPath()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            context.BinOutputPath = @"BinOutputPath";

            var handler = CreateInstance();

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": { 
                        "AnyChanges": true,
                        "ChangedProperties": [ "TargetPath" ]
                    },
                    "After": { 
                        "Properties": {
                            "TargetPath": "NewBinOutputPath"
                        }
                    }
                }
                """);

            Handle(context, handler, projectChange);

            Assert.Equal("NewBinOutputPath", context.BinOutputPath);
        }

        [Fact]
        public void Handle_WhenTargetPathIsNotChanged_DoesNotSetBinOutputPath()
        {
            var context = IWorkspaceProjectContextMockFactory.Create();
            context.BinOutputPath = @"BinOutputPath";

            var handler = CreateInstance();

            var projectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": { 
                        "AnyChanges": true,
                        "ChangedProperties": [ ]
                    },
                    "After": { 
                        "Properties": {
                            "TargetPath": "NewBinOutputPath"
                        }
                    }
                }
                """);

            Handle(context, handler, projectChange);

            Assert.Equal("BinOutputPath", context.BinOutputPath);
        }

        [Theory]
        [InlineData(
            """
            {
                "Difference": { 
                    "AnyChanges": false,
                    "ChangedProperties": [ ]
                },
                "After": { 
                    "Properties": {
                        "RootNamespace": "value"
                    }
                }
            }
            """)]
        [InlineData(
            """
            {
                "Difference": { 
                    "AnyChanges": true,
                    "ChangedProperties": [ "TargetPath" ]
                },
                "After": { 
                    "Properties": {
                        "TargetPath": "value"
                    }
                }
            }
            """)]
        [InlineData(
            """
            {
                "Difference": { 
                    "AnyChanges": true,
                    "ChangedProperties": [ ]
                },
                "After": { 
                    "Properties": {
                        "RootNamespace": "value"
                    }
                }
            }
            """)]
        public void Handle_WhenPropertyIsNotChanged_DoesNotCallSetProperty(string input)
        {
            int callCount = 0;
            var context = IWorkspaceProjectContextMockFactory.ImplementSetProperty((name, value) => { callCount++; });

            var handler = CreateInstance();

            var projectChange = IProjectChangeDescriptionFactory.FromJson(input);

            Handle(context, handler, projectChange);

            Assert.Equal(0, callCount);
        }

        internal override IProjectEvaluationHandler CreateInstance()
        {
            return new ProjectPropertiesItemHandler(UnconfiguredProjectFactory.Create());
        }
    }
}
