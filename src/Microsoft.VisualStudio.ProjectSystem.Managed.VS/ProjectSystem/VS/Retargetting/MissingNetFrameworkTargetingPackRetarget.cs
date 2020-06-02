// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [Export(typeof(IProjectPrerequisiteCheckProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class MissingNetFrameworkTargetingPackRetarget : IProjectPrerequisiteCheckProvider
    {
        private static readonly IEnumerable<string> s_rules = new string[] { ConfigurationGeneral.SchemaName };

        public IEnumerable<string> GetProjectEvaluationRuleNames() => s_rules;

        public Task<TargetDescriptionBase?> CheckAsync(IImmutableDictionary<string, IProjectRuleSnapshot> projectState)
        {
            string? targetFrameworkIdentifier = projectState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkIdentifierProperty, string.Empty);
            string? targetFrameworkVersion = projectState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkVersionProperty, string.Empty);
            string? targetFrameworkProfile = projectState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkProfileProperty, string.Empty);

            if (targetFrameworkVersion.Length > 1)
            {
                string versionWithoutTheV = targetFrameworkVersion.Substring(1);

                if (!Microsoft.Build.Utilities.ToolLocationHelper.GetPathToReferenceAssemblies(targetFrameworkIdentifier, targetFrameworkVersion, targetFrameworkProfile).Any())
                {
                    return Task.FromResult((TargetDescriptionBase?)new InstallTargetingPackTargetDescription($"Microsoft.Net.Component.{versionWithoutTheV}.TargetingPack"));
                }
            }

            return Task.FromResult((TargetDescriptionBase?)null);
        }
    }
}
