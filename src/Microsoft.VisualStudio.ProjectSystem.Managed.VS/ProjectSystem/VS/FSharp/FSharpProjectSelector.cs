// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Packaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.FSharp
{
    [Export(typeof(IPackageService))]
    [Guid("E720DAD0-1854-47FC-93AF-E719B54B84E6")]
    [ProvideObject(typeof(FSharpProjectSelector), RegisterUsing = RegistrationMethod.CodeBase)]
    internal sealed class FSharpProjectSelector : IVsProjectSelector, IPackageService
    {
        private const string MSBuildXmlNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <inheritdoc />
        public async Task<IDisposable?> InitializeAsync(ManagedProjectSystemPackage package, IComponentModel componentModel)
        {
            IVsRegisterProjectSelector projectSelector = await package.GetServiceAsync<SVsRegisterProjectTypes, IVsRegisterProjectSelector>();

            Guid selectorGuid = typeof(FSharpProjectSelector).GUID;
            projectSelector.RegisterProjectSelector(ref selectorGuid, this, out uint cookie);

            return cookie == VSConstants.VSCOOKIE_NIL
                ? null 
                : new DisposableDelegate(() => projectSelector.UnregisterProjectSelector(cookie));
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
    }
}
