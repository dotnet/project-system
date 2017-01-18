// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export]
    internal class ProjectEncodingStringWriter : StringWriter
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly Lazy<Encoding> _fileEncoding;

        [ImportingConstructor]
        public ProjectEncodingStringWriter(IProjectThreadingService threadingService,
            UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _threadingService = threadingService;
            _unconfiguredProject = unconfiguredProject;
            _fileEncoding = new Lazy<Encoding>(() => _threadingService.ExecuteSynchronously(_unconfiguredProject.GetFileEncodingAsync));
        }

        public override Encoding Encoding => _fileEncoding.Value;
    }
}
