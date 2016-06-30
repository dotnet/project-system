using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
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
    [Export("Implicit", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "Implicit")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ImplicitProjectPropertiesProvider : DelegatedProjectPropertiesProviderBase
    {
        [ImportingConstructor]
        public ImplicitProjectPropertiesProvider(
            [Import("Microsoft.VisualStudio.ProjectSystem.ProjectFile")] IProjectPropertiesProvider provider)
            : base(provider)
        {
        }

        public override IProjectProperties GetProperties(string file, string itemType, string item)
            => new ImplicitProjectProperties(DelegatedProvider.GetProperties(file, itemType, item));

        private class ImplicitProjectProperties : DelegatedProjectPropertiesBase
        {
            public ImplicitProjectProperties(IProjectProperties properties)
                : base(properties)
            {
            }

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
