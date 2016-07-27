// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetFrameworkMoniker")]
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
            _unconfiguredProjectVsServices.ThreadingService.Fork(() =>
            {
                _unconfiguredProjectVsServices.VsHierarchy.SetProperty(HierarchyId.Root, (int)VsHierarchyPropID.TargetFrameworkMoniker, unevaluatedPropertyValue);
                return System.Threading.Tasks.Task.CompletedTask;
            }, options: ForkOptions.HideLocks | ForkOptions.StartOnMainThread);
            
            return System.Threading.Tasks.Task.FromResult<string>(null);
        }
    }
}
