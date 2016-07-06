// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    [ExportInterceptingPropertyValueProvider]
    internal sealed class TargetFrameworkValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IUnconfiguredProjectVsServices _projectVsServices;

        [ImportingConstructor]
        public TargetFrameworkValueProvider(IUnconfiguredProjectVsServices projectVsServices)
        {
            Requires.NotNull(projectVsServices, nameof(projectVsServices));

            _projectVsServices = projectVsServices;
        }

        public override string GetPropertyName() => "TargetFramework";

        public override async Task<string> InterceptGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            Requires.NotNull(_projectVsServices.VsHierarchy, "vsHierarchy");

            // Fetch the target framework version from the VSHierarchy.
            object targetFrameworkVersion;
            if (ErrorHandler.Succeeded(_projectVsServices.VsHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)Shell.VsHierarchyPropID.TargetFrameworkVersion, out targetFrameworkVersion)))
            {
                return ((uint)targetFrameworkVersion).ToString();
            }

            return await base.InterceptGetEvaluatedPropertyValueAsync(evaluatedPropertyValue, defaultProperties).ConfigureAwait(false);
        }
    }
}