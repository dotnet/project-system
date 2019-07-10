// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Build.Execution;

#nullable disable

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("ProjectFileWithInterceptionViaSnapshot", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("ProjectFileWithInterceptionViaSnapshot", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "ProjectFileWithInterceptionViaSnapshot")]
    [ExportMetadata("HasEquivalentProjectInstancePropertiesProvider", true)]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class ProjectFileInterceptedViaSnapshotProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public ProjectFileInterceptedViaSnapshotProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider,
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            [ImportMany(ContractNames.ProjectPropertyProviders.ProjectFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
        }

        public override IProjectProperties GetCommonProperties()
        {
            IProjectProperties defaultProperties = base.GetCommonProperties();
            return InterceptProperties(defaultProperties);
        }

        public override IProjectProperties GetCommonProperties(ProjectInstance projectInstance)
        {
            IProjectProperties defaultProperties = base.GetCommonProperties(projectInstance);
            return InterceptProperties(defaultProperties);
        }
    }
}
