// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem
{
    [Export(typeof(IProjectXmlAccessor))]
    internal class MSBuildXmlAccessor : IProjectXmlAccessor
    {
        private readonly IProjectLockService _projectLockService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public MSBuildXmlAccessor(IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject,
            IFileSystem fileSystem)
        {
            Requires.NotNull(projectLockService, nameof(projectLockService));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(fileSystem, nameof(fileSystem));

            _projectLockService = projectLockService;
            _unconfiguredProject = unconfiguredProject;
            _fileSystem = fileSystem;
        }

        public async Task<string> GetProjectXmlAsync()
        {
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                return projectXml.RawXml;
            }
        }

        public async Task SaveProjectXmlAsync(string toSave)
        {
            using (var access = await _projectLockService.WriteLockAsync())
            {
                await access.CheckoutAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                // We must clear the project dirty flag first. If it's dirty in memory, the ProjectReloadManager will detect that
                // the project is dirty and fail the reload, and discard the changes.
                var projectRoot = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                var stringWriter = new StringWriter();

                // Calling save on the ProjectRootElement clears the dirty flag. However, we don't care about the result, to just
                // throw it into a stringwriter and discard.
                projectRoot.Save(stringWriter);
                var encoding = await _unconfiguredProject.GetFileEncodingAsync().ConfigureAwait(false);
                _fileSystem.WriteAllText(_unconfiguredProject.FullPath, toSave, encoding);
            }
        }
        public async Task<string> GetEvaluatedPropertyValue(UnconfiguredProject unconfiguredProject, string propertyName)
        {
            var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var evaluatedProject = await access.GetProjectAsync(configuredProject).ConfigureAwait(false);
                return evaluatedProject.GetProperty(propertyName)?.EvaluatedValue;
            }
        }

        public async Task ExecuteInWriteLock(Action<ProjectRootElement> action)
        {
            using (var access = await _projectLockService.WriteLockAsync())
            {
                await access.CheckoutAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                var msbuildProject = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(false);
                action.Invoke(msbuildProject);
            }
        }
    }
}
