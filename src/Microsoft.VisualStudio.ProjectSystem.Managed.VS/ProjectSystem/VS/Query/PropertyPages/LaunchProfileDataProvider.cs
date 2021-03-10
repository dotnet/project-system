// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation;
using Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;
using Microsoft.VisualStudio.ProjectSystem.Query.QueryExecution;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query.PropertyPages
{
    /// <summary>
    /// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve launch profile information
    /// (see <see cref="ILaunchProfile"/>) for a project.
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="IProject.LaunchProfiles"/>.
    /// </remarks>
    [QueryDataProvider(LaunchProfileType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(ProjectSystem.Query.ProjectModel.Metadata.ProjectType.TypeName, ProjectSystem.Query.ProjectModel.Metadata.ProjectType.LaunchProfilesPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class LaunchProfileDataProvider : QueryDataProviderBase, IQueryByRelationshipDataProvider
    {
        private readonly IPropertyPageQueryCacheProvider _queryCacheProvider;

        [ImportingConstructor]
        public LaunchProfileDataProvider(
            IProjectServiceAccessor projectServiceAccessor,
            IPropertyPageQueryCacheProvider queryCacheProvider)
            : base(projectServiceAccessor)
        {
            _queryCacheProvider = queryCacheProvider;
        }

        IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new LaunchProfileFromProjectDataProducer((ILaunchProfilePropertiesAvailableStatus)properties, _queryCacheProvider);
        }
    }
}
