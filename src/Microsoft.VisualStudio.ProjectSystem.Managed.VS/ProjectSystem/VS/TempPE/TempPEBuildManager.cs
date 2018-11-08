using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(ITempPEBuildManager))]
    internal class TempPEBuildManager : ITempPEBuildManager
    {
        private readonly IUnconfiguredProjectCommonServices _unconfiguredProjectServices;
        private readonly ITempPECompilerHost _compilerHost;
        private readonly IVsService<IVsRunningDocumentTable> _rdt;

        [ImportingConstructor]
        public TempPEBuildManager(
            IUnconfiguredProjectCommonServices unconfiguredProjectServices,
            ITempPECompilerHost compilerHost,
            IVsService<SVsRunningDocumentTable, IVsRunningDocumentTable> rdt)
        {
            _unconfiguredProjectServices = unconfiguredProjectServices;
            _compilerHost = compilerHost;
            _rdt = rdt;
        }

        public async Task<string[]> GetDesignTimeOutputFilenamesAsync(bool shared)
        {
            var propertyToCheck = shared ? Compile.DesignTimeSharedInputProperty : Compile.DesignTimeProperty;

            var project = _unconfiguredProjectServices.ActiveConfiguredProject;
            var ruleSource = project.Services.ProjectSubscription.ProjectRuleSource;
            var update = await ruleSource.GetLatestVersionAsync(project, new string[] { Compile.SchemaName });
            var snapshot = update[Compile.SchemaName];

            var fileNames = new List<string>();
            foreach (var item in snapshot.Items.Values)
            {
                bool isLink = GetBooleanPropertyValue(item, Compile.LinkProperty);
                bool designTime = GetBooleanPropertyValue(item, propertyToCheck);

                if (!isLink && designTime)
                {
                    if (item.TryGetValue(Compile.FullPathProperty, out string path))
                    {
                        fileNames.Add(path);
                    }
                }
            }

            return fileNames.ToArray();

            bool GetBooleanPropertyValue(IImmutableDictionary<string, string> item, string propName)
            {
                return item.TryGetValue(propName, out string value) && StringComparers.PropertyValues.Equals(value, "true");
            }
        }

        public async Task<string> GetTempPEBlobAsync(string fileName)
        {
            await _unconfiguredProjectServices.ThreadingService.SwitchToUIThread();

            var property = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync();
            var languageName = (await property.LanguageServiceName.GetValueAsync())?.ToString();

            if (languageName == null) return null;

            //var referencesProperty = await _unconfiguredProjectServices.ActiveConfiguredProjectProperties.GetResolvedAssemblyReferencePropertiesAsync();
            //var path = await referencesProperty.ResolvedPath.GetValueAsPathAsync(false, false);

            var fileNames = new List<string>(await GetDesignTimeOutputFilenamesAsync(true));
            
            fileNames.Add(fileName);

            var rdt = await _rdt.GetValueAsync();

            if (rdt.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, fileName, out IVsHierarchy hierarchy, out uint itemid, out IntPtr docDataPtr, out uint cookie) != VSConstants.S_OK)
            {
                return null;
            }

            string contents = null;
            DocData docData = null;
            try
            {
                docData = new DocData(Marshal.GetObjectForIUnknown(docDataPtr));
                contents = GetStringFromTextStream(docData.Buffer as IVsTextStream);
            }
            finally
            {
                if (docData != null)
                    docData.Dispose();

                //This is for GetObjectForIUnknown call
                Marshal.Release(docDataPtr);
            }


            contents = GetFileContentsFromRDT(rdt, fileName);

            string outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var result = _compilerHost.Compile(languageName, outputPath, new string[] { contents }, Array.Empty<string>());

            return "";
            // return <temp pe xml>;
        }

        private static string GetFileContentsFromRDT(IVsRunningDocumentTable runningDocumentTable, string file)
        {
            string contents = String.Empty;

            int hr = runningDocumentTable.GetRunningDocumentsEnum(out IEnumRunningDocuments runningDocuments);
            if (hr == VSConstants.S_OK && runningDocuments != null)
            {
                uint[] docCookie = new uint[1];
                while (runningDocuments.Next(1, docCookie, out uint fetched) == VSConstants.S_OK)
                {
                    IntPtr docDataPtr = IntPtr.Zero;
                    string docfile = string.Empty;
                    if (runningDocumentTable.GetDocumentInfo(docCookie[0], out uint rdtFlags, out uint readLocks, out uint editLocks, out docfile, out IVsHierarchy hierarchy, out uint itemId, out docDataPtr) == VSConstants.S_OK && docDataPtr != IntPtr.Zero)
                    {
                        if (docDataPtr != IntPtr.Zero)
                        {
                            DocData docData = null;
                            try
                            {
                                docData = new DocData(Marshal.GetObjectForIUnknown(docDataPtr));
                                if (string.Equals(string.Empty, file, StringComparison.OrdinalIgnoreCase) && docData.Buffer != null)
                                {
                                    if (docData.Buffer is IVsBatchUpdate batchUpdate)
                                        batchUpdate.FlushPendingUpdates(0);

                                    contents = GetStringFromTextStream(docData.Buffer as IVsTextStream);
                                    break;
                                }
                            }
                            finally
                            {
                                if (docData != null)
                                    docData.Dispose();

                                //This is for GetObjectForIUnknown call
                                Marshal.Release(docDataPtr);
                            }
                        }
                    }
                }
            }

            return contents;
        }

        private static string GetStringFromTextStream(IVsTextStream textStream)
        {
            textStream.GetSize(out int length);

            string textInStream;// = string.Empty;
            IntPtr buffer = Marshal.AllocCoTaskMem((length + 1) * 2);
            try
            {
                textStream.GetStream(0, length, buffer);
                textInStream = Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }

            return textInStream;
        }
    }
}
