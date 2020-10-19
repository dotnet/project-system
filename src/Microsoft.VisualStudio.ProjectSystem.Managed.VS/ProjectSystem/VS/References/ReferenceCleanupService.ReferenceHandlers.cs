// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.References.Roslyn;

namespace Microsoft.VisualStudio.ProjectSystem.VS.References
{
    internal partial class ReferenceCleanupService
    {
        private abstract class ReferenceHandler
        {
            protected readonly ReferenceType _referenceType;
            protected readonly string _schema;
            private IProjectRuleSnapshot _snapshotReferences;

            protected ReferenceHandler(ReferenceType referenceType, string schema)
            {
                _referenceType = referenceType;
                _schema = schema;
            }

            public IProjectRuleSnapshot GetProjectSnapshot(ConfiguredProject selectedConfiguredProject)
            {
                IProjectSubscriptionService? serviceSubscription = selectedConfiguredProject.Services.ProjectSubscription;
                Assumes.Present(serviceSubscription);

                serviceSubscription.ProjectRuleSource.SourceBlock.TryReceive(filter, out IProjectVersionedValue<IProjectSubscriptionUpdate> item);

                _snapshotReferences = item.Value.CurrentState[_schema];

                return _snapshotReferences;
            }

            private static bool filter(IProjectVersionedValue<IProjectSubscriptionUpdate> obj)
            {
                return true;
            }

            public void GetAndAppendReferences(List<Reference> references)
            {
                foreach (var item in _snapshotReferences.Items)
                {
                    string treatAsUsed = GetAttributeTreatAsUsed(item);

                    string itemSpecification = GetAttributeItemSpecification(item);

                    references.Add(new Reference(_referenceType, itemSpecification, treatAsUsed == "True"));
                }
            }

            private string GetAttributeTreatAsUsed(KeyValuePair<string, IImmutableDictionary<string, string>> item)
            {
                item.Value.TryGetValue("TreatAsUsed", out string treatAsUsed);
                return treatAsUsed;
            }

            protected abstract string GetAttributeItemSpecification(KeyValuePair<string, IImmutableDictionary<string, string>> item);

            public abstract void RemoveReference(ConfiguredProject configuredProject, Reference reference);
            public abstract void AddReference(ConfiguredProject configuredProject, Reference reference);
            public abstract Task UpdateReferenceAsync(ConfiguredProject configuredProject, Reference reference);
        }

        private class ProjectReferenceHandler : ReferenceHandler
        {
            internal ProjectReferenceHandler() : base(ReferenceType.Project, ProjectReference.SchemaName)
            { }

            public override void AddReference(ConfiguredProject configuredProject, Reference reference)
            {
                Assumes.Present(configuredProject.Services.ProjectReferences);
                configuredProject.Services.ProjectReferences.AddAsync(reference.ItemSpecification);
            }

            public override void RemoveReference(ConfiguredProject configuredProject, Reference reference)
            {
                Assumes.Present(configuredProject.Services.ProjectReferences);
                configuredProject.Services.ProjectReferences.RemoveAsync(reference.ItemSpecification);

            }

            public override Task UpdateReferenceAsync(ConfiguredProject configuredProject, Reference reference)
            {
                throw new NotImplementedException();
            }

            protected override string GetAttributeItemSpecification(KeyValuePair<string, IImmutableDictionary<string, string>> item)
            {
                item.Value.TryGetValue("Identity", out string itemSpecification);
                return itemSpecification;
            }
        }

        private class PackageReferenceHandler : ReferenceHandler
        {
            internal PackageReferenceHandler() : base(ReferenceType.Package, PackageReference.SchemaName)
            { }

            public override void AddReference(ConfiguredProject configuredProject, Reference reference)
            {
                Assumes.Present(configuredProject.Services.PackageReferences);
                string packageIdentity = reference.ItemSpecification;
                string version = "";
                configuredProject.Services.PackageReferences.AddAsync(packageIdentity, version);
            }

            public override void RemoveReference(ConfiguredProject configuredProject, Reference reference)
            {
                Assumes.Present(configuredProject.Services.PackageReferences);
                configuredProject.Services.PackageReferences.RemoveAsync(reference.ItemSpecification);
            }

            public override Task UpdateReferenceAsync(ConfiguredProject configuredProject, Reference reference)
            {
                throw new NotImplementedException();
            }

            protected override string GetAttributeItemSpecification(KeyValuePair<string, IImmutableDictionary<string, string>> item)
            {
                item.Value.TryGetValue("Name", out string itemSpecification);
                return itemSpecification;
            }
        }

        private class AssemblyReferenceHandler : ReferenceHandler
        {
            internal AssemblyReferenceHandler() : base(ReferenceType.Assembly, AssemblyReference.SchemaName)
            { }

            public override void AddReference(ConfiguredProject configuredProject, Reference reference)
            {
                Assumes.Present(configuredProject.Services.AssemblyReferences);
                AssemblyName assemblyName;
                string assemblyPath = "";
                //_configuredProject.Services.AssemblyReferences.AddAsync(assemblyName: assemblyName, assemblyPath);
            }

            public override void RemoveReference(ConfiguredProject configuredProject, Reference reference)
            {
                Assumes.Present(configuredProject.Services.PackageReferences);
                configuredProject.Services.AssemblyReferences.RemoveAsync(null, reference.ItemSpecification);
            }

            public override Task UpdateReferenceAsync(ConfiguredProject configuredProject, Reference reference)
            {
                throw new NotImplementedException();
            }

            protected override string GetAttributeItemSpecification(KeyValuePair<string, IImmutableDictionary<string, string>> item)
            {
                item.Value.TryGetValue("SDKName", out string itemSpecification);
                return itemSpecification;
            }
        }

        private class SdkReferenceHandler : ReferenceHandler
        {
            internal SdkReferenceHandler() : base(ReferenceType.Unknown, SdkReference.SchemaName)
            { }

            public override void AddReference(ConfiguredProject configuredProject, Reference reference)
            {
                throw new NotImplementedException();
            }

            public override void RemoveReference(ConfiguredProject configuredProject, Reference reference)
            {
                // Do not remove Sdks
            }

            public override Task UpdateReferenceAsync(ConfiguredProject configuredProject, Reference reference)
            {
                throw new NotImplementedException();
            }

            protected override string GetAttributeItemSpecification(KeyValuePair<string, IImmutableDictionary<string, string>> item)
            {
                item.Value.TryGetValue("Name", out string itemSpecification);
                return itemSpecification;
            }
        }
    }
}
