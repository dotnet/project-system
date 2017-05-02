using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using System.Linq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportDynamicEnumValuesProvider("SupportedTargetFrameworksEnumProvider")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicOrFSharp)]
    internal class SupportedTargetFrameworksEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly IProjectLockService _projectLockService;
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public SupportedTargetFrameworksEnumProvider(IProjectLockService projectLockService, ConfiguredProject configuredProject)
        {
            _projectLockService = projectLockService;
            _configuredProject = configuredProject;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair> options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new SupportedTargetFrameworksEnumValuesGenerator(_projectLockService, _configuredProject));
        }

        internal class SupportedTargetFrameworksEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private const string SupportedTargetFrameworkItemName = "SupportedTargetFramework";
            private const string DisplayNameMetadataName = "DisplayName";

            private readonly IProjectLockService _projectLockService;
            private readonly ConfiguredProject _configuredProject;

            public SupportedTargetFrameworksEnumValuesGenerator(IProjectLockService projectLockService, ConfiguredProject configuredProject)
            {
                _projectLockService = projectLockService;
                _configuredProject = configuredProject;
            }

            public bool AllowCustomValues => false;

            public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                var enumValues = new List<IEnumValue>();
                using (var access = await _projectLockService.ReadLockAsync())
                {
                    var project = await access.GetProjectAsync(_configuredProject);
                    var items = project.GetItems(itemType: SupportedTargetFrameworkItemName);

                    foreach (var item in items)
                    {
                        var val = new PageEnumValue(new EnumValue { Name = item.EvaluatedInclude, DisplayName = item.GetMetadataValue(DisplayNameMetadataName) });
                        enumValues.Add(val);
                    }
                }

                return enumValues;
            }

            public Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
            {
                throw new NotImplementedException();
            }
        }
    }
}
