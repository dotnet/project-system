using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Returns the support target frameworks for a particular project. The values are
    /// read from the SDK's SupportTargetFramework items.
    /// </summary>
    [ExportDynamicEnumValuesProvider("SupportedTargetFrameworksEnumProvider")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    internal class SupportedTargetFrameworksEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly IProjectXmlAccessor _projectXmlAccessor;
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public SupportedTargetFrameworksEnumProvider(IProjectXmlAccessor projectXmlAccessor, ConfiguredProject configuredProject)
        {
            Requires.NotNull(projectXmlAccessor, nameof(projectXmlAccessor));
            Requires.NotNull(configuredProject, nameof(configuredProject));
            _projectXmlAccessor = projectXmlAccessor;
            _configuredProject = configuredProject;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new SupportedTargetFrameworksEnumValuesGenerator(_projectXmlAccessor, _configuredProject));
        }

        internal class SupportedTargetFrameworksEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private const string SupportedTargetFrameworkItemName = "SupportedTargetFramework";
            private const string DisplayNameMetadataName = "DisplayName";

            private readonly IProjectXmlAccessor _projectXmlAccessor;
            private readonly ConfiguredProject _configuredProject;

            public SupportedTargetFrameworksEnumValuesGenerator(IProjectXmlAccessor projectXmlAccessor, ConfiguredProject configuredProject)
            {
                _projectXmlAccessor = projectXmlAccessor;
                _configuredProject = configuredProject;
            }

            public bool AllowCustomValues => false;

            public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                var enumValues = new List<IEnumValue>();
                var items = await _projectXmlAccessor.GetItems(_configuredProject, itemType: SupportedTargetFrameworkItemName, metadataName: DisplayNameMetadataName).ConfigureAwait(false);

                foreach ((string evaluatedInclude, string metadataValue) in items)
                {
                    var val = new PageEnumValue(new EnumValue { Name = evaluatedInclude, DisplayName = metadataValue });
                    enumValues.Add(val);
                }

                return enumValues;
            }

            /// <summary>
            /// This property is only used to get the enum values, there is no actual
            /// persisted value in the project. So this method should never be called.
            /// </summary>
            /// <param name="userSuppliedValue"></param>
            /// <returns></returns>
            public Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
            {
                throw new NotImplementedException();
            }
        }
    }
}
