// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

        // There are two ways of storing pre/post build events in the project.
        //
        // 1. As MSBuild properties (PreBuildEvent / PostBuildEvent)
        // 2. As MSBuild tasks (PreBuild / PostBuild)
        //
        // Properties were used in legacy projects.
        //
        // For SDK style projects, we should use tasks.
        //
        // In legacy projects, the properties were defined _after_ the import of common targets,
        // meaning that the properties had access to a full range of property values for use in their
        // bodies.
        //
        // In SDK projects, it's not possible to define a property in the project after the common
        // targets, so an MSBuild task is used instead.
        //
        // Some projects still define these events using properties, and the below code will work
        // with such properties when they exist. However if these properties are absent, then
        // tasks are used instead.
        //
        // Examples of MSBuild properties that are not available to PreBuildEvent/PostBuildEvent
        // properties (but which are available to PreBuild/PostBuild targets) are ProjectExt,
        // PlatformName, ProjectDir, TargetDir, TargetFileName, TargetExt, ProjectFileName,
        // ProjectPath, TargetPath, TargetName, ProjectName, ConfigurationName, and OutDir.
        //
        // Tasks are defined as:
        //
        // <Target Name="PreBuild" AfterTargets="PreBuildEvent">
        //   <Exec Command="echo Hello World" />
        // </Target>
        // <Target Name="PostBuild" AfterTargets="PostBuildEvent">
        //   <Exec Command="echo Hello World" />
        // </Target>

        public override async Task<string> OnGetUnevaluatedPropertyValueAsync(
            string propertyName,
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties)
        {
            (bool success, string? property) = await _helper.TryGetUnevaluatedPropertyValueAsync(defaultProperties);

            if (success)
            {
                return property ?? string.Empty;
            }

            return await _projectAccessor.OpenProjectXmlForReadAsync(_unconfiguredProject, _helper.TryGetValueFromTarget) ?? string.Empty;
        }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(
            string propertyName,
            string evaluatedPropertyValue,
            IProjectProperties defaultProperties)
        {
            (bool success, string property) = await _helper.TryGetEvaluatedPropertyValueAsync(defaultProperties);

            if (success)
            {
                return property;
            }

            return await _projectAccessor.OpenProjectXmlForReadAsync(_unconfiguredProject, _helper.TryGetValueFromTarget) ?? string.Empty;
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
