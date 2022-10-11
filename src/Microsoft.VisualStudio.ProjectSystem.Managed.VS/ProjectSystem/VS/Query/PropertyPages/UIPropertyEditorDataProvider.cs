// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve UI property value editors
    /// (<see cref="IUIPropertyEditorSnapshot"/>).
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="IUIPropertySnapshot.Editors"/>. Can also retrieve a <see
    /// cref="IUIPropertyEditorSnapshot"/> based on its ID.
    /// </remarks>
    [QueryDataProvider(UIPropertyEditorType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(UIPropertyType.TypeName, UIPropertyType.EditorsPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByIdDataProvider))]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class UIPropertyEditorDataProvider : QueryDataProviderBase, IQueryByIdDataProvider, IQueryByRelationshipDataProvider
    {
        [ImportingConstructor]
        public UIPropertyEditorDataProvider(IProjectServiceAccessor projectServiceAccessor)
            : base(projectServiceAccessor)
        {
        }

        public IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue> CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new UIPropertyEditorByIdDataProducer((IUIPropertyEditorPropertiesAvailableStatus)properties, ProjectService);
        }

        IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new UIPropertyEditorFromUIPropertyDataProducer((IUIPropertyEditorPropertiesAvailableStatus)properties);
        }
    }
}
