// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices;

public sealed class UpdateHandlersTests
{
    [Fact]
    public void Construction()
    {
        var exportFactory = ExportFactoryFactory.Implement(
            factory: () => Mock.Of<IWorkspaceUpdateHandler>(MockBehavior.Loose));

        UpdateHandlers instance = new(new[] { exportFactory });
    }

    [Fact]
    public void DisposeDisposesHandlers()
    {
        int disposeCount = 0;

        var exportFactory = ExportFactoryFactory.Implement(
            factory: () => Mock.Of<IWorkspaceUpdateHandler>(MockBehavior.Loose),
            disposeAction: () => disposeCount++);

        UpdateHandlers instance = new(new[] { exportFactory });

        Assert.Equal(0, disposeCount);

        instance.Dispose();

        Assert.Equal(1, disposeCount);

        instance.Dispose();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void EvaluationRulesPopulated()
    {
        using UpdateHandlers instance = new(new[] { Create("Rule1"), Create("Rule2"), Create("Rule3") });

        Assert.Equal(new[] { "Rule1", "Rule2", "Rule3", "ConfigurationGeneral" }, instance.EvaluationRules);

        static ExportFactory<IWorkspaceUpdateHandler> Create(string ruleName)
        {
            return ExportFactoryFactory.Implement<IWorkspaceUpdateHandler>(
                factory: () =>
                {
                    var mock = new Mock<IProjectEvaluationHandler>(MockBehavior.Strict);
                    mock.SetupGet(o => o.ProjectEvaluationRule).Returns(ruleName);
                    return mock.Object;
                });
        }
    }

    [Fact]
    public void PopulatesHandlers()
    {
        var commandLineHandler = new Mock<ICommandLineHandler>(MockBehavior.Loose).Object;
        var evaluationHandler = new Mock<IProjectEvaluationHandler>(MockBehavior.Loose).Object;
        var sourceItemHandler = new Mock<ISourceItemsHandler>(MockBehavior.Loose).Object;

        using UpdateHandlers instance = new(new[]
        {
            ExportFactoryFactory.Implement<IWorkspaceUpdateHandler>(factory: () => commandLineHandler),
            ExportFactoryFactory.Implement<IWorkspaceUpdateHandler>(factory: () => evaluationHandler),
            ExportFactoryFactory.Implement<IWorkspaceUpdateHandler>(factory: () => sourceItemHandler)
        });

        Assert.Equal(new[] { commandLineHandler }, instance.CommandLineHandlers);
        Assert.Equal(new[] { evaluationHandler }, instance.EvaluationHandlers);
        Assert.Equal(new[] { sourceItemHandler }, instance.SourceItemHandlers);
    }
}
