// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider("TargetFrameworkMoniker", ExportInterceptingPropertyValueProviderFile.ProjectFile)]
    internal sealed class TargetFrameworkMonikerValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVsServices;
        private readonly ProjectProperties _properties;
        private readonly IVsFrameworkParser _frameworkParser;

        private const string _targetFrameworkProperty = "TargetFramework";

        [ImportingConstructor]
        public TargetFrameworkMonikerValueProvider(IUnconfiguredProjectVsServices unconfiguredProjectVsServices, ProjectProperties properties, IVsFrameworkParser frameworkParser)
        {
            _unconfiguredProjectVsServices = unconfiguredProjectVsServices;
            _properties = properties;
            _frameworkParser = frameworkParser;
        }

        public override async Task<string?> OnSetPropertyValueAsync(
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
                var frameworkName = new FrameworkName(unevaluatedPropertyValue);
                await defaultProperties.SetPropertyValueAsync(_targetFrameworkProperty, _frameworkParser.GetShortFrameworkName(frameworkName));
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
