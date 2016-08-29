// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetFrameworkMoniker", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetFrameworkMonikerValueProvider : InterceptingPropertyValueProviderBase
    {
        private IUnconfiguredProjectVsServices _unconfiguredProjectVsServices;

        [ImportingConstructor]
        public TargetFrameworkMonikerValueProvider(IUnconfiguredProjectVsServices unconfiguredProjectVsServices)
        {
            _unconfiguredProjectVsServices = unconfiguredProjectVsServices;
        }

        public override Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            // CPS implements IVsHierarchy.SetProperty for the TFM property to call through the multi-targeting service and change the TFM.
            // This causes the project to be reloaded after changing the values.
            // Since the property providers are called under a write-lock, trying to reload the project on the same context fails saying it can't load the project
            // if a lock is held. We are not going to write to the file under this lock (we return null from this method) and so we fork execution here to schedule
            // a lambda on the UI thread and we don't pass the lock information from this context to the new one. 
            _unconfiguredProjectVsServices.ThreadingService.Fork(() =>
            {
                _unconfiguredProjectVsServices.VsHierarchy.SetProperty(HierarchyId.Root, (int)VsHierarchyPropID.TargetFrameworkMoniker, unevaluatedPropertyValue);
                return System.Threading.Tasks.Task.CompletedTask;
            }, options: ForkOptions.HideLocks | ForkOptions.StartOnMainThread);

            return System.Threading.Tasks.Task.FromResult<string>(null);
        }
    }
}
