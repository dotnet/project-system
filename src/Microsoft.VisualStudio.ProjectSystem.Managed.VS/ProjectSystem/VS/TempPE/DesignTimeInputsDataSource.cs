// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// Processes Compile source items into <see cref="DesignTimeInputs" /> that includes design time and shared design time inputs
    /// only.
    /// </summary>
    [Export(typeof(IDesignTimeInputsDataSource))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class DesignTimeInputsDataSource : ChainedProjectValueDataSourceBase<DesignTimeInputs>, IDesignTimeInputsDataSource
    {
        private static readonly ImmutableHashSet<string> s_ruleNames = Empty.OrdinalIgnoreCaseStringSet.Add(Compile.SchemaName);

        private readonly UnconfiguredProject _project;
        private readonly IActiveConfiguredProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public DesignTimeInputsDataSource(UnconfiguredProject project,
                                          IUnconfiguredProjectServices unconfiguredProjectServices,
                                          IActiveConfiguredProjectSubscriptionService projectSubscriptionService)
            : base(project, synchronousDisposal: false, registerDataSource: false)
        {
            _project = project;
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override UnconfiguredProject ContainingProject
        {
            get { return _project; }
        }

        protected override IDisposable LinkExternalInput(ITargetBlock<IProjectVersionedValue<DesignTimeInputs>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.SourceItemsRuleSource;

            // Transform the changes from evaluation/design-time build -> restore data
            DisposableValue<ISourceBlock<IProjectVersionedValue<DesignTimeInputs>>> transformBlock = source.SourceBlock
                                                                                .TransformWithNoDelta(update => update.Derive(u => GetDesignTimeInputs(u.CurrentState)),
                                                                                                      suppressVersionOnlyUpdates: false,
                                                                                                      ruleNames: s_ruleNames);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private DesignTimeInputs GetDesignTimeInputs(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
        {
            var designTimeInputs = new HashSet<string>(StringComparers.Paths);
            var designTimeSharedInputs = new HashSet<string>(StringComparers.Paths);

            foreach ((string itemName, IImmutableDictionary<string, string> metadata) in currentState.GetSnapshotOrEmpty(Compile.SchemaName).Items)
            {
                (bool designTime, bool designTimeShared) = GetDesignTimePropsForItem(metadata);

                string fullPath = _project.MakeRooted(itemName);

                if (designTime)
                {
                    designTimeInputs.Add(fullPath);
                }

                // Legacy allows files to be DesignTime and DesignTimeShared
                if (designTimeShared)
                {
                    designTimeSharedInputs.Add(fullPath);
                }
            }

            return new DesignTimeInputs(designTimeInputs, designTimeSharedInputs);
        }

        private static (bool designTime, bool designTimeShared) GetDesignTimePropsForItem(IImmutableDictionary<string, string> item)
        {
            item.TryGetValue(Compile.LinkProperty, out string linkString);
            item.TryGetValue(Compile.DesignTimeProperty, out string designTimeString);
            item.TryGetValue(Compile.DesignTimeSharedInputProperty, out string designTimeSharedString);

            if (!string.IsNullOrEmpty(linkString))
            {
                // Linked files are never used as TempPE inputs
                return (false, false);
            }

            return (StringComparers.PropertyLiteralValues.Equals(designTimeString, bool.TrueString), StringComparers.PropertyLiteralValues.Equals(designTimeSharedString, bool.TrueString));
        }
    }
}
