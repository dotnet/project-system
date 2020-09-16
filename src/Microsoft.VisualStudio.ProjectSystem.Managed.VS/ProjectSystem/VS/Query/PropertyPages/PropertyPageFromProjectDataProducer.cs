// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
    /// Handles retrieving a set of <see cref="IPropertyPage"/>s from an <see cref="IProject"/>.
    /// </summary>
    internal class PropertyPageFromProjectDataProducer : PropertyPageDataProducer, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        public PropertyPageFromProjectDataProducer(IPropertyPagePropertiesAvailableStatus properties)
            : base(properties)
        {
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));

            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is UnconfiguredProject project)
            {
                try
                {
                    if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog)
                    {
                        var propertyPageQueryContext = new PropertyPageQueryCache(project);
                        foreach (var schemaName in projectCatalog.GetProjectLevelPropertyPagesSchemas())
                        {
                            if (projectCatalog.GetSchema(schemaName) is Rule rule
                                && !rule.PropertyPagesHidden)
                            {
                                IEntityValue propertyPageValue = await CreatePropertyPageValueAsync(request.RequestData, propertyPageQueryContext, rule);
                                await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyPageValue, request, ProjectModelZones.Cps));
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
