// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.Build.Construction;

namespace Microsoft.Build
{
    public static class MSBuildAssert
    {
        public static void AssertProjectXml(string expected, ProjectRootElement actual)
        {
            AssertProjectXml(ProjectRootElementFactory.Create(expected), actual);
        }

        public static void AssertProjectXml(ProjectRootElement expected, ProjectRootElement actual)
        {
            string expectedXml = ProjectXmlToString(expected);
            string actualXml = ProjectXmlToString(actual);

            Assert.Equal(expectedXml, actualXml);
        }

        private static string ProjectXmlToString(ProjectRootElement projectXml)
        {
            using var writer = new StringWriterWithUtf8Encoding();
            projectXml.Save(writer);

            return writer.ToString();
        }

        // MSBuild will write out the XML declaration if the encoding isn't UTF8, 
        // force it into thinking it is to make comparison easier.
        private class StringWriterWithUtf8Encoding : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
