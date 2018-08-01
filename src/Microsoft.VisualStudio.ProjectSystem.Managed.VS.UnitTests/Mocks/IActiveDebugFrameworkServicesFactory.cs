// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class IActiveDebugFrameworkServicesFactory : AbstractMock<IActiveDebugFrameworkServices>
    {
        public IActiveDebugFrameworkServicesFactory()
            : base(MockBehavior.Strict)
        {
        }

        public IActiveDebugFrameworkServicesFactory ImplementGetActiveDebuggingFrameworkPropertyAsync(string activeFramework)
        {
            Setup(x => x.GetActiveDebuggingFrameworkPropertyAsync())
                        .Returns(Task.FromResult(activeFramework));
            return this;
        }

        public IActiveDebugFrameworkServicesFactory ImplementGetConfiguredProjectForActiveFrameworkAsync(ConfiguredProject project)
        {
            Setup(x => x.GetConfiguredProjectForActiveFrameworkAsync())
                        .Returns(Task.FromResult(project));

            return this;
        }

        public IActiveDebugFrameworkServicesFactory ImplementGetProjectFrameworksAsync(List<string> frameworsks)
        {
            Setup(x => x.GetProjectFrameworksAsync())
                        .Returns(Task.FromResult(frameworsks));

            return this;
        }

        public IActiveDebugFrameworkServicesFactory ImplementSetActiveDebuggingFrameworkPropertyAsync(string aciiveFramework)
        {
            Setup(x => x.SetActiveDebuggingFrameworkPropertyAsync(aciiveFramework))
                        .Returns(Task.CompletedTask);

            return this;
        }
    }
}
