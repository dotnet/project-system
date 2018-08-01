// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class IActiveDebugFrameworkServicesMock : AbstractMock<IActiveDebugFrameworkServices>
    {
        public IActiveDebugFrameworkServicesMock()
            : base(MockBehavior.Strict)
        {
        }

        public IActiveDebugFrameworkServicesMock ImplementGetActiveDebuggingFrameworkPropertyAsync(string activeFramework)
        {
            Setup(x => x.GetActiveDebuggingFrameworkPropertyAsync())
                        .ReturnsAsync(activeFramework);

            return this;
        }

        public IActiveDebugFrameworkServicesMock ImplementGetConfiguredProjectForActiveFrameworkAsync(ConfiguredProject project)
        {
            Setup(x => x.GetConfiguredProjectForActiveFrameworkAsync())
                        .ReturnsAsync(project);

            return this;
        }

        public IActiveDebugFrameworkServicesMock ImplementGetProjectFrameworksAsync(List<string> frameworsks)
        {
            Setup(x => x.GetProjectFrameworksAsync())
                        .ReturnsAsync(frameworsks);

            return this;
        }

        public IActiveDebugFrameworkServicesMock ImplementSetActiveDebuggingFrameworkPropertyAsync(string aciiveFramework)
        {
            Setup(x => x.SetActiveDebuggingFrameworkPropertyAsync(aciiveFramework))
                        .ReturnsAsync(() => { });

            return this;
        }
    }
}
