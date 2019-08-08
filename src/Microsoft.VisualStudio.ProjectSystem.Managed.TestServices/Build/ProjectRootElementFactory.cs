// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
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
