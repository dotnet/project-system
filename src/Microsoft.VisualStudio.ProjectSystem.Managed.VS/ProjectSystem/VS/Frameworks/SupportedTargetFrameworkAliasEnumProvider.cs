// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;
using EnumCollection = System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>;
using EnumCollectionProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Frameworks
{
    /// <summary>
    ///     Responsible for producing valid values for the TargetFramework property from a design-time build.
    /// </summary>
    [ExportDynamicEnumValuesProvider("SupportedTargetFrameworkAliasEnumProvider")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class SupportedTargetFrameworkAliasEnumProvider : ChainedProjectValueDataSourceBase<EnumCollection>, IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
    {
        private readonly IProjectSubscriptionService _subscriptionService;

        [ImportingConstructor]
        public SupportedTargetFrameworkAliasEnumProvider(
            ConfiguredProject project,
            IProjectSubscriptionService subscriptionService)
            : base(project, synchronousDisposal: false, registerDataSource: false)
        {
            _subscriptionService = subscriptionService;

            ReadyToBuild = new OrderPrecedenceImportCollection<IConfiguredProjectReadyToBuild>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        public OrderPrecedenceImportCollection<IConfiguredProjectReadyToBuild> ReadyToBuild
        {
            get;
        }

        [ConfiguredProjectAutoLoad]
        [AppliesTo(ProjectCapability.DotNet)]
        public void Load()
        {
            // To avoid UI delays when opening the AppDesigner for the first time, 
            // we auto-load so that we are included in the first design-time build
            // for the project.
            EnsureInitialized();
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<EnumCollectionProjectValue> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = _subscriptionService.ProjectBuildRuleSource;

            // Transform the changes from design-time build -> Supported target frameworks
            DisposableValue<ISourceBlock<EnumCollectionProjectValue>> transformBlock = source.SourceBlock.TransformWithNoDelta(
                update => update.Derive(Transform),
                suppressVersionOnlyUpdates: false,
                ruleNames: SupportedTargetFrameworkAlias.SchemaName);

            // Set the link up so that we publish changes to target block
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        private static EnumCollection Transform(IProjectSubscriptionUpdate input)
        {
            IProjectRuleSnapshot snapshot = input.CurrentState[SupportedTargetFrameworkAlias.SchemaName];

            return snapshot.Items.Select(ToEnumValue)
                                    .ToList();
        }

        private static IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item)
        {
            return new PageEnumValue(new EnumValue()
            {
                // Example: <SupportedTargetFrameworkAlias  Include="net5.0-windows"
                //                                          DisplayName=".NET 5.0" />

                Name = item.Key,
                DisplayName = item.Value[SupportedTargetFrameworkAlias.DisplayNameProperty]
            });
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(this);
        }

        public async Task<EnumCollection> GetListedValuesAsync()
        {
            if (!IsReadyToBuild())
                throw new InvalidOperationException("This configuration is not set to build");

            // NOTE: This has a race, if called off the UI thread, the configuration could become 
            // inactive underneath us and hence not ready for build, causing below to block forever.

            using (JoinableCollection.Join())
            {
                EnumCollectionProjectValue snapshot = await SourceBlock.ReceiveAsync();

                // TODO: This is a hotfix for item ordering. Remove this OrderBy when completing: https://github.com/dotnet/project-system/issues/7025
                return snapshot.Value.OrderBy(e => e.DisplayName).ToArray();
            }
        }

        private bool IsReadyToBuild()
        {
            IConfiguredProjectReadyToBuild? readyToBuild = ReadyToBuild.FirstOrDefault()?.Value;

            return readyToBuild?.IsValidToBuild == true;
        }

        bool IDynamicEnumValuesGenerator.AllowCustomValues => false;

        Task<IEnumValue?> IDynamicEnumValuesGenerator.TryCreateEnumValueAsync(string userSuppliedValue) => TaskResult.Null<IEnumValue>();
    }
}
