// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Base class for <see cref="IQueryActionExecutor"/>s that modify launch settings and launch profiles.
    /// </summary>
    internal abstract class LaunchProfileActionBase : QueryDataProducerBase<IEntityValue>, IQueryActionExecutor
    {
        public Task OnRequestProcessFinishedAsync(IQueryProcessRequest request)
        {
            return ResultReceiver.OnRequestProcessFinishedAsync(request);
        }

        public async Task ReceiveResultAsync(QueryProcessResult<IEntityValue> result)
        {
            result.Request.QueryExecutionContext.CancellationToken.ThrowIfCancellationRequested();

            if (((IEntityValueFromProvider)result.Result).ProviderState is UnconfiguredProject project
                && project.Services.ExportProvider.GetExportedValueOrDefault<ILaunchSettingsProvider>() is ILaunchSettingsProvider launchSettingsProvider)
            {
                await ExecuteAsync(launchSettingsProvider, result.Request.QueryExecutionContext.CancellationToken);
            }

            await ResultReceiver.ReceiveResultAsync(result);
        }

        protected abstract Task ExecuteAsync(ILaunchSettingsProvider launchSettingsProvider, CancellationToken cancellationToken);
    }
}
