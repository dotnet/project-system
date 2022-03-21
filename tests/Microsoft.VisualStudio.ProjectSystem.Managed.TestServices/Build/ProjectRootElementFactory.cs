// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml;

namespace Microsoft.Build.Construction
{
    internal static class ProjectRootElementFactory
    {
        public static ProjectRootElement Create(string? xml = null)
        {
            if (string.IsNullOrEmpty(xml))
                xml = "<Project/>";

            using var reader = XmlReader.Create(new StringReader(xml));
            return ProjectRootElement.Create(reader);
        }
    }
}
