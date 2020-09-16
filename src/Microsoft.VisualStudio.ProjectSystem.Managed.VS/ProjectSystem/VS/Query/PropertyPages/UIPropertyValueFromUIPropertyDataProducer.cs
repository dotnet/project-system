// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIPropertyValue"/>s from an <see cref="IUIProperty"/>.
    /// </summary>
    internal class UIPropertyValueFromUIPropertyDataProducer : UIPropertyValueDataProducer, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        public UIPropertyValueFromUIPropertyDataProducer(IUIPropertyValuePropertiesAvailableStatus properties)
            : base(properties)
        {
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is (PropertyPageQueryCache context, Rule schema, string propertyName))
            {
                try
                {
                    if (await context.GetKnownConfigurationsAsync() is IImmutableSet<ProjectConfiguration> knownConfigurations)
                    {
                        foreach (var knownConfiguration in knownConfigurations)
                        {
                            if (await context.BindToRule(knownConfiguration, schema.Name) is IRule rule
                                && rule.GetProperty(propertyName) is ProjectSystem.Properties.IProperty property)
                            {
                                IEntityValue propertyValue = await CreateUIPropertyValueValueAsync(request.RequestData, knownConfiguration, property);
                                await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyValue, request, ProjectModelZones.Cps));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    request.QueryExecutionContext.ReportError(ex);
                }
            }

            await ResultReceiver.OnRequestProcessFinishedAsync(request);
        }
    }
}
