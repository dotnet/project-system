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

    internal abstract class ProjectSetUIPropertyValueActionBase : QueryDataProducerBase<IEntityValue>, IProjectUpdateActionExecutor, IQueryActionExecutor
    {
        private readonly IPropertyPageQueryCacheProvider _queryCacheProvider;
        private readonly string _pageName;
        private readonly string _propertyName;
        private readonly ReadOnlyCollection<ProjectSystem.Query.ProjectModelMethods.Actions.ConfigurationDimensionValue> _dimensions;

        private readonly Dictionary<string, List<IRule>> _rules = new(StringComparers.Paths);

        public ProjectSetUIPropertyValueActionBase(IPropertyPageQueryCacheProvider queryCacheProvider, string pageName, string propertyName, ReadOnlyCollection<ProjectSystem.Query.ProjectModelMethods.Actions.ConfigurationDimensionValue> dimensions)
        {
            _queryCacheProvider = queryCacheProvider;
            _pageName = pageName;
            _propertyName = propertyName;
            _dimensions = dimensions;
        }

        public async Task OnBeforeExecutingBatchAsync(IReadOnlyList<QueryProcessResult<IEntityValue>> allItems, CancellationToken cancellationToken)
        {
            foreach (QueryProcessResult<IEntityValue> item in allItems)
            {
                if (((IEntityValueFromProvider)item.Result).ProviderState is UnconfiguredProject project)
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

            Requires.NotNull(allItems, nameof(allItems));
        }

        public async Task ReceiveResultAsync(QueryProcessResult<IEntityValue> result)
        {
            Requires.NotNull(result, nameof(result));
            result.Request.QueryExecutionContext.CancellationToken.ThrowIfCancellationRequested();
            if (((IEntityValueFromProvider)result.Result).ProviderState is UnconfiguredProject project)
            {
                if (_rules.TryGetValue(project.FullPath, out List<IRule> boundRules))
                {
                    foreach (IRule boundRule in boundRules)
                    {
                        
                        if (boundRule.GetProperty(_propertyName) is IProperty property)
                        {
                            await SetValue(property);
                        }
                    }
                }
            }

            await ResultReceiver.ReceiveResultAsync(result);
        }

        public Task OnRequestProcessFinishedAsync(IQueryProcessRequest request)
        {
            _rules.Clear();
            return ResultReceiver.OnRequestProcessFinishedAsync(request);
        }

        protected abstract Task SetValue(IProperty property);
    }

    internal sealed class ProjectSetEvaluatedUIPropertyValueAction : ProjectSetUIPropertyValueActionBase
    {
        private readonly SetEvaluatedUIPropertyValue _parameter;

        public ProjectSetEvaluatedUIPropertyValueAction(IPropertyPageQueryCacheProvider queryCacheProvider, SetEvaluatedUIPropertyValue parameter)
            : base(queryCacheProvider, parameter.Page, parameter.Name, parameter.Dimensions)
        {
            Requires.NotNull(parameter, nameof(parameter));
            Requires.NotNull(parameter.Dimensions, $"{nameof(parameter)}.{nameof(parameter.Dimensions)}");

            _parameter = parameter;
        }

        protected override Task SetValue(IProperty property)
        {
            return property.SetValueAsync(_parameter.Value);
        }
    }

    internal sealed class ProjectSetUnevaluatedUIPropertyValueAction : ProjectSetUIPropertyValueActionBase
    {
        private readonly SetUnevaluatedUIPropertyValue _parameter;

        public ProjectSetUnevaluatedUIPropertyValueAction(IPropertyPageQueryCacheProvider queryCacheProvider, SetUnevaluatedUIPropertyValue parameter)
            : base(queryCacheProvider, parameter.Page, parameter.Name, parameter.Dimensions)
        {
            Requires.NotNull(parameter, nameof(parameter));
            Requires.NotNull(parameter.Dimensions, $"{nameof(parameter)}.{nameof(parameter.Dimensions)}");

            _parameter = parameter;
        }

        protected override Task SetValue(IProperty property)
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
