// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(ExportContractNames.VsTypes.ProjectNodeComExtension)]
    [AppliesTo(ProjectCapability.OpenProjectFile)]
    [ComServiceIid(typeof(IVsTextBufferProvider))]
    [ComServiceIid(typeof(IResettableBuffer))]
    internal class VsTextBufferProviderAggregator : IVsTextBufferProvider, IResettableBuffer
    {
        private static readonly Guid XmlEditorFactory = Guid.Parse("{fa3cd31e-987b-443a-9b81-186104e8dac1}");

        private readonly ITextBufferManager _textBufferManager;

        [ImportingConstructor]
        public VsTextBufferProviderAggregator(ITextBufferManager textBufferManager)
        {
            _textBufferManager = textBufferManager;
        }

        public Int32 GetTextBuffer(out IVsTextLines ppTextBuffer)
        {
            ppTextBuffer = _textBufferManager.TextLines;
            return VSConstants.S_OK;
        }

        public Int32 SetTextBuffer(IVsTextLines pTextBuffer)
        {
            return VSConstants.E_NOTIMPL;
        }

        public Int32 LockTextBuffer(Int32 fLock)
        {
            return VSConstants.S_OK;
        }

        public void Reset()
        {
            _textBufferManager.ResetBuffer();
        }
    }

    /// <summary>
    /// Com-Visible interface for reseting the text in the buffer. This is used by the LoadedProjectFileEditorFactory in order to
    /// reset the contents of the buffer to the current msbuild xml every time the buffer is reopened.
    /// </summary>
    [Guid("5372EF46-CA1A-4DAE-B9C7-9140839381AE")]
    [InterfaceType(1)]
    public interface IResettableBuffer
    {
        void Reset();
    }
}
