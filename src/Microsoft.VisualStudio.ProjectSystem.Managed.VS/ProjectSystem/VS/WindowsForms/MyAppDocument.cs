// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Microsoft.VisualStudio.Shell.Design.Serialization;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

internal class MyAppDocument
{
    private XDocument _doc;
    private XElement _root;
    private readonly DocDataTextReader _reader;

    private readonly string _fileName;

    public MyAppDocument(string fileName, DocDataTextReader textReader)
    {
        _reader = textReader;
        _doc = XDocument.Load(_reader);
        _root = _doc.Root;
        _fileName = fileName;
    }

    public string GetProperty(string filePath, string propertyName)
    {
        _doc = XDocument.Load(_reader);
        _root = _doc.Root;
        return (string)_root.Element(propertyName);
    }

    public void SetProperty(string propertyName, string propertyValue)
    {
        _root.Element(propertyName).Value = propertyValue;
        _doc.Save(_fileName);
    }
}
