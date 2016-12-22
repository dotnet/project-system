// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    [Export(typeof(IMsBuildAccessor))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class MsBuildAccessor : IMsBuildAccessor
    {
        private readonly IProjectLockService _projectLockService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public MsBuildAccessor(IProjectLockService projectLockService, UnconfiguredProject unconfiguredProject, IFileSystem fileSystem)
        {
            _projectLockService = projectLockService;
            _unconfiguredProject = unconfiguredProject;
            _fileSystem = fileSystem;
        }

        public async Task<string> GetProjectXmlAsync()
        {
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var stringWriter = new StringWriter();
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                projectXml.Save(stringWriter);
                return stringWriter.ToString();
            }
        }

        public async Task SaveProjectXmlAsync(string toSave)
        {
            var encoding = await _unconfiguredProject.GetFileEncodingAsync().ConfigureAwait(false);
            using (var access = await _projectLockService.WriteLockAsync())
            {
                await access.CheckoutAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                _fileSystem.WriteAllText(_unconfiguredProject.FullPath, toSave, encoding);
            }
        }
    }
}
