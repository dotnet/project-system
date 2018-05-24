// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("ProjectFileWithInterception", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [Export("ProjectFileWithInterception", typeof(IProjectInstancePropertiesProvider))]
    [Export(typeof(IProjectInstancePropertiesProvider))]
    [ExportMetadata("Name", "ProjectFileWithInterception")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal sealed class ProjectFileInterceptedProjectPropertiesProvider : InterceptedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public ProjectFileInterceptedProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider,
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectInstancePropertiesProvider instanceProvider,
            UnconfiguredProject project,
            [ImportMany(ContractNames.ProjectPropertyProviders.ProjectFile)]IEnumerable<Lazy<IInterceptingPropertyValueProvider, IInterceptingPropertyValueProviderMetadata>> interceptingValueProviders)
            : base(provider, instanceProvider, project, interceptingValueProviders)
        {
        }
    }
}
