// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Editor.Listeners;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory
{
    [Export(typeof(IExportFactory<ITextBufferStateListener>))]
    internal class TextBufferStateListenerExportFactory : IExportFactory<ITextBufferStateListener>
    {
        private readonly ExportFactory<ITextBufferStateListener> _factory;

        [ImportingConstructor]
        public TextBufferStateListenerExportFactory(ExportFactory<ITextBufferStateListener> factory)
        {
            _factory = factory;
        }

        public ITextBufferStateListener CreateExport() => _factory.CreateExport().Value;
    }
}
