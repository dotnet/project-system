// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    internal abstract partial class AbstractBuildEventValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IProjectAccessor _projectAccessor;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly AbstractBuildEventHelper _helper;

        protected AbstractBuildEventValueProvider(
            IProjectAccessor projectAccessor,
            UnconfiguredProject project,
            IProjectThreadingService threadingService,
            AbstractBuildEventHelper helper)
        {
            _projectAccessor = projectAccessor;
            _unconfiguredProject = project;
            _threadingService = threadingService;
            _helper = helper;
        }

        public override Task<string> OnGetEvaluatedPropertyValueAsync(
            string evaluatedPropertyValue,
            IProjectProperties defaultProperties)
        {
            return _projectAccessor.OpenProjectXmlForReadAsync(_unconfiguredProject, projectXml =>
            {
                return _threadingService.ExecuteSynchronously(() => _helper.GetPropertyAsync(projectXml, defaultProperties));
            });
        }

        public override async Task<string> OnSetPropertyValueAsync(
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            await _projectAccessor.OpenProjectXmlForWriteAsync(_unconfiguredProject, projectXml =>
            {
                _threadingService.ExecuteSynchronously(() => _helper.SetPropertyAsync(unevaluatedPropertyValue, defaultProperties, projectXml));
            });

            return null;
        }
    }
}
