// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.VS.Rename;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal sealed class Renamer
    {
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;
        private readonly IUserNotificationServices _userNotificationServices;
        private readonly IEnvironmentOptions _optionsSettings;
        private readonly IRoslynServices _roslynServices;
        private readonly Project _project;
        private readonly string _newFilePath;
        private readonly string _oldFilePath;
        private bool _docAdded = false;
        private bool _docRemoved = false;
        private bool _docChanged = false;

        internal Renamer(Workspace workspace,
                         IProjectThreadingService threadingService,
                         IUserNotificationServices userNotificationServices,
                         IEnvironmentOptions optionsSettings,
                         IRoslynServices roslynServices,
                         Project project,
                         string oldFilePath,
                         string newFilePath)
        {
            _workspace = workspace;
            _threadingService = threadingService;
            _userNotificationServices = userNotificationServices;
            _optionsSettings = optionsSettings;
            _roslynServices = roslynServices;
            _project = project;
            _newFilePath = newFilePath;
            _oldFilePath = oldFilePath;
        }

        public async void OnWorkspaceChangedAsync(object sender, WorkspaceChangeEventArgs args)
        {
            Document oldDocument = (from d in _project.Documents where StringComparers.Paths.Equals(d.FilePath, _oldFilePath) select d).FirstOrDefault();

            if (oldDocument == null)
                return;

            if (args.Kind == WorkspaceChangeKind.DocumentAdded && args.ProjectId == _project.Id)
            {
                Project project = (from p in args.NewSolution.Projects where p.Id.Equals(_project.Id) select p).FirstOrDefault();
                Document addedDocument = (from d in project?.Documents where d.Id.Equals(args.DocumentId) select d).FirstOrDefault();

                if (addedDocument != null && StringComparers.Paths.Equals(addedDocument.FilePath, _newFilePath))
                {
                    _docAdded = true;
                }
            }

            if (args.Kind == WorkspaceChangeKind.DocumentRemoved && args.ProjectId == _project.Id && args.DocumentId == oldDocument.Id)
            {
                _docRemoved = true;
            }

            if (args.Kind == WorkspaceChangeKind.DocumentChanged && args.ProjectId == _project.Id)
            {
                _docChanged = true;
            }

            if (_docAdded && _docRemoved && _docChanged)
            {
                _workspace.WorkspaceChanged -= OnWorkspaceChangedAsync;
                var myNewProject = _workspace.CurrentSolution.Projects.Where(p => StringComparers.Paths.Equals(p.FilePath, _project.FilePath)).FirstOrDefault();
                await RenameAsync(myNewProject).ConfigureAwait(false);
            }
        }

        public async Task RenameAsync(Project project)
        {
            var renameStrategy = await GetStrategyAsync(project).ConfigureAwait(false);
            if (renameStrategy != null)
                await renameStrategy.RenameAsync(project, _oldFilePath, _newFilePath).ConfigureAwait(false);
        }

        private async Task<IRenameStrategy> GetStrategyAsync(Project project)
        {
            IRenameStrategy[] strategies = new IRenameStrategy[] {
                new SimpleRenameStrategy(_threadingService, _userNotificationServices, _optionsSettings, _roslynServices)
            };

            var isCaseSensitive = false;
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            if (compilation != null)
            {
                isCaseSensitive = compilation.IsCaseSensitive;
            }
            return strategies.FirstOrDefault(s => s.CanHandleRename(_oldFilePath, _newFilePath, isCaseSensitive));
        }
    }
}
