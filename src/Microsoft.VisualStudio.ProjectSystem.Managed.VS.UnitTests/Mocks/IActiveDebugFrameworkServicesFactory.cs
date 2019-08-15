// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal static class IActiveDebugFrameworkServicesFactory
    {
        public static IActiveDebugFrameworkServices ImplementGetConfiguredProjectForActiveFrameworkAsync(ConfiguredProject? project)
        {
            var service = new IActiveDebugFrameworkServicesMock();
            service.ImplementGetConfiguredProjectForActiveFrameworkAsync(project);

            return service.Object;
        }
    }
}
