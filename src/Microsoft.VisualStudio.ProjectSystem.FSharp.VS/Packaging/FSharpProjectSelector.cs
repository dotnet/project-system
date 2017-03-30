using System;
using Microsoft.VisualStudio.Shell.Interop1;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.VisualStudio.ProjectSystem.VS.Generators;

namespace Microsoft.VisualStudio.Packaging
{
    [ComVisible(true)]
    [Guid(SelectorGuid)]
    [ClassRegistration(SelectorGuid, "Microsoft.VisualStudio.Packaging.FSharpProjectSelector, Microsoft.VisualStudio.ProjectSystem.FSharp.VS")]
    public sealed class FSharpProjectSelector : IVsProjectSelector
    {
        public const string SelectorGuid = "E720DAD0-1854-47FC-93AF-E719B54B84E6";

        public void GetProjectFactoryGuid(Guid guidProjectType, string pszFilename, out Guid guidProjectFactory)
        {
            XDocument doc = XDocument.Load(pszFilename);
            XmlNamespaceManager nsm = new XmlNamespaceManager(new NameTable());
            nsm.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");
            var hasProjectElementWithSdkAttribute = doc.XPathSelectElement("/msb:Project[@Sdk]", nsm) != null || doc.XPathSelectElement("/Project[@Sdk]") != null;
            var hasImportElementWithSdk = doc.XPathSelectElement("msb:Import[@Sdk]", nsm) != null || doc.XPathSelectElement("Import[@Sdk]") != null;

            if (hasProjectElementWithSdkAttribute || hasImportElementWithSdk)
            {
                guidProjectFactory = Guid.Parse(FSharpProjectSystemPackage.ProjectTypeGuid);
                return;
            }

            guidProjectFactory = Guid.Parse(FSharpProjectSystemPackage.LegacyProjectTypeGuid);
        }
    }
}
