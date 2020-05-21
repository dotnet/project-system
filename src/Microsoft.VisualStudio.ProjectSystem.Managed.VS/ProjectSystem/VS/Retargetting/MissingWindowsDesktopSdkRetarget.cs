﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

/*
 * 
 *  IGNORE THIS, JUST FOR TESTING
 * 
 */

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
#if DEBUG
    [Export(typeof(IProjectRetargetCheckProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
#endif
    internal class MissingWindowsDesktopSdkRetarget : IProjectRetargetCheckProvider
    {
        private static readonly IEnumerable<string> s_rules = new string[] { ConfigurationGeneral.SchemaName };

        private readonly ConfiguredProject _project;
        private readonly IProjectAccessor _projectAccessor;

        [ImportingConstructor]
        internal MissingWindowsDesktopSdkRetarget(ConfiguredProject project,
                                                  IVsService<SVsTrackProjectRetargeting, IVsTrackProjectRetargeting2> retargettingService,
                                                  IProjectAccessor projectAccessor)
        {
            _project = project;
            _projectAccessor = projectAccessor;
        }

        public IEnumerable<string> GetProjectEvaluationRuleNames() => s_rules;

        public Task<TargetDescriptionBase?> CheckAsync(IImmutableDictionary<string, IProjectRuleSnapshot> projectState)
        {
            string? useWpf = projectState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, "UseWPF", null);
            string? useWindowsForms = projectState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, "UseWindowsForms", null);

            if (useWpf == null && useWindowsForms == null)
            {
                return Task.FromResult((TargetDescriptionBase?)new SelectDesktopPlatformTargetDescription());
            }

            return Task.FromResult((TargetDescriptionBase?)null);
        }

        public Task FixAsync(IProjectTargetChange projectTargetChange)
        {
            var selectPlatform = projectTargetChange as SelectDesktopPlatformTargetDescription;

            Assumes.NotNull(selectPlatform);

            return _projectAccessor.OpenProjectXmlForWriteAsync(_project.UnconfiguredProject, root =>
            {
                Microsoft.Build.Construction.ProjectPropertyElement prop = BuildUtilities.GetOrAddProperty(root, "Use" + selectPlatform.SelectedPlatform);
                prop.Value = "true";

                root.Save();
            });
        }
    }
}
