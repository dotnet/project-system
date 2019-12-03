// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.FSharp
{
    [Export(typeof(IPackageService))]
    [Guid("E720DAD0-1854-47FC-93AF-E719B54B84E6")]
    [ProvideObject(typeof(FSharpProjectSelector), RegisterUsing = RegistrationMethod.CodeBase)]
    internal sealed class FSharpProjectSelector : IVsProjectSelector, IPackageService, IDisposable
    {
        private const string MSBuildXmlNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        private readonly JoinableTaskContext _context;
        private IVsRegisterProjectSelector? _projectSelector;
        private uint _cookie = VSConstants.VSCOOKIE_NIL;

        [ImportingConstructor]
        public FSharpProjectSelector(JoinableTaskContext context)
        {
            _context = context;
        }

        public async Task InitializeAsync(IAsyncServiceProvider asyncServiceProvider)
        {
            Assumes.Null(_projectSelector);
            Assumes.True(_context.IsOnMainThread, "Must be on UI thread");

            _projectSelector = await asyncServiceProvider.GetServiceAsync<SVsRegisterProjectTypes, IVsRegisterProjectSelector>();

            Guid selectorGuid = GetType().GUID;
            _projectSelector.RegisterProjectSelector(ref selectorGuid, this, out _cookie);
        }

        public void GetProjectFactoryGuid(Guid guidProjectType, string pszFilename, out Guid guidProjectFactory)
        {
            var doc = XDocument.Load(pszFilename);
            GetProjectFactoryGuid(doc, out guidProjectFactory);
        }

        internal static void GetProjectFactoryGuid(XDocument doc, out Guid guidProjectFactory)
        {
            var nsm = new XmlNamespaceManager(new NameTable());
            nsm.AddNamespace("msb", MSBuildXmlNamespace);

            // If the project has either a Project-level SDK attribute or an Import-level SDK attribute, we'll open it with the new project system.
            // Check both namespace-qualified and unqualified forms to include projects with and without the xmlns attribute.
            bool hasProjectElementWithSdkAttribute = doc.XPathSelectElement("/msb:Project[@Sdk]", nsm) != null || doc.XPathSelectElement("/Project[@Sdk]") != null;
            bool hasImportElementWithSdkAttribute = doc.XPathSelectElement("/*/msb:Import[@Sdk]", nsm) != null || doc.XPathSelectElement("/*/Import[@Sdk]") != null;

            if (hasProjectElementWithSdkAttribute || hasImportElementWithSdkAttribute)
            {
                guidProjectFactory = ProjectType.FSharpGuid;
                return;
            }

            guidProjectFactory = ProjectType.LegacyFSharpGuid;
        }

        public void Dispose()
        {
            Assumes.True(_context.IsOnMainThread, "Must be on UI thread");

            if (_cookie != VSConstants.VSCOOKIE_NIL)
            {
                _projectSelector?.UnregisterProjectSelector(_cookie);
            }
        }
    }
}
