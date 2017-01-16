// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
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
        public MSBuildXmlAccessor(IProjectLockService projectLockService, UnconfiguredProject unconfiguredProject, IFileSystem fileSystem)
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
                var xmlString = stringWriter.ToString();
                // Remove the xml prelude to deal with https://github.com/dotnet/roslyn-project-system/issues/1168 until
                // we have a better solution. The XML returned here has a utf-16 header, even if the project file is
                // encoded as UTF-8. This will mess up the project file encoding, so we strip it here to prevent that case.
                // Note that if the user adds the header manually it will still be stripped, so we need to find a better
                // long term solution for this.
                if (xmlString.StartsWith("<?xml", StringComparison.Ordinal))
                {
                    xmlString = xmlString.Substring(xmlString.IndexOf(Environment.NewLine) + Environment.NewLine.Length);
                }
                return xmlString;
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
