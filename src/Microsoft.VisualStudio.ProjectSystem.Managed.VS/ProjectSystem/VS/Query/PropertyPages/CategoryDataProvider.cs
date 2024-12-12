﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query;

/// <summary>
/// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve category information (<see
/// cref="ICategory"/>).
/// </summary>
/// <remarks>
/// Responsible for populating <see cref="IPropertyPageSnapshot.Categories"/>. Can also retrieve a <see cref="ICategory"/>
/// based on its ID.
/// Note this is almost identical to the <see cref="LaunchProfileCategoryDataProvider"/>; the only reason we have
/// both is that <see cref="RelationshipQueryDataProviderAttribute"/> cannot be applied multiple times to the same
/// type, so we can't have one type that handles multiple relationships.
/// </remarks>
[QueryDataProvider(CategoryType.TypeName, ProjectModel.ModelName)]
[RelationshipQueryDataProvider(PropertyPageType.TypeName, PropertyPageType.CategoriesPropertyName)]
[QueryDataProviderZone(ProjectModelZones.Cps)]
[Export(typeof(IQueryByIdDataProvider))]
[Export(typeof(IQueryByRelationshipDataProvider))]
internal class CategoryDataProvider : QueryDataProviderBase, IQueryByIdDataProvider, IQueryByRelationshipDataProvider
{
    [ImportingConstructor]
    public CategoryDataProvider(IProjectServiceAccessor projectServiceAccessor)
        : base(projectServiceAccessor)
    {
    }

    public IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue> CreateQueryDataSource(IPropertiesAvailableStatus properties)
    {
        return new CategoryByIdDataProducer((ICategoryPropertiesAvailableStatus)properties, ProjectService);
    }

    IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
    {
        return new CategoryFromRuleDataProducer((ICategoryPropertiesAvailableStatus)properties);
    }
}
