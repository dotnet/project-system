using System.Xml;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    public static class StringExtensions
    {
        public static ProjectRootElement AsProjectRootElement(this string @string)
        {
            var stringReader = new System.IO.StringReader(@string);
            var xmlReader = new XmlTextReader(stringReader);
            var root = ProjectRootElement.Create(xmlReader);
            return root;
        }
    }
}
