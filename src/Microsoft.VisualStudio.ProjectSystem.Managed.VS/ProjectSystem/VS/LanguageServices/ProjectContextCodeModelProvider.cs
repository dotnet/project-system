// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
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
    [AppliesTo(ProjectCapability.DotNetLanguageService)]
    internal class ProjectContextCodeModelProvider : ICodeModelProvider, IProjectCodeModelProvider
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly ICodeModelFactory _codeModelFactory;
        private readonly IActiveWorkspaceProjectContextHost _projectContextHost;

        [ImportingConstructor]
        public ProjectContextCodeModelProvider(IProjectThreadingService threadingService, ICodeModelFactory codeModelFactory, IActiveWorkspaceProjectContextHost projectContextHost)
        {
            _threadingService = threadingService;
            _codeModelFactory = codeModelFactory;
            _projectContextHost = projectContextHost;
        }

        public CodeModel GetCodeModel(Project project)
        {
            Requires.NotNull(project, nameof(project));

            return _threadingService.ExecuteSynchronously(() =>
            {
                return GetCodeModelAsync(project);
            });
        }

        public FileCodeModel? GetFileCodeModel(ProjectItem fileItem)
        {
            Requires.NotNull(fileItem, nameof(fileItem));

            return _threadingService.ExecuteSynchronously(() =>
            {
                return GetFileCodeModelAsync(fileItem);
            });
        }

        private async Task<CodeModel> GetCodeModelAsync(Project project)
        {
            await _threadingService.SwitchToUIThread();

            return await _projectContextHost.OpenContextForWriteAsync(accessor =>
            {
                return Task.FromResult(_codeModelFactory.GetCodeModel(accessor.Context, project));
            });
        }

        private async Task<FileCodeModel?> GetFileCodeModelAsync(ProjectItem fileItem)
        {
            await _threadingService.SwitchToUIThread();

            return await _projectContextHost.OpenContextForWriteAsync(accessor =>
            {
                try
                {
                    return Task.FromResult<FileCodeModel?>(_codeModelFactory.GetFileCodeModel(accessor.Context, fileItem));
                }
                catch (NotImplementedException)
                {   // Isn't a file that Roslyn knows about
                }

                return Task.FromResult<FileCodeModel?>(null);
            });
        }
    }
}
