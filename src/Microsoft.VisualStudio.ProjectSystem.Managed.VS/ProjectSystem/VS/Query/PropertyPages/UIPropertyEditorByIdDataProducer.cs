// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Handles retrieving an <see cref="IUIPropertyEditor"/> base on an ID.
    /// </summary>
    internal class UIPropertyEditorByIdDataProducer : UIPropertyEditorDataProducer, IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue>
    {
        private readonly IProjectService2 _projectService;

        public UIPropertyEditorByIdDataProducer(IUIPropertyEditorPropertiesAvailableStatus properties, IProjectService2 projectService)
            : base(properties)
        {
            Requires.NotNull(projectService, nameof(projectService));
            _projectService = projectService;
        }

        public async Task SendRequestAsync(QueryProcessRequest<IReadOnlyCollection<EntityIdentity>> request)
        {
            Requires.NotNull(request, nameof(request));

            foreach (var requestId in request.RequestData)
            {
                if (requestId.KeysCount == 4
                    && requestId.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string path)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.PropertyPageName, out string propertyPageName)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.UIPropertyName, out string propertyName)
                    && requestId.TryGetValue(ProjectModelIdentityKeys.EditorName, out string editorName))
                {
                    try
                    {
                        if (_projectService.GetLoadedProject(path) is UnconfiguredProject project
                            && await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog
                            && projectCatalog.GetSchema(propertyPageName) is Rule rule
                            && rule.TryGetPropertyAndIndex(propertyName, out var property, out var index)
                            && property.ValueEditors.FirstOrDefault(ed => string.Equals(ed.EditorType, editorName)) is ValueEditor editor)
                        {
                            IEntityValue editorValue = await CreateEditorValueAsync(request.QueryExecutionContext.EntityRuntime, requestId, editor);
                            await ResultReceiver.ReceiveResultAsync(new QueryProcessResult<IEntityValue>(editorValue, request, ProjectModelZones.Cps));
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
