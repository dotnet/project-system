// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal class IActiveDebugFrameworkServicesFactory
    {
        private Mock<IActiveDebugFrameworkServices> _mock;
        public IActiveDebugFrameworkServicesFactory(MockBehavior mockBehavior = MockBehavior.Strict)
        {
            _mock = new Mock<IActiveDebugFrameworkServices>(mockBehavior);
        }

        public IActiveDebugFrameworkServices Object => _mock.Object;

        public void Verify()
        {
            _mock.Verify();
        }

        public IActiveDebugFrameworkServicesFactory ImplementGetActiveDebuggingFrameworkPropertyAsync(string activeFramework)
        {
            _mock.Setup(x => x.GetActiveDebuggingFrameworkPropertyAsync())
                              .Returns(Task.FromResult(activeFramework));
            return this;
        }

        public IActiveDebugFrameworkServicesFactory ImplementGetConfiguredProjectForActiveFrameworkAsync(ConfiguredProject project)
        {
            _mock.Setup(x => x.GetConfiguredProjectForActiveFrameworkAsync())
                              .Returns(Task.FromResult(project));
            return this;
        }

        public IActiveDebugFrameworkServicesFactory ImplementGetProjectFrameworksAsync(List<string> frameworsks)
        {
            _mock.Setup(x => x.GetProjectFrameworksAsync())
                              .Returns(Task.FromResult(frameworsks));
            return this;
        }

        public IActiveDebugFrameworkServicesFactory ImplementSetActiveDebuggingFrameworkPropertyAsync(string aciiveFramework)
        {
            _mock.Setup(x => x.SetActiveDebuggingFrameworkPropertyAsync(aciiveFramework))
                              .Returns(Task.CompletedTask);
            return this;
        }
    }
}
