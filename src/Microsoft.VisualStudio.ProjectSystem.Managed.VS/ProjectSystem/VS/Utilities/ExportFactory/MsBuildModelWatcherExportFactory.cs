// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory
{
    [Export(typeof(IExportFactory<IMsBuildModelWatcher>))]
    class MsBuildModelWatcherExportFactory : IExportFactory<IMsBuildModelWatcher>
    {
        private readonly ExportFactory<IMsBuildModelWatcher> _factory;

        [ImportingConstructor]
        public MsBuildModelWatcherExportFactory(ExportFactory<IMsBuildModelWatcher> factory)
        {
            _factory = factory;
        }

        public IMsBuildModelWatcher CreateExport()
        {
            return _factory.CreateExport().Value;
        }
    }
}
