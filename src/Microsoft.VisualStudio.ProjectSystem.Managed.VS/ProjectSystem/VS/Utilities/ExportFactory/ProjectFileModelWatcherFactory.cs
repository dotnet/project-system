// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory
{
    [Export(typeof(IExportFactory<IProjectFileModelWatcher>))]
    internal class ProjectFileModelWatcherFactory : IExportFactory<IProjectFileModelWatcher>
    {
        private readonly ExportFactory<IProjectFileModelWatcher> _factory;

        [ImportingConstructor]
        public ProjectFileModelWatcherFactory(ExportFactory<IProjectFileModelWatcher> factory)
        {
            _factory = factory;
        }

        public IProjectFileModelWatcher CreateExport() => _factory.CreateExport().Value;
    }
}
