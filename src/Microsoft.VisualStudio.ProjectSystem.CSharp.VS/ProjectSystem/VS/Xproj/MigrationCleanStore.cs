// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Xproj
{
    [Export(typeof(IMigrationCleanStore))]
    class MigrationCleanStore : IMigrationCleanStore
    {
        private readonly object _lock = new object();

        private ISet<string> _files = new HashSet<string>();

        public ISet<string> DrainFiles()
        {
            lock (_lock)
            {
                var files = _files;
                if (files.Count > 0)
                {
                    _files = new HashSet<string>();
                }
                return files;
            }
        }

        public void AddFiles(params string[] fullPaths)
        {
            lock (_lock)
            {
                _files.AddRange(fullPaths);
            }
        }
    }
}
