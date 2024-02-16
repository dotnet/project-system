// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

public sealed class MSBuildDependencyCollectionTests
{
    private readonly DependencyGroupType _dependencyGroupType = new(
        "id",
        "caption",
        KnownProjectImageMonikers.Library,
        KnownProjectImageMonikers.LibraryWarning,
        KnownProjectImageMonikers.LibraryError,
        ProjectTreeFlags.Create("Test"));

    private static readonly ProjectImageMoniker _icon = new(Guid.NewGuid(), 1);
    private static readonly ProjectImageMoniker _iconImplicit = new(Guid.NewGuid(), 1);
    private static readonly ProjectImageMoniker _iconWarning = new(Guid.NewGuid(), 1);
    private static readonly ProjectImageMoniker _iconError = new(Guid.NewGuid(), 1);

    [Fact]
    public void Constructor()
    {
        var factory = new Mock<MSBuildDependencyFactoryBase>(MockBehavior.Strict);
        factory.SetupGet(factory => factory.DependencyGroupType).Returns(_dependencyGroupType);

        var collection = new MSBuildDependencyCollection(factory.Object);

        Assert.Same(_dependencyGroupType, collection.DependencyGroupType);

        factory.VerifyAll();
    }

    [Fact]
    public void TryUpdate_DoesNothingWhenNoChanges()
    {
        var factory = new Mock<MSBuildDependencyFactoryBase>(MockBehavior.Strict);
        factory.SetupGet(f => f.UnresolvedRuleName).Returns("UnresolvedRuleName");
        factory.SetupGet(f => f.ResolvedRuleName).Returns("ResolvedRuleName");
        factory.SetupGet(factory => factory.DependencyGroupType).Returns(_dependencyGroupType);

        var collection = new MSBuildDependencyCollection(factory.Object);

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.Create();
        IProjectChangeDescription buildProjectChange = IProjectChangeDescriptionFactory.Create();

        Assert.False(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out ImmutableArray<IDependency>? dependencies));
        Assert.Null(dependencies);
    }

    [Fact]
    public void TryUpdate_AddsDependencyWhenIntroducedInEvaluation()
    {
        var collection = Create();

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "Item1" ],
                        "ChangedItems": [],
                        "RemovedItems": []
                    },
                    "Before": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                        }
                    },
                    "After": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                            "Item1": {
                                "A": "1"
                            }
                        }
                    }
                }
                """);
        IProjectChangeDescription? buildProjectChange = null;

        Assert.True(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out ImmutableArray<IDependency>? dependencies));
        Assert.NotNull(dependencies);
        var dependency = Assert.IsType<MSBuildDependency>(Assert.Single<IDependency>(dependencies));
        Assert.Equal("Item1", dependency.Id);
        Assert.Equal("UnresolvedCaption", dependency.Caption);
        Assert.Equal(DependencyTreeFlags.UnresolvedDependencyFlags + ProjectTreeFlags.Create("TestUnresolved"), dependency.Flags);
        Assert.Equal(DiagnosticLevel.None, dependency.DiagnosticLevel);
        Assert.Equal(_icon, dependency.Icon);
        Assert.Equal("UnresolvedRuleName", dependency.SchemaName);
        Assert.Equal("SchemaItemType", dependency.SchemaItemType);
        Assert.False(dependency.UseResolvedReferenceRule);
        Assert.Equal(new[] { new KeyValuePair<string, string>("A", "1") }, dependency.BrowseObjectProperties);
        Assert.Equal("Item1", dependency.FilePath);
        Assert.False(dependency.IsImplicit);
        Assert.Null(dependency.IsResolved);
    }

    [Fact]
    public void TryUpdate_AddsResolvedDependencyWhenIntroducedInEvaluationAndBuild()
    {
        var collection = Create();

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "Item1" ]
                    },
                    "After": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                            "Item1": {
                                "A": "1"
                            }
                        }
                    }
                }
                """);
        IProjectChangeDescription? buildProjectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "ResolvedItem1" ]
                    },
                    "After": {
                        "RuleName": "ResolvedRuleName",
                        "Items": {
                            "ResolvedItem1": {
                                "OriginalItemSpec": "Item1",
                                "A": "2"
                            }
                        }
                    }
                }
                """);

        Assert.True(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out ImmutableArray<IDependency>? dependencies));
        Assert.NotNull(dependencies);
        var dependency = Assert.IsType<MSBuildDependency>(Assert.Single<IDependency>(dependencies));
        Assert.Equal("Item1", dependency.Id);
        Assert.Equal("ResolvedCaption", dependency.Caption);
        Assert.Equal(DependencyTreeFlags.ResolvedDependencyFlags + ProjectTreeFlags.Create("TestResolved"), dependency.Flags);
        Assert.Equal(DiagnosticLevel.None, dependency.DiagnosticLevel);
        Assert.Equal(_icon, dependency.Icon);
        Assert.Equal("ResolvedRuleName", dependency.SchemaName);
        Assert.Equal("SchemaItemType", dependency.SchemaItemType);
        Assert.True(dependency.UseResolvedReferenceRule);
        Assert.Equal(new[] { new KeyValuePair<string, string>("OriginalItemSpec", "Item1"), new KeyValuePair<string, string>("A", "2") }, dependency.BrowseObjectProperties);
        Assert.Equal("ResolvedItem1", dependency.FilePath);
        Assert.False(dependency.IsImplicit);
        Assert.True(dependency.IsResolved);
    }

    [Fact]
    public void TryUpdate_AddsResolvedDependencyWhenIntroducedInEvaluationThenBuild()
    {
        var collection = Create();

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "Item1" ]
                    },
                    "After": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                            "Item1": {
                                "A": "1"
                            }
                        }
                    }
                }
                """);
        IProjectChangeDescription? buildProjectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "ResolvedItem1" ]
                    },
                    "After": {
                        "RuleName": "ResolvedRuleName",
                        "Items": {
                            "ResolvedItem1": {
                                "OriginalItemSpec": "Item1",
                                "A": "2"
                            }
                        }
                    }
                }
                """);

        // Evaluation only
        Assert.True(collection.TryUpdate(evaluationProjectChanged, buildProjectChange: null, "FullPath", out ImmutableArray<IDependency>? dependencies));
        // Evaluation and build (joint)
        Assert.True(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out dependencies));
        Assert.NotNull(dependencies);
        var dependency = Assert.IsType<MSBuildDependency>(Assert.Single<IDependency>(dependencies));
        Assert.Equal("Item1", dependency.Id);
        Assert.Equal("ResolvedCaption", dependency.Caption);
        Assert.Equal(DependencyTreeFlags.ResolvedDependencyFlags + ProjectTreeFlags.Create("TestResolved"), dependency.Flags);
        Assert.Equal(DiagnosticLevel.None, dependency.DiagnosticLevel);
        Assert.Equal(_icon, dependency.Icon);
        Assert.Equal("ResolvedRuleName", dependency.SchemaName);
        Assert.Equal("SchemaItemType", dependency.SchemaItemType);
        Assert.True(dependency.UseResolvedReferenceRule);
        Assert.Equal(new[] { new KeyValuePair<string, string>("OriginalItemSpec", "Item1"), new KeyValuePair<string, string>("A", "2") }, dependency.BrowseObjectProperties);
        Assert.Equal("ResolvedItem1", dependency.FilePath);
        Assert.False(dependency.IsImplicit);
        Assert.True(dependency.IsResolved);
    }

    [Fact]
    public void TryUpdate_AddsUnresolvedDependencyWhenIntroducedInEvaluationButNotBuild()
    {
        var collection = Create();

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "Item1" ]
                    },
                    "After": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                            "Item1": {
                                "A": "1"
                            }
                        }
                    }
                }
                """);
        IProjectChangeDescription? buildProjectChange = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": false,
                        "AddedItems": []
                    },
                    "After": {
                        "RuleName": "ResolvedRuleName",
                        "Items": {}
                    }
                }
                """);

        Assert.True(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out ImmutableArray<IDependency>? dependencies));
        Assert.NotNull(dependencies);
        var dependency = Assert.IsType<MSBuildDependency>(Assert.Single<IDependency>(dependencies));
        Assert.Equal("Item1", dependency.Id);
        Assert.Equal("UnresolvedCaption", dependency.Caption);
        Assert.Equal(DependencyTreeFlags.UnresolvedDependencyFlags + ProjectTreeFlags.Create("TestUnresolved"), dependency.Flags);
        Assert.Equal(DiagnosticLevel.Warning, dependency.DiagnosticLevel);
        Assert.Equal(_iconWarning, dependency.Icon);
        Assert.Equal("UnresolvedRuleName", dependency.SchemaName);
        Assert.Equal("SchemaItemType", dependency.SchemaItemType);
        Assert.False(dependency.UseResolvedReferenceRule);
        Assert.Equal(new[] { new KeyValuePair<string, string>("A", "1") }, dependency.BrowseObjectProperties);
        Assert.Equal("Item1", dependency.FilePath);
        Assert.False(dependency.IsImplicit);
        Assert.False(dependency.IsResolved);
    }

    [Fact]
    public void TryUpdate_MarksImplicit_IsImplicitlyDefined()
    {
        var collection = Create();

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "Item1" ],
                        "ChangedItems": [],
                        "RemovedItems": []
                    },
                    "After": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                            "Item1": {
                                "IsImplicitlyDefined": "true"
                            }
                        }
                    }
                }
                """);
        IProjectChangeDescription? buildProjectChange = null;

        Assert.True(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out ImmutableArray<IDependency>? dependencies));
        Assert.NotNull(dependencies);
        var dependency = Assert.IsType<MSBuildDependency>(Assert.Single<IDependency>(dependencies));

        Assert.True(dependency.IsImplicit);
        Assert.Equal(_iconImplicit, dependency.Icon);
    }

    [Fact]
    public void TryUpdate_MarksImplicit_DefiningProjectFullPath()
    {
        var collection = Create();

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "Item1" ],
                        "ChangedItems": [],
                        "RemovedItems": []
                    },
                    "After": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                            "Item1": {
                                "DefiningProjectFullPath": "OtherPath"
                            }
                        }
                    }
                }
                """);
        IProjectChangeDescription? buildProjectChange = null;

        Assert.True(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out ImmutableArray<IDependency>? dependencies));
        Assert.NotNull(dependencies);
        var dependency = Assert.IsType<MSBuildDependency>(Assert.Single<IDependency>(dependencies));

        Assert.True(dependency.IsImplicit);
        Assert.Equal(_iconImplicit, dependency.Icon);
    }

    [Fact]
    public void TryUpdate_IgnoresNonVisibleDependency()
    {
        var collection = Create();

        IProjectChangeDescription evaluationProjectChanged = IProjectChangeDescriptionFactory.FromJson(
                """
                {
                    "Difference": {
                        "AnyChanges": true,
                        "AddedItems": [ "Item1" ],
                        "ChangedItems": [],
                        "RemovedItems": []
                    },
                    "After": {
                        "RuleName": "UnresolvedRuleName",
                        "Items": {
                            "Item1": {
                                "Visible": "false"
                            }
                        }
                    }
                }
                """);
        IProjectChangeDescription? buildProjectChange = null;

        Assert.False(collection.TryUpdate(evaluationProjectChanged, buildProjectChange, "FullPath", out ImmutableArray<IDependency>? dependencies));
        Assert.Null(dependencies);
    }

    private MSBuildDependencyCollection Create()
    {
        var factory = new Mock<MSBuildDependencyFactoryBase>(MockBehavior.Strict);

        factory.SetupGet(f => f.UnresolvedRuleName).Returns("UnresolvedRuleName");
        factory.SetupGet(f => f.ResolvedRuleName).Returns("ResolvedRuleName");
        factory.SetupGet(f => f.SchemaItemType).Returns("SchemaItemType");
        factory.SetupGet(f => f.DependencyGroupType).Returns(_dependencyGroupType);
        factory.SetupGet(f => f.Icon).Returns(_icon);
        factory.SetupGet(f => f.IconImplicit).Returns(_iconImplicit);
        factory.SetupGet(f => f.IconWarning).Returns(_iconWarning);
        factory.SetupGet(f => f.IconError).Returns(_iconError);
        factory.SetupGet(f => f.FlagCache).Returns(new MSBuildDependencyFactoryBase.DependencyFlagCache(ProjectTreeFlags.Create("TestResolved"), ProjectTreeFlags.Create("TestUnresolved")));
        factory.Setup(f => f.GetOriginalItemSpec("ResolvedItem1", It.IsAny<ImmutableDictionary<string, string>>())).Returns("Item1");
        factory.Setup(f => f.GetUnresolvedCaption("Item1", It.IsAny<ImmutableDictionary<string, string>>())).Returns("UnresolvedCaption");
        factory.Setup(f => f.GetResolvedCaption("ResolvedItem1", "Item1", It.IsAny<ImmutableDictionary<string, string>>())).Returns("ResolvedCaption");
        factory.Setup(f => f.UpdateTreeFlags(It.IsAny<string>(), It.IsAny<ProjectTreeFlags>())).Returns((string id, ProjectTreeFlags f) => f);
        factory.Setup(f => f.GetDiagnosticLevel(It.IsAny<bool?>(), It.IsAny<bool>(), It.IsAny<ImmutableDictionary<string, string>>(), It.IsAny<DiagnosticLevel>())).CallBase();

        return new MSBuildDependencyCollection(factory.Object);
    }
}
