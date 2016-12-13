// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// This class represents properties corresponding to assembly attributes that are stored in the source code of the project.
    /// </summary>
    internal class SourceAssemblyAttributePropertyValueProvider
    {
        private readonly string _assemblyAttributeFullName;
        private readonly ILanguageServiceHost _languageServiceHost;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;

        public SourceAssemblyAttributePropertyValueProvider(
            string assemblyAttributeFullName,
            ILanguageServiceHost languageServiceHost,
            Workspace workspace,
            IProjectThreadingService threadingService)
        {
            _assemblyAttributeFullName = assemblyAttributeFullName;
            _languageServiceHost = languageServiceHost;
            _workspace = workspace;
            _threadingService = threadingService;
        }

        private Project GetActiveProject()
        {
            var activeConfiguredProject = _languageServiceHost.ActiveProjectContext;
            if (activeConfiguredProject == null)
            {
                return null;
            }

            return _workspace
                .CurrentSolution
                .Projects.Where(p => p.Id == ((AbstractProject)activeConfiguredProject).Id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the value of the property from the source assembly attribute.
        /// </summary>
        public async Task<string> GetPropertyValueAsync()
        {
            var project = GetActiveProject();
            if (project == null)
            {
                return null;
            }

            var attribute = await GetAttributeAsync(_assemblyAttributeFullName, project).ConfigureAwait(false);
            if (attribute == null)
            {
                return null;
            }

            return attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
        }

        /// <summary>
        /// Sets the value of the property in the source assembly attribute.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task SetPropertyValueAsync(string value)
        {
            var project = GetActiveProject();
            if (project == null)
            {
                return;
            }

            var attribute = await GetAttributeAsync(_assemblyAttributeFullName, project).ConfigureAwait(false);
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
                if (attribute.AttributeConstructor.Parameters.FirstOrDefault()?.Type.SpecialType == SpecialType.System_Boolean)
                {
                    newNode = syntaxGenerator.AttributeArgument(bool.Parse(value) ? syntaxGenerator.TrueLiteralExpression() : syntaxGenerator.FalseLiteralExpression());
                }
                else
                {
                    newNode = syntaxGenerator.AttributeArgument(syntaxGenerator.LiteralExpression(value));
                }

                newNode = newNode.WithTriviaFrom(argumentNode);
                var editor = await DocumentEditor.CreateAsync(project.GetDocument(attributeNode.SyntaxTree)).ConfigureAwait(false);
                editor.ReplaceNode(argumentNode, newNode);

                // Apply changes needs to happen on the UI Thread.
                await _threadingService.SwitchToUIThread();
                _workspace.TryApplyChanges(editor.GetChangedDocument().Project.Solution);
            }
        }

        /// <summary>
        /// Get the attribute corresponding to the given property from the given project.
        /// </summary>
        private static async Task<AttributeData> GetAttributeAsync(string assemblyAttributeFullName, Project project)
        {
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            var assemblyAttributes = compilation.Assembly.GetAttributes();

            var attributeTypeSymbol = compilation.GetTypeByMetadataName(assemblyAttributeFullName);
            if (attributeTypeSymbol == null)
            {
                return null;
            }

            return assemblyAttributes.FirstOrDefault(attrib => attrib.AttributeClass.Equals(attributeTypeSymbol));
        }
    }
}
