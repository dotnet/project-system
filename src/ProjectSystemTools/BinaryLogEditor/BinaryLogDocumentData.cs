// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.Build.Logging;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.LogModel;
using Microsoft.VisualStudio.ProjectSystem.LogModel.Builder;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BinaryLogEditor
{
    internal sealed class BinaryLogDocumentData : IVsPersistDocData2, IPersistFileFormat
    {
        private string _filename;

        public Log Log { get; private set; }

        public event EventHandler Loaded;

        public int GetGuidEditorType(out Guid classGuid)
        {
            classGuid = ProjectSystemToolsPackage.BinaryLogEditorFactoryGuid;
            return VSConstants.S_OK;
        }

        public int IsDocDataDirty(out int isDirty)
        {
            isDirty = 0;
            return VSConstants.S_OK;
        }

        public int SetUntitledDocPath(string documentDataPath) => VSConstants.S_OK;

        public int LoadDocData(string moniker)
        {
            try
            {
                var replayer = new BinaryLogReplayEventSource();
                var builder = new ModelBuilder(replayer);
                replayer.Replay(moniker);
                _filename = moniker;
                Log = builder.Finish();
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aggregateException)
                {
                    Log = new Log(null, ImmutableList<Evaluation>.Empty, aggregateException.InnerExceptions.ToImmutableList());
                }
                else
                {
                    Log = new Log(null, ImmutableList<Evaluation>.Empty, new[] { ex }.ToImmutableList());
                }
            }

            Loaded?.Invoke(this, new EventArgs());

            return VSConstants.S_OK;
        }

        public int SaveDocData(VSSAVEFLAGS saveFlags, out string monikerNew, out int isSaveCancelled)
        {
            monikerNew = string.Empty;
            isSaveCancelled = -1;
            return VSConstants.S_OK;
        }

        public int Close() => VSConstants.S_OK;

        public int OnRegisterDocData(uint docCookie, IVsHierarchy hierarchyNew, uint itemidNew) => VSConstants.S_OK;

        public int RenameDocData(uint attributes, IVsHierarchy hierarchyNew, uint itemidNew, string monikerNew) => VSConstants.S_OK;

        public int IsDocDataReloadable(out int isReloadable)
        {
            isReloadable = 0;
            return VSConstants.S_OK;
        }

        public int ReloadDocData(uint flags) => VSConstants.S_OK;

        public int SetDocDataDirty(int isDirty) => VSConstants.S_OK;

        public int IsDocDataReadOnly(out int isReadOnly)
        {
            isReadOnly = 1;
            return VSConstants.S_OK;
        }

        public int SetDocDataReadOnly(int isReadOnly) => VSConstants.S_OK;

        int IPersist.GetClassID(out Guid classId) => GetGuidEditorType(out classId);

        public int IsDirty(out int isDirty) => IsDocDataDirty(out isDirty);

        public int InitNew(uint formatIndex) => VSConstants.S_OK;

        public int Load(string filename, uint mode, int readOnly) => VSConstants.S_OK;

        public int Save(string filename, int remember, uint formatIndex)
        {
            return VSConstants.S_OK;
        }

        public int SaveCompleted(string filename) => VSConstants.S_OK;

        public int GetCurFile(out string filename, out uint formatIndex)
        {
            filename = _filename;
            formatIndex = 1;
            return VSConstants.S_OK;
        }

        public int GetFormatList(out string formatList)
        {
            formatList = $"{BinaryLogEditorResources.FileExtensionName}\n*{BinaryLogEditorResources.FileExtension}\n";
            return VSConstants.S_OK;
        }

        int IPersistFileFormat.GetClassID(out Guid classId) => GetGuidEditorType(out classId);
    }
}
