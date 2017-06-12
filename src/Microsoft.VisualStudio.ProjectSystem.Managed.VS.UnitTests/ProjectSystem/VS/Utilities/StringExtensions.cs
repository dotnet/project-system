using System;
using System.IO;
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

        public static string SaveAndGetChanges(this ProjectRootElement root)
        {
            var tempFile = Path.GetTempFileName();
            root.Save(tempFile);
            var result = File.ReadAllText(tempFile);
            File.Delete(tempFile);
            return result;
        }
    }
}
