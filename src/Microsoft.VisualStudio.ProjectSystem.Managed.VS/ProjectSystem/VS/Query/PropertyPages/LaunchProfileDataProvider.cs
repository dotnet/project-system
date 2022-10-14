// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve launch profile information
    /// (see <see cref="ILaunchProfile"/>) for a project.
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="Microsoft.VisualStudio.ProjectSystem.Query.IProjectSnapshot.LaunchProfiles"/>.
    /// </remarks>
    [QueryDataProvider(LaunchProfileType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(ProjectSystem.Query.Metadata.ProjectType.TypeName, ProjectSystem.Query.Metadata.ProjectType.LaunchProfilesPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByIdDataProvider))]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class LaunchProfileDataProvider : QueryDataProviderBase, IQueryByIdDataProvider, IQueryByRelationshipDataProvider
    {

        [ImportingConstructor]
        public LaunchProfileDataProvider(
            IProjectServiceAccessor projectServiceAccessor)
            : base(projectServiceAccessor)
        {
        }

        public IQueryDataProducer<IReadOnlyCollection<EntityIdentity>, IEntityValue> CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new LaunchProfileByIdDataProducer((ILaunchProfilePropertiesAvailableStatus)properties, ProjectService);
        }

        IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new LaunchProfileFromProjectDataProducer((ILaunchProfilePropertiesAvailableStatus)properties);
        }
    }
}
