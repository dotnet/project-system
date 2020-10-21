// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IUIPropertyEditor"/> base on an ID.
    /// </summary>
    internal class UIPropertyEditorByIdDataProducer : QueryDataProducerBase<IEntityValue>, IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue>
    {
        private readonly IUIPropertyEditorPropertiesAvailableStatus _properties;
        private readonly IProjectService2 _projectService;

        public UIPropertyEditorByIdDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties, IProjectService2 projectService)
        {
            Requires.NotNull(properties, nameof(properties));
            Requires.NotNull(projectService, nameof(projectService));
            _properties = properties;
            _projectService = projectService;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IReadOnlyCollection<EntityIdentity>> request)
        {
            Requires.NotNull(request, nameof(request));

            foreach (EntityIdentity requestId in request.RequestData)
            {
                if (requestId.KeysCount == 4
                    && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string path)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.UIPropertyName, out string propertyName)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.EditorName, out string editorName))
                {
                    try
                    {
                        IEntityValue? propertyEditor = await UIPropertyEditorDataProducer.CreateEditorValueAsync(
                            request.QueryExecutionContext.EntityRuntime,
                            requestId,
                            _projectService,
                            path,
                            propertyPageName,
                            propertyName,
                            editorName,
                            _properties);

                        if (propertyEditor is not null)
                        {
                            await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(propertyEditor, request, ProjectModelZones.Cps));
                        }
                    }
                    catch (Exception ex)
                    {
                        request.QueryExecutionContext.ReportError(ex);
                    }
                }
            }

            await ResultReceiver.OnRequestProcessFinishedAsync(request);
        }
    }
}
