using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export(typeof(IUnconfiguredProjectPropertyProviderService))]
    internal class UnconfiguredProjectPropertyProviderService : IUnconfiguredProjectPropertyProviderService
    {
        private readonly ProjectProperties _properties;

        [ImportingConstructor]
        public UnconfiguredProjectPropertyProviderService(ProjectProperties properties)
        {
            _properties = properties;
        }

        public async Task<string> GetTargetFrameworksAsync()
        {
            var configuration = await _properties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            return (string)await configuration.TargetFrameworks.GetValueAsync().ConfigureAwait(false);
        }
    }
}
