// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query.PropertyPages
{
    /// <summary>
    /// Creates <see cref="IQueryDataProducer{TRequest, TResult}"/> instances that retrieve launch profile type information
    /// (see <see cref="ILaunchProfileTypeSnapshot"/>) for a project.
    /// </summary>
    /// <remarks>
    /// Responsible for populating <see cref="Microsoft.VisualStudio.ProjectSystem.Query.IProjectSnapshot.LaunchProfiles"/>.
    /// </remarks>
    [QueryDataProvider(LaunchProfileTypeType.TypeName, ProjectModel.ModelName)]
    [RelationshipQueryDataProvider(ProjectSystem.Query.Metadata.ProjectType.TypeName, ProjectSystem.Query.Metadata.ProjectType.LaunchProfileTypesPropertyName)]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryByRelationshipDataProvider))]
    internal class LaunchProfileTypeDataProvider : QueryDataProviderBase, IQueryByRelationshipDataProvider
    {
        [ImportingConstructor]
        public LaunchProfileTypeDataProvider(
            IProjectServiceAccessor projectServiceAccessor)
            : base(projectServiceAccessor)
        {
        }

        IQueryDataProducer<IEntityValue, IEntityValue> IQueryByRelationshipDataProvider.CreateQueryDataSource(IPropertiesAvailableStatus properties)
        {
            return new LaunchProfileTypeFromProjectDataProducer((ILaunchProfileTypePropertiesAvailableStatus)properties);
        }
    }

    internal class LaunchProfileTypeFromProjectDataProducer : QueryDataFromProviderStateProducerBase<UnconfiguredProject>
    {
        private readonly ILaunchProfileTypePropertiesAvailableStatus _properties;

        public LaunchProfileTypeFromProjectDataProducer(ILaunchProfileTypePropertiesAvailableStatus properties)
        {
            _properties = properties;
        }

        protected override Task<IEnumerable<IEntityValue>> CreateValuesAsync(IQueryExecutionContext queryExecutionContext, IEntityValue parent, UnconfiguredProject providerState)
        {
            return CreateLaunchProfileTypeValuesAsync(parent, providerState);
        }

        private async Task<IEnumerable<IEntityValue>> CreateLaunchProfileTypeValuesAsync(IEntityValue parent, UnconfiguredProject project)
        {
            if (await project.GetProjectLevelPropertyPagesCatalogAsync() is IPropertyPagesCatalog projectCatalog)
            {
                return createLaunchProfileTypeValues();
            }

            return Enumerable.Empty<IEntityValue>();

            IEnumerable<IEntityValue> createLaunchProfileTypeValues()
            {
                foreach (Rule rule in DebugUtilities.GetDebugChildRules(projectCatalog))
                {
                    if (rule.Metadata.TryGetValue("CommandName", out object? commandNameObj)
                        && commandNameObj is string commandName)
                    {
                        IEntityValue launchProfileTypeValue = CreateLaunchProfileTypeValue(parent, commandName, rule);
                        yield return launchProfileTypeValue;
                    }
                }
            }
        }

        private IEntityValue CreateLaunchProfileTypeValue(IEntityValue parent, string commandName, Rule rule)
        {
            EntityIdentity identity = new(
                ((IEntityWithId)parent).Id,
                new Dictionary<string, string>
                {
                    { ProjectModelIdentityKeys.LaunchProfileTypeName, commandName }
                });

            return CreateLaunchProfileTypeValue(parent.EntityRuntime, identity, commandName, rule);
        }

        private IEntityValue CreateLaunchProfileTypeValue(IEntityRuntimeModel entityRuntime, EntityIdentity identity, string commandName, Rule rule)
        {
            LaunchProfileTypeSnapshot newLaunchProfileType = new(entityRuntime, identity, new LaunchProfileTypePropertiesAvailableStatus());

            if (_properties.CommandName)
            {
                newLaunchProfileType.CommandName = commandName;
            }

            if (_properties.DisplayName)
            {
                newLaunchProfileType.DisplayName = rule.DisplayName ?? commandName;
            }

            if (_properties.HelpUrl)
            {
                if (rule.Metadata.TryGetValue("HelpUrl", out object? helpUrlObj)
                            && helpUrlObj is string helpUrlString)
                {
                    newLaunchProfileType.HelpUrl = helpUrlString;
                }
                else
                {
                    newLaunchProfileType.HelpUrl = string.Empty;
                }
            }

            if (_properties.ImageMoniker)
            {
                if (rule.Metadata.TryGetValue("ImageMonikerGuid", out object? imageMonikerGuidObj)
                            && imageMonikerGuidObj is Guid imageMonikerGuid
                            && rule.Metadata.TryGetValue("ImageMonikerId", out object? imageMonikerIdObj)
                            && imageMonikerIdObj is int imageMonikerId)
                {
                    newLaunchProfileType.ImageMoniker = new ImageMoniker { Guid = imageMonikerGuid, Id = imageMonikerId };
                }
                else
                {
                    newLaunchProfileType.ImageMoniker = KnownMonikers.SettingsGroup;
                }
            }

            return newLaunchProfileType;
        }
    }
}
