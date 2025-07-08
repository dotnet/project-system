// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem;

internal static partial class IProjectThreadingServiceFactory
{
    public static IProjectThreadingService Create(bool verifyOnUIThread = true)
    {
        var mock = Mock.Of<IProjectThreadingService>();
        var projectThreadingService = new ProjectThreadingService(verifyOnUIThread);
        
        Mock.Get(mock)
            .SetupGet(s => s.JoinableTaskContext)
            .Returns(projectThreadingService.JoinableTaskContext);

        Mock.Get(mock)
            .SetupGet(s => s.JoinableTaskFactory)
            .Returns(projectThreadingService.JoinableTaskFactory);

        Mock.Get(mock)
            .SetupGet(s => s.IsOnMainThread)
            .Returns(projectThreadingService.IsOnMainThread);

        Mock.Get(mock)
            .Setup(s => s.ExecuteSynchronously(It.IsAny<Func<Task>>()))
            .Callback((Func<Task> asyncAction) => projectThreadingService.ExecuteSynchronously(asyncAction));

        // Set up the generic ExecuteSynchronously method - we need to use a concrete type for the setup
        Mock.Get(mock)
            .Setup(s => s.ExecuteSynchronously(It.IsAny<Func<Task<object>>>()))
            .Returns((Func<Task<object>> asyncAction) => projectThreadingService.ExecuteSynchronously(asyncAction));

        Mock.Get(mock)
            .Setup(s => s.ExecuteSynchronously(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> asyncAction) => projectThreadingService.ExecuteSynchronously(asyncAction));

        Mock.Get(mock)
            .Setup(s => s.ExecuteSynchronously(It.IsAny<Func<Task<string>>>()))
            .Returns((Func<Task<string>> asyncAction) => projectThreadingService.ExecuteSynchronously(asyncAction));

        Mock.Get(mock)
            .Setup(s => s.VerifyOnUIThread())
            .Callback(() => projectThreadingService.VerifyOnUIThread());

        Mock.Get(mock)
            .Setup(s => s.Fork(
                It.IsAny<Func<Task>>(),
                It.IsAny<JoinableTaskFactory>(),
                It.IsAny<UnconfiguredProject>(),
                It.IsAny<ConfiguredProject>(),
                It.IsAny<ErrorReportSettings>(),
                It.IsAny<ProjectFaultSeverity>(),
                It.IsAny<ForkOptions>()))
            .Callback((Func<Task> asyncAction,
                JoinableTaskFactory? factory,
                UnconfiguredProject? unconfiguredProject,
                ConfiguredProject? configuredProject,
                ErrorReportSettings? watsonReportSettings,
                ProjectFaultSeverity faultSeverity,
                ForkOptions options) =>
                projectThreadingService.Fork(asyncAction, factory, unconfiguredProject, configuredProject, watsonReportSettings, faultSeverity, options));

        Mock.Get(mock)
            .Setup(s => s.SuppressProjectExecutionContext())
            .Returns(() => projectThreadingService.SuppressProjectExecutionContext());

        return mock;
    }
}
