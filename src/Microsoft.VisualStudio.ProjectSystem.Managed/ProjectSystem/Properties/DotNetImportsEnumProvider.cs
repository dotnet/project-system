// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework.XamlTypes;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[ExportDynamicEnumValuesProvider("DotNetImportsEnumProvider")]
[AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
internal class DotNetImportsEnumProvider : IDynamicEnumValuesProvider
{
    private readonly Imports _imports;

    [ImportingConstructor]
    public DotNetImportsEnumProvider(Imports imports)
    {
        _imports = imports;
    }
    public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
    {
        return Task.FromResult<IDynamicEnumValuesGenerator>(new DotNetImportsEnumGenerator(_imports));
    }
    
    private class DotNetImportsEnumGenerator : IDynamicEnumValuesGenerator
    {
        private readonly Imports _imports;
        
        public DotNetImportsEnumGenerator(Imports imports)
        {
            _imports = imports;

        }
        
        public Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            ImmutableArray<IEnumValue>.Builder importsArray = ImmutableArray.CreateBuilder<IEnumValue>(_imports.Count);
            foreach (string import in _imports)
            {
                importsArray.Add(new PageEnumValue(new EnumValue { Name = import, DisplayName = import}));
            }

            return Task.FromResult(importsArray.MoveToImmutable() as ICollection<IEnumValue>);
        }

        public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            if (userSuppliedValue.Length == 0)
            {
                return Task.FromResult<IEnumValue?>(null);
            }

            _imports.Add(userSuppliedValue);
            var value = new PageEnumValue(new EnumValue { Name = userSuppliedValue, DisplayName = userSuppliedValue });
            return Task.FromResult<IEnumValue?>(value);
        }

        public bool AllowCustomValues => true;
    }
}
