// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve property information (see
    /// <see cref="IUIPropertySnapshot"/>).
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="Properties"/>.
    /// Note this is almost identical to the <see cref="UIPropertyDataProvider"/>; the only reason we have both is that
    /// <see cref="RelationshipQueryDataProviderAttribute"/> cannot be applied multiple times to the same type, so we
    /// can't have one type that handles multiple relationships.
    /// </remarks>
    [QueryDataProvider(UIPropertyType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(LaunchProfileType.TypeName, LaunchProfileType.PropertiesPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class LaunchProfileUIPropertyDataProvider : QueryDataProviderBase, IQueryByRelationshipDataProvider
    {
        [ImportingConstructor]
        public LaunchProfileUIPropertyDataProvider(IProjectServiceAccessor projectServiceAccessor)
            : base(projectServiceAccessor)
        {
        }

        IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new UIPropertyFromRuleDataProducer((IUIPropertyPropertiesAvailableStatus)properties);
        }
    }
}
