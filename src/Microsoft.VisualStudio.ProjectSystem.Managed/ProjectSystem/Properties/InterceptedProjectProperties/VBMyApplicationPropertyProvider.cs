using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("VBMyApplicationProperty", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("VBMyApplicationProperty", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "VBMyApplicationProperty")]
    internal sealed class VBMyApplicationPropertyProvider : InterceptedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public VBMyApplicationPropertyProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider,
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            [ImportMany(ContractNames.ProjectPropertyProviders.ProjectFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
        }

        public override IProjectProperties GetCommonProperties()
        {
            return base.GetCommonProperties();
        }
        public override IProjectProperties GetCommonProperties(ProjectInstance projectInstance)
        {
            return base.GetCommonProperties(projectInstance);
        }

        public override IProjectProperties GetItemProperties(ITaskItem taskItem)
        {
            return base.GetItemProperties(taskItem);
        }

        public override IProjectProperties GetItemProperties(ProjectInstance projectInstance, string itemType, string itemName)
        {
            return base.GetItemProperties(projectInstance, itemType, itemName);
        }

        public override IProjectProperties GetItemProperties(string itemType, string item)
        {
            return base.GetItemProperties(itemType, item);
        }

        public override IProjectProperties GetItemTypeProperties(ProjectInstance projectInstance, string itemType)
        {
            return base.GetItemTypeProperties(projectInstance, itemType);
        }

        public override IProjectProperties GetItemTypeProperties(string itemType)
        {
            return base.GetItemTypeProperties(itemType);
        }

        public override IProjectProperties GetProperties(string file, string itemType, string item)
        {
            return base.GetProperties(file, itemType, item);
        }
    }
}
