// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Frameworks;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving a set of <see cref="IUIPropertyEditor"/>s from an <see cref="IUIProperty"/>.
    /// </summary>
    internal class UIPropertyEditorFromUIPropertyDataProducer : UIPropertyEditorDataProducer, IQueryDataProducer<IEntityValue, IEntityValue>
    {
        public UIPropertyEditorFromUIPropertyDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties)
            : base(properties)
        {
        }

        public async Task SendRequestAsync(QueryProcessRequest<IEntityValue> request)
        {
            Requires.NotNull(request, nameof(request));

            if ((request.RequestData as IEntityValueFromProvider)?.ProviderState is (PropertyPageQueryCache context, Rule schema, string propertyName))
            {
                try
                {
                    var property = schema.GetProperty(propertyName);
                    foreach (var editor in property.ValueEditors)
                    {
                        IEntityValue editorValue = await CreateEditorValueAsync(request.RequestData, editor);
                        await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(editorValue, request, ProjectModelZones.Cps));
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
