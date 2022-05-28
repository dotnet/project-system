// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

internal class MyAppDocument
{
    private readonly XDocument doc;
    private readonly XElement root;

    private readonly string name;

    public MyAppDocument(string fileName, string filePath)
    {
        var xmlString = File.ReadAllText(filePath);
        doc = XDocument.Parse(xmlString);
        root = doc.Root;
        name = fileName;
    }

    public string GetProperty(string propertyName)
    {
        return (string)root.Element(propertyName);
    }

    public void SetProperty(string propertyName, string propertyValue)
    {
        root.Element(propertyName).Value = propertyValue;
        doc.Save(name);
    }
}
