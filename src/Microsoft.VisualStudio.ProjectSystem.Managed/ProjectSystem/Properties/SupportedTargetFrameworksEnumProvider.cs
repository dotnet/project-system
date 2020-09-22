// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Versioning;
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
                    string identifier = project.GetPropertyValue(ConfigurationGeneral.TargetFrameworkIdentifierProperty);

                    return (ICollection<IEnumValue>)project.GetItems(itemType: SupportedTargetFrameworkItemName)
                                                           .Where(i => CanRetargetTo(identifier, i.EvaluatedInclude))
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

            private static bool CanRetargetTo(string identifier, string moniker)
            {
                // We currently limit retargeting between target framework families as the SDK lists all available 
                // supported frameworks, not just the ones that are supported. https://github.com/dotnet/project-system/issues/3024 
                // is tracking making this better.

                try
                {
                    var name = new FrameworkName(moniker);

                    return StringComparers.FrameworkIdentifiers.Equals(name.Identifier, identifier);
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }
    }
}
