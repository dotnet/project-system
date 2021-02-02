// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using TaskResult = Microsoft.VisualStudio.Threading.TaskResult;

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

        public CodeModel? GetCodeModel(Project project)
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

        private async Task<CodeModel?> GetCodeModelAsync(Project project)
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

                return TaskResult.Null<FileCodeModel>();
            });
        }
    }
}
