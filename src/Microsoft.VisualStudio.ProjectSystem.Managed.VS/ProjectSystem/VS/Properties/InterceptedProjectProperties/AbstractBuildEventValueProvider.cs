// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    internal abstract partial class AbstractBuildEventValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IProjectLockService _projectLockService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly AbstractBuildEventHelper _helper;

        protected AbstractBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject project,
            AbstractBuildEventHelper helper)
        {
            _projectLockService = projectLockService;
            _unconfiguredProject = project;
            _helper = helper;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(
            string evaluatedPropertyValue,
            IProjectProperties defaultProperties)
        {
#pragma warning disable RS0030 // symbol IProjectLockService is banned
            return _projectLockService.ReadLockAsync(async access =>
            {
                ProjectRootElement projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath);
                return await _helper.GetPropertyAsync(projectXml, defaultProperties);
            });
#pragma warning restore RS0030 // symbol IProjectLockService is banned
        }

        public override async Task<string> OnSetPropertyValueAsync(
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
#pragma warning disable RS0030 // symbol IProjectLockService is banned
            await _projectLockService.WriteLockAsync(async access =>
            {
                ProjectRootElement projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath);
                await _helper.SetPropertyAsync(unevaluatedPropertyValue, defaultProperties, projectXml);
            });
#pragma warning restore RS0030 // symbol IProjectLockService is banned

            return null;
        }
    }
}
