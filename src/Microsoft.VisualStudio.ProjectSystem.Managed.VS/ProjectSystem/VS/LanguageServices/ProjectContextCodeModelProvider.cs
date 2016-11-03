// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    /// <summary>
    ///     Adapts CPS's <see cref="ICodeModelProvider"/> and <see cref="IProjectCodeModelProvider"/> to Roslyn's <see cref="ICodeModelFactory"/> implementation.
    /// </summary>
    [Export(typeof(ICodeModelProvider))]
    [Export(typeof(IProjectCodeModelProvider))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasicLanguageService)]
    internal class ProjectContextCodeModelProvider : ICodeModelProvider, IProjectCodeModelProvider
    {
        private readonly ICodeModelFactory _codeModelFactory;
        private readonly ILanguageServiceHost _languageServiceHost;

        [ImportingConstructor]
        public ProjectContextCodeModelProvider(ICodeModelFactory codeModelFactory, ILanguageServiceHost languageServiceHost)
        {
            Requires.NotNull(codeModelFactory, nameof(codeModelFactory));
            Requires.NotNull(languageServiceHost, nameof(languageServiceHost));

            _codeModelFactory = codeModelFactory;
            _languageServiceHost = languageServiceHost;
        }

        public CodeModel GetCodeModel(Project project)
        {
            Requires.NotNull(project, nameof(project));

            IWorkspaceProjectContext projectContext = _languageServiceHost.ActiveProjectContext;
            if (projectContext == null)
                return null;

            return _codeModelFactory.GetCodeModel(projectContext, project);
        }

        public FileCodeModel GetFileCodeModel(ProjectItem fileItem)
        {
            Requires.NotNull(fileItem, nameof(fileItem));

            IWorkspaceProjectContext projectContext = _languageServiceHost.ActiveProjectContext;
            if (projectContext == null)
                return null;

            return _codeModelFactory.GetFileCodeModel(projectContext, fileItem);
        }
    }
}
