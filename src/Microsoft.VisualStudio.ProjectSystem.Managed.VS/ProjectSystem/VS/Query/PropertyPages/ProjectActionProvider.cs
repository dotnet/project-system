// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModelMethods.Actions;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <para>
    /// Handles Project Query API actions that target the <see cref="ProjectSystem.Query.ProjectModel.IProject"/>.
    /// </para>
    /// <para>
    /// Specifically, this type is responsible for creating the appropriate <see cref="IQueryActionExecutor"/>
    /// for a given <see cref="ExecutableStep"/>, and all further processing is handled by that executor.
    /// </para>
    /// </summary>
    [QueryDataProvider(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.TypeName, ProjectModel.ModelName)]
    [QueryActionProvider(ProjectModelActionNames.SetEvaluatedUIPropertyValue, typeof(SetEvaluatedUIPropertyValue))]
    [QueryActionProvider(ProjectModelActionNames.SetUnevaluatedUIPropertyValue, typeof(SetUnevaluatedUIPropertyValue))]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryActionProvider))]
    internal sealed class ProjectActionProvider : IQueryActionProvider
    {
        private readonly IPropertyPageQueryCacheProvider _queryCacheProvider;

        [ImportingConstructor]
        public ProjectActionProvider(IPropertyPageQueryCacheProvider queryCacheProvider)
        {
            _queryCacheProvider = queryCacheProvider;
        }

        public IQueryActionExecutor CreateQueryActionDataTransformer(ExecutableStep executableStep)
        {
            Requires.NotNull(executableStep, nameof(executableStep));

            return executableStep.Action switch
            {
                ProjectModelActionNames.SetEvaluatedUIPropertyValue => new ProjectSetEvaluatedUIPropertyValueAction(_queryCacheProvider, (SetEvaluatedUIPropertyValue)executableStep),
                ProjectModelActionNames.SetUnevaluatedUIPropertyValue => new ProjectSetUnevaluatedUIPropertyValueAction(_queryCacheProvider, (SetUnevaluatedUIPropertyValue)executableStep),
                _ => throw new InvalidOperationException($"{nameof(ProjectActionProvider)} does not handle action '{executableStep.Action}'.")
            };
        }
    }

    /// <summary>
    /// <para>
    /// Handles the core logic of setting properties on projects. Note this type has no dependencies on the Project Query API;
    /// extracting the necessary data from the API is handled by <see cref="ProjectSetUIPropertyValueActionBase{T}"/>.
    /// </para>
    /// <para>
    /// This handles setting a specific property on a specific page across multiple configurations of multiple projects.
    /// </para>
    /// </summary>
    internal class ProjectSetUIPropertyValueActionCore
    {
        private readonly IPropertyPageQueryCacheProvider _queryCacheProvider;
        private readonly string _pageName;
        private readonly string _propertyName;
        private readonly IEnumerable<(string dimension, string value)> _dimensions;
        private readonly Func<IProperty, Task> _setValueAsync;

        private readonly Dictionary<string, List<IRule>> _rules = new(StringComparers.Paths);

        /// <summary>
        /// Creates a <see cref="ProjectSetUIPropertyValueActionCore"/>.
        /// </summary>
        /// <param name="queryCacheProvider">Provides access to a <see cref="UnconfiguredProject"/>'s known configurations and <see cref="IRule"/>s.</param>
        /// <param name="pageName">The name of the page containing the property.</param>
        /// <param name="propertyName">The name of the property to update.</param>
        /// <param name="dimensions">The dimension names and values indicating which project configurations should be updated with the new value.</param>
        /// <param name="setValueAsync">A delegate that, given the <see cref="IProperty"/> to update, actually sets the value.</param>
        public ProjectSetUIPropertyValueActionCore(
            IPropertyPageQueryCacheProvider queryCacheProvider,
            string pageName,
            string propertyName,
            IEnumerable<(string dimension, string value)> dimensions,
            Func<IProperty, Task> setValueAsync)
        {
            _queryCacheProvider = queryCacheProvider;
            _pageName = pageName;
            _propertyName = propertyName;
            _dimensions = dimensions;
            _setValueAsync = setValueAsync;
        }

        /// <summary>
        /// Handles any pre-processing that should occur before actually setting the property values.
        /// This is called once before <see cref="ExecuteAsync(UnconfiguredProject)"/>.
        /// </summary>
        /// <remarks>
        /// Because of the project locks help by the core parts of the Project Query API in CPS we need
        /// to retrieve and cache all of the affected <see cref="IRule"/>s ahead of time. 
        /// </remarks>
        /// <param name="targetProjects">The set of projects we should try to update.</param>
        public async Task OnBeforeExecutingBatchAsync(IEnumerable<UnconfiguredProject> targetProjects)
        {
            foreach (UnconfiguredProject project in targetProjects)
            {
                if (!_rules.TryGetValue(project.FullPath, out List<IRule> projectRules))
                {
                    projectRules = new List<IRule>();

                    IPropertyPageQueryCache propertyPageCache = _queryCacheProvider.CreateCache(project);
                    if (await propertyPageCache.GetKnownConfigurationsAsync() is IImmutableSet<ProjectConfiguration> knownConfigurations)
                    {
                        foreach (ProjectConfiguration knownConfiguration in knownConfigurations.Where(config => config.MatchesDimensions(_dimensions)))
                        {
                            if (await propertyPageCache.BindToRule(knownConfiguration, _pageName) is IRule boundRule)
                            {
                                projectRules.Add(boundRule);
                            }
                        }
                    }

                    _rules.Add(project.FullPath, projectRules);
                }
            }
        }

        /// <summary>
        /// Handles setting the property value within a single project. This is called once per
        /// <see cref="UnconfiguredProject"/> targeted by the query.
        /// </summary>
        /// <param name="targetProject">The project to update.</param>
        public async Task ExecuteAsync(UnconfiguredProject targetProject)
        {
            if (_rules.TryGetValue(targetProject.FullPath, out List<IRule> boundRules))
            {
                foreach (IRule boundRule in boundRules)
                {
                    if (boundRule.GetProperty(_propertyName) is IProperty property)
                    {
                        await _setValueAsync(property);
                    }
                }
            }
        }

        /// <summary>
        /// Handles clean up when we're all done executing the project action. This is called
        /// once after all calls to <see cref="ExecuteAsync(UnconfiguredProject)"/> have completed.
        /// </summary>
        public void OnAfterExecutingBatch()
        {
            _rules.Clear();
        }
    }

    /// <summary>
    /// This type, along with it's derived types, serves as an intermediary between the core layers of the 
    /// Project Query API in CPS and the core logic for setting properties which is handled by <see cref="ProjectSetUIPropertyValueActionCore" />.
    /// Responsible for extracting the necessary data from the Project Query API types and delegating work
    /// to the <see cref="ProjectSetUIPropertyValueActionCore"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the property to set: <see cref="object"/> for setting evaluated property values, and
    /// <see cref="string"/> for setting unevaluated property values.
    /// </typeparam>
    internal abstract class ProjectSetUIPropertyValueActionBase<T> : QueryDataProducerBase<IEntityValue>, IProjectUpdateActionExecutor, IQueryActionExecutor
    {
        private readonly ProjectSetUIPropertyValueActionCore _coreExecutor;

        public ProjectSetUIPropertyValueActionBase(
            IPropertyPageQueryCacheProvider queryCacheProvider,
            string pageName,
            string propertyName,
            ReadOnlyCollection<ProjectSystem.Query.ProjectModelMethods.Actions.ConfigurationDimensionValue> dimensions)
        {
            _coreExecutor = new ProjectSetUIPropertyValueActionCore(
                queryCacheProvider,
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
                await _coreExecutor.ExecuteAsync(project);
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

    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="ProjectModelActionNames.SetEvaluatedUIPropertyValue"/> actions.
    /// </summary>
    internal sealed class ProjectSetEvaluatedUIPropertyValueAction : ProjectSetUIPropertyValueActionBase<object?>
    {
        private readonly SetEvaluatedUIPropertyValue _parameter;

        public ProjectSetEvaluatedUIPropertyValueAction(IPropertyPageQueryCacheProvider queryCacheProvider, SetEvaluatedUIPropertyValue parameter)
            : base(queryCacheProvider, parameter.Page, parameter.Name, parameter.Dimensions)
        {
            Requires.NotNull(parameter, nameof(parameter));
            Requires.NotNull(parameter.Dimensions, $"{nameof(parameter)}.{nameof(parameter.Dimensions)}");

            _parameter = parameter;
        }

        protected override Task SetValueAsync(IProperty property)
        {
            return property.SetValueAsync(_parameter.Value);
        }
    }

    /// <summary>
    /// <see cref="IQueryActionExecutor"/> handling <see cref="ProjectModelActionNames.SetUnevaluatedUIPropertyValue"/> actions.
    /// </summary>
    internal sealed class ProjectSetUnevaluatedUIPropertyValueAction : ProjectSetUIPropertyValueActionBase<string?>
    {
        private readonly SetUnevaluatedUIPropertyValue _parameter;

        public ProjectSetUnevaluatedUIPropertyValueAction(IPropertyPageQueryCacheProvider queryCacheProvider, SetUnevaluatedUIPropertyValue parameter)
            : base(queryCacheProvider, parameter.Page, parameter.Name, parameter.Dimensions)
        {
            Requires.NotNull(parameter, nameof(parameter));
            Requires.NotNull(parameter.Dimensions, $"{nameof(parameter)}.{nameof(parameter.Dimensions)}");

            _parameter = parameter;
        }

        protected override Task SetValueAsync(IProperty property)
        {
            if (property is IEvaluatedProperty evaluatedProperty)
            {
                return evaluatedProperty.SetUnevaluatedValueAsync(_parameter.Value ?? string.Empty);
            }
            else
            {
                return property.SetValueAsync(_parameter.Value);
            }
        }
    }
}
