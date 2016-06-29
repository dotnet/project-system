// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// This class represents properties that are stored in the source code of the project.
    /// </summary>
    internal class SourceFileProperties : IProjectProperties
    {
        private readonly IProjectPropertiesContext _projectPropertiesContext;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;

        private readonly ImmutableDictionary<string, string> _attributeNameMap = new Dictionary<string, string>
        {
            { "Title",                      "System.Reflection.AssemblyTitleAttribute" },
            { "Description",                "System.Reflection.AssemblyDescriptionAttribute" },
            { "Company",                    "System.Reflection.AssemblyCompanyAttribute" },
            { "Product",                    "System.Reflection.AssemblyProductAttribute" },
            { "Copyright",                  "System.Reflection.AssemblyCopyrightAttribute" },
            { "Trademark",                  "System.Reflection.AssemblyTrademarkAttribute" },
            { "AssemblyVersion",            "System.Reflection.AssemblyVersionAttribute" },
            { "AssemblyFileVersion",        "System.Reflection.AssemblyFileVersionAttribute" },
            { "NeutralResourcesLanguage",   "System.Resources.NeutralResourcesLanguageAttribute" },
            { "AssemblyGuid",               "System.Runtime.InteropServices.GuidAttribute" },
            { "ComVisible",                 "System.Runtime.InteropServices.ComVisibleAttribute" }
        }.ToImmutableDictionary();

        public SourceFileProperties(IProjectPropertiesContext projectPropertiesContext, Workspace workspace, IProjectThreadingService threadingService)
        {
            _projectPropertiesContext = projectPropertiesContext;
            _workspace = workspace;
            _threadingService = threadingService;
        }

        public IProjectPropertiesContext Context => _projectPropertiesContext;

        public string FileFullPath => _projectPropertiesContext.File;

        public PropertyKind PropertyKind => PropertyKind.PropertyGroup;

        public Task<string> GetUnevaluatedPropertyValueAsync(string propertyName)
        {
            return GetEvaluatedPropertyValueAsync(propertyName);
        }

        public Task<bool> IsValueInheritedAsync(string propertyName)
        {
            return Task.FromResult(false);
        }

        public async Task<string> GetEvaluatedPropertyValueAsync(string propertyName)
        {
            if (_attributeNameMap.ContainsKey(propertyName))
            {
                var project = _workspace
                              .CurrentSolution
                              .Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _projectPropertiesContext.File))
                              .FirstOrDefault();
                if (project == null)
                {
                    return null;
                }

                var attribute = await GetAttributeAsync(propertyName, project).ConfigureAwait(false);
                if (attribute == null)
                {
                    return null;
                }

                return attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
            }

            return null;
        }

        public async Task SetPropertyValueAsync(string propertyName, string unevaluatedPropertyValue, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            if (_attributeNameMap.ContainsKey(propertyName))
            {
                var project = _workspace
                              .CurrentSolution
                              .Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _projectPropertiesContext.File))
                              .FirstOrDefault();
                if (project == null)
                {
                    return;
                }

                var attribute = await GetAttributeAsync(propertyName, project).ConfigureAwait(false);
                if (attribute == null)
                {
                    return;
                }

                var attributeNode = await attribute.ApplicationSyntaxReference.GetSyntaxAsync().ConfigureAwait(false);
                var syntaxGenerator = SyntaxGenerator.GetGenerator(project);
                var arguments = syntaxGenerator.GetAttributeArguments(attributeNode);

                // The attributes of interest to us have one argument. If there are more then we have broken code - don't change that.
                if (arguments.Count == 1)
                {
                    var argumentNode = arguments[0];
                    SyntaxNode newNode;
                    if (propertyName.Equals("ComVisible"))
                    {
                        newNode = syntaxGenerator.AttributeArgument(bool.Parse(unevaluatedPropertyValue) ? syntaxGenerator.TrueLiteralExpression() : syntaxGenerator.FalseLiteralExpression());
                    }
                    else
                    {
                        newNode = syntaxGenerator.AttributeArgument(syntaxGenerator.LiteralExpression(unevaluatedPropertyValue));
                    }

                    newNode = newNode.WithTriviaFrom(argumentNode);
                    var editor = await DocumentEditor.CreateAsync(project.GetDocument(attributeNode.SyntaxTree)).ConfigureAwait(false);
                    editor.ReplaceNode(argumentNode, newNode);

                    // Apply changes needs to happen on the UI Thread.
                    await _threadingService.SwitchToUIThread();
                    _workspace.TryApplyChanges(editor.GetChangedDocument().Project.Solution);
                }
            }
        }

        private async Task<AttributeData> GetAttributeAsync(string propertyName, Project project)
        {
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            var assemblyAttributes = compilation.Assembly.GetAttributes();

            var attributeTypeSymbol = compilation.GetTypeByMetadataName(_attributeNameMap[propertyName]);
            if (attributeTypeSymbol == null)
            {
                return null;
            }

            return assemblyAttributes.FirstOrDefault(attrib => attrib.AttributeClass.Equals(attributeTypeSymbol));
        }

        // There aren't any usages of the following methods and so they are unimplemented.
        public Task DeleteDirectPropertiesAsync()
        {
            throw new NotSupportedException();
        }

        public Task DeletePropertyAsync(string propertyName, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<string>> GetDirectPropertyNamesAsync()
        {
            throw new NotSupportedException();
        }

        public Task<IEnumerable<string>> GetPropertyNamesAsync()
        {
            throw new NotSupportedException();
        }
    }
}
