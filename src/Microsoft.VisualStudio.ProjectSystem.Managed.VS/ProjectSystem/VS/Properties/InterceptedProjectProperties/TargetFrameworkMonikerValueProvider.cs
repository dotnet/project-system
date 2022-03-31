// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetFrameworkMoniker", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetFrameworkMonikerValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVsServices;
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public TargetFrameworkMonikerValueProvider(IUnconfiguredProjectVsServices unconfiguredProjectVsServices, ProjectProperties properties)
        {
            _unconfiguredProjectVsServices = unconfiguredProjectVsServices;
            _properties = properties;
        }

        public override async Task<string?> OnSetPropertyValueAsync(
            string propertyName,
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            ConfigurationGeneral configuration = await _properties.GetConfigurationGeneralPropertiesAsync();
            string? currentTargetFramework = (string?)await configuration.TargetFramework.GetValueAsync();
            string? currentTargetFrameworks = (string?)await configuration.TargetFrameworks.GetValueAsync();
            if (!string.IsNullOrEmpty(currentTargetFrameworks))
            {
                throw new InvalidOperationException(VSResources.MultiTFEditNotSupported);
            }
            else if (!string.IsNullOrEmpty(currentTargetFramework))
            {
                await defaultProperties.SetPropertyValueAsync(ConfigurationGeneral.TargetFrameworkProperty, unevaluatedPropertyValue);
            }
            else
            {
                // CPS implements IVsHierarchy.SetProperty for the TFM property to call through the multi-targeting service and change the TFM.
                // This causes the project to be reloaded after changing the values.
                // Since the property providers are called under a write-lock, trying to reload the project on the same context fails saying it can't load the project
                // if a lock is held. We are not going to write to the file under this lock (we return null from this method) and so we fork execution here to schedule
                // a lambda on the UI thread and we don't pass the lock information from this context to the new one.
                _unconfiguredProjectVsServices.ThreadingService.RunAndForget(() =>
                {
                    _unconfiguredProjectVsServices.VsHierarchy.SetProperty(HierarchyId.Root, (int)VsHierarchyPropID.TargetFrameworkMoniker, unevaluatedPropertyValue);
                    return System.Threading.Tasks.Task.CompletedTask;
                }, options: ForkOptions.HideLocks | ForkOptions.StartOnMainThread,
                   unconfiguredProject: _unconfiguredProjectVsServices.Project);
            }
            return null;
        }
    }
}
