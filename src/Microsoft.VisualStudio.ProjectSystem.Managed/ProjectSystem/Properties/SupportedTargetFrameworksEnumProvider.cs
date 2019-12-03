// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Returns the support target frameworks for a particular project. The values are
    /// read from the SDK's SupportTargetFramework items.
    /// </summary>
    [ExportDynamicEnumValuesProvider("SupportedTargetFrameworksEnumProvider")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class SupportedTargetFrameworksEnumProvider : IDynamicEnumValuesProvider
    {
        private readonly IProjectAccessor _projectAccessor;
        private readonly ConfiguredProject _configuredProject;

        [ImportingConstructor]
        public SupportedTargetFrameworksEnumProvider(IProjectAccessor projectAccessor, ConfiguredProject configuredProject)
        {
            _projectAccessor = projectAccessor;
            _configuredProject = configuredProject;
        }

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(new SupportedTargetFrameworksEnumValuesGenerator(_projectAccessor, _configuredProject));
        }

        internal class SupportedTargetFrameworksEnumValuesGenerator : IDynamicEnumValuesGenerator
        {
            private const string SupportedTargetFrameworkItemName = "SupportedTargetFramework";
            private const string DisplayNameMetadataName = "DisplayName";

            private readonly IProjectAccessor _projectAccessor;
            private readonly ConfiguredProject _configuredProject;

            public SupportedTargetFrameworksEnumValuesGenerator(IProjectAccessor projectAccessor, ConfiguredProject configuredProject)
            {
                _projectAccessor = projectAccessor;
                _configuredProject = configuredProject;
            }

            public bool AllowCustomValues => false;

            public Task<ICollection<IEnumValue>> GetListedValuesAsync()
            {
                return _projectAccessor.OpenProjectForReadAsync(_configuredProject, project =>
                {
                    return (ICollection<IEnumValue>)project.GetItems(itemType: SupportedTargetFrameworkItemName)
                                                           .Select(i => new PageEnumValue(new EnumValue
                                                           {
                                                               Name = i.EvaluatedInclude,
                                                               DisplayName = i.GetMetadataValue(DisplayNameMetadataName)
                                                           }))
                                                           .ToArray<IEnumValue>();
                });
            }

            /// <summary>
            /// This property is only used to get the enum values, there is no actual
            /// persisted value in the project. So this method should never be called.
            /// </summary>
            /// <param name="userSuppliedValue"></param>
            public Task<IEnumValue?> TryCreateEnumValueAsync(string userSuppliedValue)
            {
                throw new NotImplementedException();
            }
        }
    }
}
