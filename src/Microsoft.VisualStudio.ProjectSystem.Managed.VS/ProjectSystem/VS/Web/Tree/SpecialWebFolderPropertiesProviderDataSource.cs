// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks.Dataflow;

using PropertiesProviderProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<Microsoft.VisualStudio.ProjectSystem.IProjectTreePropertiesProvider>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web.Tree
{
    /// <summary>
    ///     Provides a data source that produces a new <see cref="IProjectTreePropertiesProvider"/> 
    ///     everytime the ASP.NET code folders change.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProviderDataSource))]
    [AppliesTo("AspNet")] // TODO:
    internal class SpecialWebFolderPropertiesProviderDataSource : ChainedProjectValueDataSourceBase<IProjectTreePropertiesProvider>, IProjectTreePropertiesProviderDataSource
    {
        private readonly SpecialCodeFolderDataSource _codeFolderDataSource;

        [ImportingConstructor]
        public SpecialWebFolderPropertiesProviderDataSource(
            UnconfiguredProject project,
            SpecialCodeFolderDataSource codeFolderDataSource)
            : base(project, synchronousDisposal: true, registerDataSource: true)
        {
            _codeFolderDataSource = codeFolderDataSource;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<PropertiesProviderProjectValue> targetBlock)
        {
            JoinUpstreamDataSources(_codeFolderDataSource);

            DisposableValue<ISourceBlock<PropertiesProviderProjectValue>> block =
                _codeFolderDataSource.SourceBlock.Transform(
                    snapshot => snapshot.Derive(CreateProvider));

            block.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            return block;
        }

        private IProjectTreePropertiesProvider CreateProvider(IImmutableSet<string> folders)
        {
            return new SpecialWebFolderPropertiesProvider(ContainingProject!, folders);
        }
    }
}
