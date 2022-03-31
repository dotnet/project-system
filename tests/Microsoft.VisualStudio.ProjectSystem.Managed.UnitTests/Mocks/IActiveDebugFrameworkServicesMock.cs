// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class IActiveDebugFrameworkServicesMock : AbstractMock<IActiveDebugFrameworkServices>
    {
        public IActiveDebugFrameworkServicesMock()
            : base(MockBehavior.Strict)
        {
        }

        public IActiveDebugFrameworkServicesMock ImplementGetActiveDebuggingFrameworkPropertyAsync(string? activeFramework)
        {
            Setup(x => x.GetActiveDebuggingFrameworkPropertyAsync())
                        .ReturnsAsync(activeFramework);

            return this;
        }

        public IActiveDebugFrameworkServicesMock ImplementGetConfiguredProjectForActiveFrameworkAsync(ConfiguredProject? project)
        {
            Setup(x => x.GetConfiguredProjectForActiveFrameworkAsync())
                        .ReturnsAsync(project);

            return this;
        }

        public IActiveDebugFrameworkServicesMock ImplementGetProjectFrameworksAsync(List<string>? frameworks)
        {
            Setup(x => x.GetProjectFrameworksAsync())
                        .ReturnsAsync(frameworks);

            return this;
        }

        public IActiveDebugFrameworkServicesMock ImplementSetActiveDebuggingFrameworkPropertyAsync(string activeFramework)
        {
            Setup(x => x.SetActiveDebuggingFrameworkPropertyAsync(activeFramework))
                        .ReturnsAsync(() => { });

            return this;
        }
    }
}
