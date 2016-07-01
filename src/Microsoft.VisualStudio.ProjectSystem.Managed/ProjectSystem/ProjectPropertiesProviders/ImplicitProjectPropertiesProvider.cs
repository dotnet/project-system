// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    /// <summary>
    /// Provides project properties that normally should not live in the project
    /// file but may be written from an external source (e.g. the solution file).
    /// This provider avoids writing these properties to the project file, but if
    /// they are already present there, it updates the value to keep in sync with
    /// the external source.
    /// </summary>
    [Export("ImplicitProjectFile", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "ImplicitProjectFile")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ImplicitProjectPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public ImplicitProjectPropertiesProvider(
            [Import(ContractNames.ProjectPropertyProviders.ProjectFile)] IProjectPropertiesProvider provider)
            : base(provider)
        {
        }

        public override IProjectProperties GetProperties(string file, string itemType, string item)
            => new ImplicitProjectProperties(DelegatedProvider.GetProperties(file, itemType, item));

        /// <summary>
        /// Implementation of IProjectProperties that avoids writing properties unless they
        /// already exist (i.e. are being updated) and delegates the rest of its operations
        /// to another IProjectProperties object
        /// </summary>
        private class ImplicitProjectProperties : DelegatedProjectPropertiesBase
        {
            public ImplicitProjectProperties(IProjectProperties properties)
                : base(properties)
            {
            }

            /// <summary>
            /// Only set properties that exist in the delegated properties object
            /// </summary>
            public override async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                var propertyNames = await DelegatedProperties.GetPropertyNamesAsync().ConfigureAwait(false);
                if (propertyNames.Contains(propertyName))
                {
                    // overwrite the property if it exists
                    await DelegatedProperties.SetPropertyValueAsync(propertyName, unevaluatedPropertyValue, dimensionalConditions).ConfigureAwait(false);
                }
            }
        }
    }
}
