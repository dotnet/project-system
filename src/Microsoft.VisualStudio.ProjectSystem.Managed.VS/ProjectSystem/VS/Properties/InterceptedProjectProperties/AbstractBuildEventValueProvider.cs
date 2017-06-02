// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    internal abstract partial class AbstractBuildEventValueProvider : InterceptingPropertyValueProviderBase
    {
        protected readonly IProjectLockService _projectLockService;
        protected readonly UnconfiguredProject _unconfiguredProject;
        private readonly Helper _helper;

        protected AbstractBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject,
            Helper helper)
        {
            _projectLockService = projectLockService;
            _unconfiguredProject = unconfiguredProject;
            _helper = helper;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(
            string evaluatedPropertyValue,
            IProjectProperties defaultProperties)
        {
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                return _helper.GetProperty(projectXml);
            }
        }

        public override async Task<string> OnSetPropertyValueAsync(
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            using (var access = await _projectLockService.WriteLockAsync())
            {
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);

                _helper.SetProperty(unevaluatedPropertyValue, projectXml);
            }

            return null;
        }
    }
}
