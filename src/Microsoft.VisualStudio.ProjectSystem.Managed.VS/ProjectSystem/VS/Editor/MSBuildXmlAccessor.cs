// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
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
            var configuredProject = await _unconfiguredProject.GetSuggestedConfiguredProjectAsync().ConfigureAwait(false);
            if (configuredProject == null)
            {
                return string.Empty;
            }
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var projectXml = await access.GetProjectAsync(configuredProject).ConfigureAwait(true);
                return projectXml.Xml.RawXml;
            }
        }

        public async Task SaveProjectXmlAsync(string toSave)
        {
            using (var access = await _projectLockService.WriteLockAsync())
            {
                await access.CheckoutAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                var encoding = await _unconfiguredProject.GetFileEncodingAsync().ConfigureAwait(false);
                _fileSystem.WriteAllText(_unconfiguredProject.FullPath, toSave, encoding);
            }
        }
    }
}
