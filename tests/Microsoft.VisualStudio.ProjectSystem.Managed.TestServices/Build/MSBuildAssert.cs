// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.Build.Construction;
using Xunit;

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
