// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.References.Roslyn;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal partial class ReferenceCleanupService
    {
        private abstract class AbstractReferenceHandler
        {
            private readonly ProjectSystemReferenceType _referenceType;
            private readonly string _schema;
            private readonly string _referenceName;
            private readonly string _itemSpecification;

            private IProjectRuleSnapshot? _snapshotReferences;

            protected AbstractReferenceHandler(ProjectSystemReferenceType referenceType, string schema, string referenceName, string itemSpecification)
            {
                _referenceType = referenceType;
                _schema = schema;
                _referenceName = referenceName;
                _itemSpecification = itemSpecification;
            }

            public abstract Task<bool> RemoveReferenceAsync(ConfiguredProject configuredProject, ProjectSystemReferenceInfo reference);

            public IProjectRuleSnapshot GetProjectSnapshot(ConfiguredProject selectedConfiguredProject)
            {
                IProjectSubscriptionService? serviceSubscription = selectedConfiguredProject.Services.ProjectSubscription;
                Assumes.Present(serviceSubscription);

                serviceSubscription.ProjectRuleSource.SourceBlock.TryReceive(Filter, out IProjectVersionedValue<IProjectSubscriptionUpdate> item);

                _snapshotReferences = item.Value.CurrentState[_schema];

                return _snapshotReferences;
            }

            private static bool Filter(IProjectVersionedValue<IProjectSubscriptionUpdate> obj) => true;

            public List<ProjectSystemReferenceInfo> GetReferences()
            {
                var references = new List<ProjectSystemReferenceInfo>();

                if (_snapshotReferences is null)
                {
                    return references;
                }

                foreach (var item in _snapshotReferences.Items)
                {
                    string treatAsUsed = GetAttributeTreatAsUsed(item);
                    string itemSpecification = GetAttributeItemSpecification(item);

                    references.Add(new ProjectSystemReferenceInfo(_referenceType, itemSpecification, treatAsUsed == bool.TrueString));
                }

                return references;
            }

            private static string GetAttributeTreatAsUsed(KeyValuePair<string, IImmutableDictionary<string, string>> item)
            {
                item.Value.TryGetValue(ProjectReference.TreatAsUsedProperty, out string treatAsUsed);
                treatAsUsed = string.IsNullOrEmpty(treatAsUsed) ? bool.FalseString : treatAsUsed;

                return treatAsUsed;
            }

            private string GetAttributeItemSpecification(KeyValuePair<string, IImmutableDictionary<string, string>> item)
            {
                item.Value.TryGetValue(_itemSpecification, out string itemSpecification);

                return itemSpecification;
            }

            public async Task<bool> UpdateReferenceAsync(ConfiguredProject activeConfiguredProject, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken)
            {
                bool wasUpdated = false;

                var projectAccessor = activeConfiguredProject.Services.ExportProvider.GetExportedValue<IProjectAccessor>();

                string newValue = referenceUpdate.Action == ProjectSystemUpdateAction.TreatAsUsed ? bool.TrueString : bool.FalseString;

                await projectAccessor.OpenProjectForWriteAsync(activeConfiguredProject, project =>
                {
                    var items = project.GetItemsByEvaluatedInclude(referenceUpdate.ReferenceInfo.ItemSpecification);

                    try
                    {
                        var item = items.Where(i =>
                                string.Compare(i.ItemType, _referenceName, StringComparison.OrdinalIgnoreCase) == 0)
                            .First();

                        if (item != null)
                        {
                            item.SetMetadataValue(ProjectReference.TreatAsUsedProperty, newValue);

                            wasUpdated = true;
                        }
                    }
                    catch
                    {
                    }
                }, cancellationToken : cancellationToken);

                return wasUpdated;
            }
        }

        private class ProjectAbstractReferenceHandler : AbstractReferenceHandler
        {
            internal ProjectAbstractReferenceHandler() : base(ProjectSystemReferenceType.Project, ProjectReference.SchemaName, ProjectReference.SchemaName, ProjectReference.IdentityProperty)
            { }

            public override async Task<bool> RemoveReferenceAsync(ConfiguredProject configuredProject, ProjectSystemReferenceInfo reference)
            {
                Assumes.Present(configuredProject);
                Assumes.Present(configuredProject.Services);
                Assumes.Present(configuredProject.Services.ProjectReferences);

                await configuredProject.Services.ProjectReferences.RemoveAsync(reference.ItemSpecification);

                return true;
            }
        }

        private class PackageAbstractReferenceHandler : AbstractReferenceHandler
        {
            internal PackageAbstractReferenceHandler() : base(ProjectSystemReferenceType.Package, PackageReference.SchemaName, PackageReference.SchemaName, PackageReference.NameProperty)
            { }

            public override async Task<bool> RemoveReferenceAsync(ConfiguredProject configuredProject, ProjectSystemReferenceInfo reference)
            {
                Assumes.Present(configuredProject);
                Assumes.Present(configuredProject.Services);
                Assumes.Present(configuredProject.Services.PackageReferences);

                await configuredProject.Services.PackageReferences.RemoveAsync(reference.ItemSpecification);

                return true;
            }
        }

        private class AssemblyAbstractReferenceHandler : AbstractReferenceHandler
        {
            internal AssemblyAbstractReferenceHandler() : base(ProjectSystemReferenceType.Assembly, AssemblyReference.SchemaName, "Reference", AssemblyReference.IdentityProperty)
            { }

            public override async Task<bool> RemoveReferenceAsync(ConfiguredProject configuredProject, ProjectSystemReferenceInfo reference)
            {
                Assumes.Present(configuredProject);
                Assumes.Present(configuredProject.Services);
                Assumes.Present(configuredProject.Services.AssemblyReferences);

                var assemblyName = new AssemblyName(reference.ItemSpecification);

                await configuredProject.Services.AssemblyReferences.RemoveAsync(assemblyName, null);

                return true;
            }
        }
    }
}
