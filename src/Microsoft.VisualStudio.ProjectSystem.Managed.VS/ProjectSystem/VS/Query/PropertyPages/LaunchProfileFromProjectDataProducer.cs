// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    internal class LaunchProfileFromProjectDataProducer : QueryDataFromProviderStateProducerBase<UnconfiguredProject>
    {
        private readonly ILaunchProfilePropertiesAvailableStatus _properties;

        public LaunchProfileFromProjectDataProducer(ILaunchProfilePropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, UnconfiguredProject providerState)
        {
            if (providerState.Services.ExportProvider.GetExportedValueOrDefault<IProjectLaunchProfileHandler>() is IProjectLaunchProfileHandler launchProfileHandler)
            {
                return launchProfileHandler.RetrieveAllLaunchProfileEntitiesAsync(queryExecutionContext, parent, _properties);
            }

            return TaskResult.EmptyEnumerable<IEntityValue>();
        }
    }
}
