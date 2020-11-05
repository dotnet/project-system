// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;

using ProjectCapabilitiesProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.IProjectCapabilitiesSnapshot>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web
{
    /// <summary>
    ///     Produces capabilities from project type GUIDs.
    /// </summary>
    [Export(ExportContractNames.Scopes.ConfiguredProject, typeof(IProjectCapabilitiesProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class ProjectTypeCapabilitiesProvider : ChainedProjectValueDataSourceBase<IProjectCapabilitiesSnapshot>, IProjectCapabilitiesProvider
    {
        private readonly ProjectTypeGuidsDataSource _dataSource;

        [ImportingConstructor]
        protected ProjectTypeCapabilitiesProvider(ConfiguredProject configuredProject, ProjectTypeGuidsDataSource dataSource)
            : base(configuredProject)
        {
            _dataSource = dataSource;
        }

        public ProjectCapabilitiesProjectValue? Current { get; private set; }

        protected override IDisposable LinkExternalInput(ITargetBlock<ProjectCapabilitiesProjectValue> targetBlock)
        {
            JoinUpstreamDataSources(_dataSource);

            DisposableValue<ISourceBlock<ProjectCapabilitiesProjectValue>> block = _dataSource.SourceBlock.Transform(
                snapshot => Current = snapshot.Derive(s => CreateCapabilitiesSnapshot(s)));

            block.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return block;
        }

        private IProjectCapabilitiesSnapshot CreateCapabilitiesSnapshot(IImmutableList<Guid> projectTypes)
        {
            ImmutableHashSet<string> capabilities = Empty.CapabilitiesSet;

            // TODO: Maybe pull this from a specific item type, or metadata on a capability?
            if (projectTypes.Contains(ProjectType.LegacyWebApplicationProjectGuid))
            {
                capabilities = capabilities.Add("AspNet");
            }
            
            if (projectTypes.Contains(ProjectType.LegacyTestProjectGuid))
            {
                capabilities = capabilities.Add("TestContainer");
            }

            if (projectTypes.Contains(ProjectType.LegacyWPFProjectGuid))
            {
                capabilities = capabilities.Add(ProjectCapability.WPF);
            }

            if (capabilities.Count == 0)
            {
                // TODO: Always add WinForms until we can detect that we're opening "legacy" project
                capabilities = capabilities.Add(ProjectCapability.WindowsForms);
            }

            return Current == null
                ? new ProjectCapabilitiesSnapshot(capabilities)
                : ((ProjectCapabilitiesSnapshot)Current.Value).Update(capabilities);
        }
    }
}
