// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    [Export(typeof(ICommandLineArgumentsProvider))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class CommandLineArgumentsProvider : ChainedProjectValueDataSourceBase<CommandLineArgumentsSnapshot>, ICommandLineArgumentsProvider
    {
        private readonly IProjectBuildSnapshotService _projectBuildSnapshotService;

        [ImportingConstructor]
        public CommandLineArgumentsProvider(
            ConfiguredProject project,
            IProjectBuildSnapshotService projectBuildSnapshotService)
            : base(project, synchronousDisposal: false, registerDataSource: false)
        {
            _projectBuildSnapshotService = projectBuildSnapshotService;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<CommandLineArgumentsSnapshot>> targetBlock)
        {
            CommandLineArgumentsSnapshot? lastSnapshot = null;

            // Transform the changes from build snapshot to command line arguments.
            // We support skipping inputs/output values if multiple exist (WithNoDelta), as only the most recent values are deemed useful.
            DisposableValue<ISourceBlock<IProjectVersionedValue<CommandLineArgumentsSnapshot>>> transformBlock
                = _projectBuildSnapshotService.SourceBlock.TransformWithNoDelta(update => update.Derive(snapshot => Transform(snapshot)));

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source block, so if it needs to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(_projectBuildSnapshotService);

            return transformBlock;

            CommandLineArgumentsSnapshot Transform(IProjectBuildSnapshot buildSnapshot)
            {
                if (!buildSnapshot.TargetOutputs.TryGetValue("CompileDesignTime", out IImmutableList<KeyValuePair<string, IImmutableDictionary<string, string>>>? targetOutputs))
                {
                    return new(ImmutableArray<string>.Empty, isChanged: false);
                }

                if (lastSnapshot is not null && IsUnchanged())
                {
                    if (lastSnapshot.IsChanged)
                    {
                        lastSnapshot = new(lastSnapshot.Arguments, isChanged: false);
                    }

                    return lastSnapshot;
                }

                ImmutableArray<string>.Builder options = ImmutableArray.CreateBuilder<string>(targetOutputs.Count);

                foreach ((string option, _) in targetOutputs)
                {
                    options.Add(option);
                }

                return lastSnapshot = new(options.MoveToImmutable(), isChanged: true);

                bool IsUnchanged()
                {
                    ImmutableArray<string> arguments = lastSnapshot.Arguments;

                    if (targetOutputs.Count == arguments.Length)
                    {
                        int i = 0;
                        foreach ((string arg, _) in targetOutputs)
                        {
                            if (!StringComparer.Ordinal.Equals(arg, arguments[i++]))
                            {
                                return false;
                            }
                        }

                        return true;
                    }

                    return false;
                }
            }
        }
    }
}
