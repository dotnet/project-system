// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Microsoft.VisualStudio.Shell.Design.Serialization;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WindowsForms;

internal class MyAppDocument
{
    private readonly XDocument _doc;
    private readonly DocData _docData;

    public MyAppDocument(DocData docData)
    {
        using var textReader = new DocDataTextReader(docData);
        _doc = XDocument.Load(textReader);
        _docData = docData;
    }

    public string GetProperty(string propertyName)
    {
        XElement element = _doc.Root.Element(propertyName);

        if (element is null)
            return string.Empty;

        return element.Value;
    }

    public void SetProperty(string propertyName, string propertyValue)
    {
        XElement element = _doc.Root.Element(propertyName);

        if (element is not null)
            element.Value = propertyValue;
        else
        {
            element = new XElement(propertyName, propertyValue);
            _doc.Root.Add(element);
        }

        using var textWriter = new DocDataTextWriter(_docData);
        _doc.Save(textWriter);
    }
}
