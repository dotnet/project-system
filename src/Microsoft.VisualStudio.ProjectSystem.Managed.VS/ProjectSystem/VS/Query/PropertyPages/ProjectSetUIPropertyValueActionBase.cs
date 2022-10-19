// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// This type, along with its derived types, serves as an intermediary between the core layers of the 
    /// Project Query API in CPS and the core logic for setting properties which is handled by <see cref="ProjectSetUIPropertyValueActionCore" />.
    /// Responsible for extracting the necessary data from the Project Query API types and delegating work
    /// to the <see cref="ProjectSetUIPropertyValueActionCore"/>.
    /// </summary>
    /// <remarks>
    /// See also: <see cref="ProjectSetEvaluatedUIPropertyValueAction"/> and
    /// <see cref="ProjectSetUnevaluatedUIPropertyValueAction"/>.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the property to set: <see cref="object"/> for setting evaluated property values, and
    /// <see cref="string"/> for setting unevaluated property values.
    /// </typeparam>
    internal abstract class ProjectSetUIPropertyValueActionBase<T> : QueryDataProducerBase<IEntityValue>, IProjectUpdateActionExecutor, IQueryActionExecutor
    {
        private readonly ProjectSetUIPropertyValueActionCore _coreExecutor;

        public ProjectSetUIPropertyValueActionBase(
            string pageName,
            string propertyName,
            ReadOnlyCollection<ProjectSystem.Query.Framework.Actions.ConfigurationDimensionValue> dimensions)
        {
            _coreExecutor = new ProjectSetUIPropertyValueActionCore(
                pageName,
                propertyName,
                dimensions.Select(d => (d.Dimension, d.Value)),
                SetValueAsync);
        }

        public Task OnBeforeExecutingBatchAsync(IReadOnlyList<QueryProcessResult<IEntityValue>> allItems, CancellationToken cancellationToken)
        {
            Requires.NotNull(allItems, nameof(allItems));

            IEnumerable<UnconfiguredProject> targetProjects = allItems
                .Select(item => ((IEntityValueFromProvider)item.Result).ProviderState)
                .OfType<UnconfiguredProject>();

            return _coreExecutor.OnBeforeExecutingBatchAsync(targetProjects);
        }

        public async Task ReceiveResultAsync(QueryProcessResult<IEntityValue> result)
        {
            Requires.NotNull(result, nameof(result));
            result.Request.QueryExecutionContext.CancellationToken.ThrowIfCancellationRequested();
            if (((IEntityValueFromProvider)result.Result).ProviderState is UnconfiguredProject project)
            {
                if (await _coreExecutor.ExecuteAsync(project))
                {
                    project.GetQueryDataVersion(out string versionKey, out long versionNumber);
                    result.Request.QueryExecutionContext.ReportUpdatedDataVersion(versionKey, versionNumber);
                }
            }

            await ResultReceiver.ReceiveResultAsync(result);
        }

        public Task OnRequestProcessFinishedAsync(IQueryProcessRequest request)
        {
            _coreExecutor.OnAfterExecutingBatch();
            return ResultReceiver.OnRequestProcessFinishedAsync(request);
        }

        /// <summary>
        /// Sets the value on the given <paramref name="property"/>.
        /// </summary>
        /// <remarks>
        /// Abstract because we need different logic for setting evaluated and unevaluated values.
        /// </remarks>
        protected abstract Task SetValueAsync(IProperty property);
    }
}
