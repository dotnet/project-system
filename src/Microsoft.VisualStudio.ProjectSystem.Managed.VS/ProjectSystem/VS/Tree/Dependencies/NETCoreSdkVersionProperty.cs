// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [Export(typeof(INETCoreSdkVersionProperty))]
    internal class NETCoreSdkVersionProperty : INETCoreSdkVersionProperty
    {
        private readonly ProjectProperties _projectProperties;

        [ImportingConstructor]
        public NETCoreSdkVersionProperty(ProjectProperties projectProperties)
        {
            _projectProperties = projectProperties;
        }

        public async Task<string> GetValueAsync()
        {
            ConfigurationGeneral projectProperties = await _projectProperties.GetConfigurationGeneralPropertiesAsync().ConfigureAwait(false);
            return (string) await (projectProperties?.NETCoreSdkVersion?.GetValueAsync()).ConfigureAwait(false);
        }
    }
}
