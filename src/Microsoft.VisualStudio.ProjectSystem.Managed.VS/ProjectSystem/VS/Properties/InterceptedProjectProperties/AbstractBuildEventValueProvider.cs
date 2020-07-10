// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    internal abstract partial class AbstractBuildEventValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly IProjectAccessor _projectAccessor;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly AbstractBuildEventHelper _helper;

        protected AbstractBuildEventValueProvider(
            IProjectAccessor projectAccessor,
            UnconfiguredProject project,
            AbstractBuildEventHelper helper)
        {
            _projectAccessor = projectAccessor;
            _unconfiguredProject = project;
            _helper = helper;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(
            string propertyName,
            string evaluatedPropertyValue,
            IProjectProperties defaultProperties)
        {
            (bool success, string? property) = await _helper.TryGetPropertyAsync(defaultProperties);

            if (success)
            {
                return property ?? string.Empty;
            }

            return await _projectAccessor.OpenProjectXmlForReadAsync(_unconfiguredProject, projectXml => _helper.GetProperty(projectXml)) ?? string.Empty;
        }

        public override async Task<string?> OnSetPropertyValueAsync(
            string propertyName,
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string>? dimensionalConditions = null)
        {
            if (await _helper.TrySetPropertyAsync(unevaluatedPropertyValue, defaultProperties))
            {
                return null;
            }

            await _projectAccessor.OpenProjectXmlForWriteAsync(_unconfiguredProject, projectXml => _helper.SetProperty(unevaluatedPropertyValue, projectXml));
            return null;
        }
    }
}
