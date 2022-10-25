// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIEditorMetadataSnapshot"/> from a <see cref="ValueEditor"/> and reporting the
    /// results.
    /// </summary>
    /// <remarks>
    /// The <see cref="ValueEditor"/> comes from the parent <see cref="IUIPropertyEditorSnapshot"/>
    /// </remarks>
    internal class UIEditorMetadataFromValueEditorProducer : QueryDataFromProviderStateProducerBase<ValueEditor>
    {
        private readonly IUIEditorMetadataPropertiesAvailableStatus _properties;

        public UIEditorMetadataFromValueEditorProducer(IUIEditorMetadataPropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, ValueEditor providerState)
        {
            return Task.FromResult(UIEditorMetadataProducer.CreateMetadataValues(parent, providerState, _properties));
        }
    }
}
