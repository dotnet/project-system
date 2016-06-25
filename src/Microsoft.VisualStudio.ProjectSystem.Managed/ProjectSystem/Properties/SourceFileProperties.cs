// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal class SourceFileProperties : IProjectProperties
    {
        private readonly IProjectPropertiesContext _projectPropertiesContext;
        private readonly Workspace _workspace;

        public SourceFileProperties(IProjectPropertiesContext projectPropertiesContext, Workspace workspace)
        {
            _projectPropertiesContext = projectPropertiesContext;
            _workspace = workspace;
        }

        public IProjectPropertiesContext Context => _projectPropertiesContext;

        public string FileFullPath => _projectPropertiesContext.File;

        public PropertyKind PropertyKind
        {
            get { throw new NotImplementedException(); }
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

        public async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            var currentProject = _workspace
                                 .CurrentSolution
                                 .Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _projectPropertiesContext.File))
                                 .FirstOrDefault();
            var compilation = await currentProject.GetCompilationAsync().ConfigureAwait(false);
            var assemblyAttributes = compilation.Assembly.GetAttributes();

            if (propertyName.Equals("Copyright"))
            {
                var assemblyCopyrightAttributeSymbol = compilation.GetTypeByMetadataName("System.Reflection.AssemblyCopyrightAttribute");
                var attribute = assemblyAttributes.FirstOrDefault(attrib => attrib.AttributeClass.Equals(assemblyCopyrightAttributeSymbol));
                if (attribute != null)
                {
                    var copyright = attribute.ConstructorArguments.FirstOrDefault().Value as string;
                    return copyright;
                }
            }

            return null;
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
