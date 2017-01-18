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
        private readonly ExportFactory<ProjectEncodingStringWriter> _stringWriterFactory;

        [ImportingConstructor]
        public MSBuildXmlAccessor(IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject,
            IFileSystem fileSystem,
            ExportFactory<ProjectEncodingStringWriter> stringWriterFactory)
        {
            Requires.NotNull(projectLockService, nameof(projectLockService));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
            Requires.NotNull(fileSystem, nameof(fileSystem));
            Requires.NotNull(stringWriterFactory, nameof(stringWriterFactory));

            _projectLockService = projectLockService;
            _unconfiguredProject = unconfiguredProject;
            _fileSystem = fileSystem;
            _stringWriterFactory = stringWriterFactory;
        }

        public async Task<string> GetProjectXmlAsync()
        {
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var stringWriter = _stringWriterFactory.CreateExport().Value;
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                projectXml.Save(stringWriter);
                return stringWriter.ToString();
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
