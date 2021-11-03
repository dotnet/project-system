// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Runtimes
{
    [Export(typeof(IRuntimeDescriptorDataSource))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class RuntimeDescriptorDataSource : ChainedProjectValueDataSourceBase<ISet<RuntimeDescriptor>>, IRuntimeDescriptorDataSource
    {
        private static readonly ImmutableHashSet<string> s_validPackages =
            ImmutableHashSet.Create("Microsoft.AspNetCore.All", "Microsoft.AspNetCore.App", "Microsoft.NETCore.App", "Microsoft.WindowsDesktop.App");

        private static readonly ImmutableDictionary<int, string> s_packageVertionToComponentId = ImmutableDictionary.Create<int, string>()
            .Add(2, "Microsoft.Net.Core.Component.SDK.2.1")
            .Add(3, "Microsoft.NetCore.Component.Runtime.3.1")
            .Add(5, "Microsoft.NetCore.Component.Runtime.5.0");

        private static readonly ImmutableHashSet<string> s_rules = Empty.OrdinalIgnoreCaseStringSet
                                                                        .Add(MissingSdkRuntime.SchemaName);

        private readonly IProjectSubscriptionService _projectSubscriptionService;

        [ImportingConstructor]
        public RuntimeDescriptorDataSource(
            ConfiguredProject project,
            IProjectSubscriptionService projectSubscriptionService)
            : base(project, synchronousDisposal: true, registerDataSource: false)
        {
            _projectSubscriptionService = projectSubscriptionService;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<IProjectVersionedValue<ISet<RuntimeDescriptor>>> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _projectSubscriptionService.ProjectBuildRuleSource;

            // Transform the changes from design-time build -> sdk runtime component data
            DisposableValue<ISourceBlock<IProjectVersionedValue<ISet<RuntimeDescriptor>>>> transformBlock =
                source.SourceBlock.TransformWithNoDelta(update => update.Derive(u => CreateRuntimeDescriptor(u.CurrentState)),
                                                        suppressVersionOnlyUpdates: false,
                                                        ruleNames: s_rules);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private ISet<RuntimeDescriptor> CreateRuntimeDescriptor(IImmutableDictionary<string, IProjectRuleSnapshot> currentState)
        {
            IProjectRuleSnapshot missingSdkRuntimes = currentState.GetSnapshotOrEmpty(MissingSdkRuntime.SchemaName);

            if (missingSdkRuntimes.Items.Count == 0)
            {
                return ImmutableHashSet<RuntimeDescriptor>.Empty;
            }

            var runtimeDescriptors = missingSdkRuntimes.Items.Select(item =>
            {
                item.Value.TryGetStringProperty(MissingSdkRuntime.VersionProperty, out string? packageVersion);
                string? componentId = MapPackageNameToComponentId(item.Key, packageVersion);
                return new RuntimeDescriptor(componentId);
            });

            // We should only see one runtime version to install.
            // VS should be able to handle it if the numbers are different.
            return new HashSet<RuntimeDescriptor>(runtimeDescriptors);
        }

        private string? MapPackageNameToComponentId(string packageName, string? packageVersion)
        {
            string compomentId = string.Empty;
            // This will return these for other .NET Core versions such as 3.1.0, 2.1.0, etc.
            // If VS doesn't have those as installation options, it should either ignore those items,
            // or possibly generate a message or warning somewhere but not prompt for IAP
            if (string.IsNullOrEmpty(packageVersion))
            {
                return compomentId;
            }

            (int major, int _) = GetPackageMajorMinorVersionNumbers(packageVersion!);

            if (s_validPackages.Contains(packageName))
            {
                compomentId = s_packageVertionToComponentId[major];
            }

            return compomentId;
        }

        private static (int, int) GetPackageMajorMinorVersionNumbers(string packageVersion)
        {
            // Ignore patch number
            var versionNumbers = packageVersion.Split('.');
            int major = int.Parse(versionNumbers[0]);
            int minor = int.Parse(versionNumbers[1]);
            
            return (major, minor);
        }
    }
}
