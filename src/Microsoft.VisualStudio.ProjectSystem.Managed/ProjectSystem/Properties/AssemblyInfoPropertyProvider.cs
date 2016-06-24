// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [Export("AssemblyInfo", typeof(IProjectPropertiesProvider))]
    [Export(typeof(IProjectPropertiesProvider))]
    [ExportMetadata("Name", "AssemblyInfo")]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class AssemblyInfoPropertyProvider : IProjectPropertiesProvider
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectPropertiesProvider _defaultProjectFilePropertiesProvider;

        [ImportingConstructor]
        internal AssemblyInfoPropertyProvider(UnconfiguredProject unconfiguredProject,
                                              [Import("Microsoft.VisualStudio.ProjectSystem.ProjectFile")] IProjectPropertiesProvider defaultProjectFilePropertiesProvider)
        {
            _unconfiguredProject = unconfiguredProject;
            _defaultProjectFilePropertiesProvider = defaultProjectFilePropertiesProvider;
        }

        public string DefaultProjectPath => _unconfiguredProject.FullPath;

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChanged
        {
            add { _defaultProjectFilePropertiesProvider.ProjectPropertyChanged += value; }
            remove { _defaultProjectFilePropertiesProvider.ProjectPropertyChanged -= value; }
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChangedOnWriter
        {
            add { _defaultProjectFilePropertiesProvider.ProjectPropertyChangedOnWriter += value; }
            remove { _defaultProjectFilePropertiesProvider.ProjectPropertyChangedOnWriter -= value; }
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChanging
        {
            add { _defaultProjectFilePropertiesProvider.ProjectPropertyChanging += value; }
            remove { _defaultProjectFilePropertiesProvider.ProjectPropertyChanging -= value; }
        }

        public IProjectProperties GetCommonProperties()
        {
            return _defaultProjectFilePropertiesProvider.GetCommonProperties();
        }

        public IProjectProperties GetItemProperties(string itemType, string item)
        {
            return _defaultProjectFilePropertiesProvider.GetItemProperties(itemType, item);
        }

        public IProjectProperties GetItemTypeProperties(string itemType)
        {
            return _defaultProjectFilePropertiesProvider.GetItemTypeProperties(itemType);
        }

        public IProjectProperties GetProperties(string file, string itemType, string item)
        {
            return new AssemblyInfoProperties(ProjectPropertiesContext.GetContext(_unconfiguredProject, itemType, item));
        }

        private class AssemblyInfoProperties : IProjectProperties
        {
            private readonly IProjectPropertiesContext _projectPropertiesContext;

            public AssemblyInfoProperties(IProjectPropertiesContext projectPropertiesContext)
            {
                this._projectPropertiesContext = projectPropertiesContext;
            }

            public IProjectPropertiesContext Context => _projectPropertiesContext;

            public string FileFullPath => _projectPropertiesContext.File;

            public PropertyKind PropertyKind
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public Task DeleteDirectPropertiesAsync()
            {
                throw new NotImplementedException();
            }

            public Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<string>> GetDirectPropertyNamesAsync()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
            {
                if (propertyName.Equals("Copyright"))
                {
                    return Task.FromResult("Copyright (c) Microsoft.");
                }

                return Task.FromResult("Unknown");
            }

            public Task<IEnumerable<string>> GetPropertyNamesAsync()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
            {
                return GetEvaluatedPropertyValueAsync(propertyName);
            }

            public Task<bool> IsValueInheritedAsync(string propertyName)
            {
                return Task.FromResult(false);
            }

            public Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
            {
                return Task.CompletedTask;
            }
        }
    }
}
