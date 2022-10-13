// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TextManager.Interop;
using DiagnosticsDebug = System.Diagnostics.Debug;

namespace Microsoft.VisualStudio.ProjectSystem.VS.WPF;

/// <summary>
/// When reading and writing the Application.xaml file, we have a requirement that
/// we do not change the user's formatting, comments, etc.  Thus we can't simply
/// read in the XML, modify it, and write it back out.  Instead, we have to
/// modify just the parts we need to directly in the text buffer.  This class
/// handles that surprisingly complex job.
/// </summary>
/// <remarks>
/// <para>
/// Ported from the class of the same name in Microsoft.VisualStudio.Editors.
/// </para>
/// <para>
/// References:
/// <list type="bullet">
/// <item>XAML Overviews: http://windowssdk.msdn.microsoft.com/en-us/library/ms744825.aspx</item>
/// <item>Property element syntax: http://windowssdk.msdn.microsoft.com/en-us/library/ms788723(VS.80).aspx#PESyntax</item>
/// </list>
/// </para>
/// </remarks>
internal class AppDotXamlDocument : AppDotXamlDocument.IDebugLockCheck, AppDotXamlDocument.IReplaceText
{
    private const string ApplicationElementName = "Application";
    private const string StartupUriPropertyName = "StartupUri";
    private const string ShutdownModePropertyName = "ShutdownMode";

    private const char SingleQuote = '\'';
    private const char DoubleQuote = '"';

    private static readonly char[] s_closingAngleBracketHelperCharacters = new[] { SingleQuote, DoubleQuote, '/', '>' };

    private readonly IVsTextLines _vsTextLines;

    private int _debugBufferLockCount = 0;

    public AppDotXamlDocument(IVsTextLines vsTextLines)
    {
        _vsTextLines = vsTextLines;
    }

    /// <summary>
    /// Retrieves the text between the given buffer line/char points
    /// </summary>
    private string GetText(int startLine, int startIndex, int endLine, int endIndex)
    {
        ErrorHandler.ThrowOnFailure(_vsTextLines.GetLineText(startLine, startIndex, endLine, endIndex, out string text));
        return text;
    }

    /// <summary>
    /// Retrieves the text starting at the given point and with the given length
    /// </summary>
    private string GetText(int startLine, int startIndex, int count)
    {
        ErrorHandler.ThrowOnFailure(_vsTextLines.GetLineText(startLine, startIndex, startLine, startIndex + count, out string text));
        return text;
    }

    /// <summary>
    /// Retrieves the text between the given buffer line/char points
    /// </summary>
    private string GetText(Location startLocation, Location endLocation)
    {
        return GetText(startLocation.LineIndex, startLocation.CharIndex, endLocation.LineIndex, endLocation.CharIndex);
    }

    /// <summary>
    /// Retrieves the text starting at the given point and with the given length
    /// </summary>
    private string GetText(Location startLocation, int count)
    {
        return GetText(startLocation.LineIndex, startLocation.CharIndex, count);
    }

    /// <summary>
    /// Retrieves all of the text in the buffer
    /// </summary>
    private string GetAllText()
    {
        ErrorHandler.ThrowOnFailure(_vsTextLines.GetLastLineIndex(out int lastLine, out int lastIndex));
        return GetText(startLine: 0, startIndex: 0, lastLine, lastIndex);
    }

    /// <summary>
    /// Escape a string in XML format, including double and single quotes
    /// </summary>
    private static string EscapeXmlString(string value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        XmlWriterSettings settings = new() { ConformanceLevel = ConformanceLevel.Fragment };
        XmlWriter xmlWriter = XmlWriter.Create(sb, settings);
        xmlWriter.WriteString(value);
        xmlWriter.Close();
        string escapedString = sb.ToString();

        // Now escape double and single quotes
        return escapedString.Replace("\"", "&quot;").Replace("'", "&apos;");
    }

    /// <summary>
    /// Unescapes an element's content value from escaped XML format.
    /// </summary>
    private static string UnescapeXmlString(string value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        // Escape any double quotes

        // Make as content of an element
        string xml = "<a>" + value + "</a>";
        StringReader stringReader = new(xml);
        XmlReaderSettings settings = new() { ConformanceLevel = ConformanceLevel.Fragment };
        XmlReader xmlReader = XmlReader.Create(stringReader, settings);
        xmlReader.ReadToFollowing("a");

        string content = xmlReader.ReadElementContentAsString();
        return content;
    }

    /// <summary>
    /// Finds the Application element that all application.xaml files must include as
    /// the single root node.
    /// </summary>
    private static void MoveToApplicationRootElement(XmlTextReader reader)
    {
        // XAML files must have only one root element.  For app.xaml, it must be "Application"
        if (reader.MoveToContent() == XmlNodeType.Element
            && reader.Name == ApplicationElementName)
        {
            // Okay
            return;
        }

        throw new XamlReadWriteException(string.Format(VSResources.WPFApp_Xaml_CouldntFindRootElement_1, ApplicationElementName));
    }

    /// <summary>
    /// Creates an XmlTextReader for the text
    /// in the buffer.
    /// </summary>
    private XmlTextReader CreateXmlTextReader()
    {
        DiagnosticsDebug.Assert(_debugBufferLockCount > 0, "Should be using BufferLock!");
        StringReader stringReader = new(GetAllText());
        // Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
        XmlTextReader xmlTextReader = new(stringReader) { DtdProcessing = DtdProcessing.Prohibit };
        return xmlTextReader;
    }

    /// <summary>
    /// Finds the value of the given property inside the xaml file.  If
    /// the property is not set in the xaml, an empty string is returned.
    /// </summary>
    private string? GetApplicationPropertyValue(string propertyName)
    {
        using BufferLock bufferLock = new(_vsTextLines, this);

        XmlTextReader reader = CreateXmlTextReader();
        XamlProperty? prop = FindApplicationPropertyInXaml(reader, propertyName);

        return prop?.UnescapedValue;
    }

    /// <summary>
    /// Find the closing angle bracket on a single line.
    /// See comments on FindClosingAngleBracket.
    /// </summary>
    /// <returns>The index on the line where the closing angle bracket is found, or -1 if not found.</returns>
    private static int FindClosingAngleBracketHelper(string line)
    {
        int index = 0;

        while (index < line.Length)
        {
            // Find the next character of interest
            index = line.IndexOfAny(s_closingAngleBracketHelperCharacters, index);
            if (index < 0)
            {
                return -1;
            }

            char characterOfInterest = line[index];
            switch (characterOfInterest)
            {
                case SingleQuote:
                case DoubleQuote:
                    // We have a string.  Skip past it.
                    int closingQuote = line.IndexOf(characterOfInterest, index + 1);
                    if (closingQuote < 0)
                    {
                        return -1;
                    }
                    else
                    {
                        index = closingQuote + 1;
                    }
                    break;
                case '>':
                    // Found '>'
                    return index;
                case '/':
                    if (line.IndexOf("/>", index) == index)
                    {
                        // Found "/>"
                        return index;
                    }
                    else
                    {
                        // Keep searching past the '/'
                        index += 1;
                    }
                    break;
                default:
                    DiagnosticsDebug.Fail("We shouldn't reach here");
                    break;
            }
        }

        return -1;
    }

    /// <summary>
    /// Searches forward from the given location, skipping quoted strings
    /// (single and double quoted), until it finds a closing angle
    /// bracket (">" or "/">).
    /// </summary>
    /// <param name="startLocation"></param>
    /// <returns>The location of the found ">" or "/>".  If it is not found, returns <see langword="null"/>.</returns>
    private Location? FindClosingAngleBracket(Location startLocation)
    {
        ErrorHandler.ThrowOnFailure(_vsTextLines.GetLastLineIndex(out int iLastLine, out int iLastIndex));

        for (int iLine = startLocation.LineIndex; iLine <= iLastLine; iLine++)
        {
            int iStartIndexForLine;
            int iEndIndexForLine;

            if (iLine == startLocation.LineIndex)
            {
                iStartIndexForLine = startLocation.CharIndex;
            }
            else
            {
                iStartIndexForLine = 0;
            }

            if (iLine == iLastLine)
            {
                iEndIndexForLine = iLastIndex;
            }
            else
            {
                ErrorHandler.ThrowOnFailure(_vsTextLines.GetLengthOfLine(iLine, out int iLineLength));
                iEndIndexForLine = iLineLength;
            }

            string lineText = GetText(iLine, iStartIndexForLine, iLine, iEndIndexForLine);

            int foundIndex = FindClosingAngleBracketHelper(lineText);
            if (foundIndex >= 0)
            {
                return new Location(iLine, iStartIndexForLine + foundIndex);
            }
        }

        // Not found
        return null;
    }

    /// <summary>
    /// From the root of a document, finds the given attribute inside the Application
    /// element, if it exists.  If not, returns Nothing.
    /// </summary>
    private XamlProperty? FindApplicationPropertyInXaml(XmlTextReader reader, string propertyName)
    {
        MoveToApplicationRootElement(reader);
        XamlProperty? prop = FindPropertyAsAttributeInCurrentElement(reader, ApplicationElementName, propertyName);
        if (prop is null)
        {
            prop = FindPropertyAsChildElementInCurrentElement(reader, ApplicationElementName, propertyName);
        }

        return prop;
    }

    private XamlPropertyInAttributeSyntax? FindPropertyAsAttributeInCurrentElement(XmlTextReader reader, string optionalPropertyQualifier, string propertyName)
    {
        // Look for either simple attribute syntax (StartupUri=xxx) or
        // fully-qualified attribute syntax (Application.StartupUri=xxx)
        string? fullyQualifiedPropertyName = string.Empty;
        if (optionalPropertyQualifier.Length != 0)
        {
            fullyQualifiedPropertyName = optionalPropertyQualifier + "." + propertyName;
        }

        string? foundPropertyName;
        if (reader.MoveToAttribute(propertyName))
        {
            foundPropertyName = propertyName;
        }
        else if (fullyQualifiedPropertyName.Length != 0
            && reader.MoveToAttribute(fullyQualifiedPropertyName))
        {
            foundPropertyName = fullyQualifiedPropertyName;
        }
        else
        {
            // Not found
            return null;
        }

        Location startLocation = new(reader);
        Location boundedEndLocation;

        // Remember the quote character actually found in the XML
        string quoteCharacterUsedByAttribute = new(reader.QuoteChar, count: 1);

        // Remember the actual value of the property
        string unescapedValue = reader.Value;

        // Find the end location of the attribute
        if (reader.MoveToNextAttribute())
        {
            boundedEndLocation = new Location(reader);
        }
        else
        {
            reader.Read();
            boundedEndLocation = new Location(reader);
            DiagnosticsDebug.Assert(boundedEndLocation.LineIndex >= startLocation.LineIndex);
            DiagnosticsDebug.Assert(boundedEndLocation.LineIndex > startLocation.LineIndex || boundedEndLocation.CharIndex > startLocation.CharIndex);
        }

        // Now we have an approximate location.  Find the exact location.
        if (_vsTextLines is not IVsTextFind vsTextFind)
        {
            DiagnosticsDebug.Fail("IVsTextFind not supported?");
            throw new InvalidOperationException();
        }
        else
        {
            // startLocation should be pointing to the attribute name.  Verify that.
            Location afterAttributeName = new(startLocation.LineIndex, startLocation.CharIndex + foundPropertyName.Length);

            // Find the equals sign ('=')
            string equalsSign = "=";
            if (ErrorHandler.Failed(vsTextFind.Find(equalsSign, afterAttributeName.LineIndex, afterAttributeName.CharIndex, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, out int equalsLine, out int equalsIndex)))
            {
                ThrowUnexpectedFormatException(startLocation);
            }
            DiagnosticsDebug.Assert(equalsSign.Equals(GetText(equalsLine, equalsIndex, 1), StringComparison.Ordinal));

            // Find the starting quote
            if (ErrorHandler.Failed(vsTextFind.Find(quoteCharacterUsedByAttribute, equalsLine, equalsIndex, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, out int startQuoteLine, out int startQuoteIndex)))
            {
                ThrowUnexpectedFormatException(startLocation);
            }
            DiagnosticsDebug.Assert(quoteCharacterUsedByAttribute.Equals(GetText(startQuoteLine, startQuoteIndex, 1), StringComparison.Ordinal));

            // Find the ending quote, assuming it's on the same line
            if (ErrorHandler.Failed(vsTextFind.Find(quoteCharacterUsedByAttribute, startQuoteLine, startQuoteIndex + 1, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, out int endQuoteLine, out int endQuoteIndex)))
            {
                ThrowUnexpectedFormatException(startLocation);
            }
            DiagnosticsDebug.Assert(quoteCharacterUsedByAttribute.Equals(GetText(endQuoteLine, endQuoteIndex, 1), StringComparison.Ordinal));

            // Now we have the start and end of the attribute's value definition
            Location valueStart = new(startQuoteLine, startQuoteIndex);
            Location valueEnd = new(endQuoteLine, endQuoteIndex + 1);
            return new XamlPropertyInAttributeSyntax(_vsTextLines, valueStart, valueEnd, unescapedValue);
        }
    }

    /// <summary>
    /// From the root of a document, finds the given attribute inside the Application
    /// element, using property element syntax, if it exists.  If not, returns <see langword="null"/>.
    /// </summary>
    private XamlPropertyInPropertyElementSyntax? FindPropertyAsChildElementInCurrentElement(XmlTextReader reader, string propertyQualifier, string propertyName)
    {
        // See http://windowssdk.msdn.microsoft.com/en-us/library/ms788723(VS.80).aspx#PESyntax
        //
        // Looking for something of this form:
        // <Application xmlns=...>
        //   <Application.StartupUri>MainWindow.xaml</Application.StartupUri>
        // </Application>

        // In this case, the "Application." prefix is required, not optional.
        string fullyQualifiedPropertyName = propertyQualifier + "." + propertyName;

        if (reader.ReadToDescendant(fullyQualifiedPropertyName))
        {
            // Found

            Location tagStart = new(reader);
            Location tagEnd = new(reader);

            Location? startTagEndingBracketLocation = FindClosingAngleBracket(tagStart);
            if (startTagEndingBracketLocation is null)
            {
                ThrowUnexpectedFormatException(tagStart);
            }

            if (reader.IsEmptyElement)
            {
                // It's an empty tag of the form <xyz/>.  The reader is at the 'x' in "xyz", so the
                // beginning is at -1 from that location.
                Location elementTagStart = new Location(reader).Shift(-1);

                // Read through the start tag
                if (!reader.Read())
                {
                    ThrowUnexpectedFormatException(tagStart);
                }

                // The reader is now right after the empty element tag
                Location elementTagEndPlusOne = new(reader);
                return new XamlPropertyInPropertyElementSyntaxWithEmptyTag(_vsTextLines, fullyQualifiedPropertyName, elementTagStart, elementTagEndPlusOne);
            }
            else
            {
                Location? valueStart;
                string unescapedValue;

                // Find the start of the content (reader's location after doing a Read through
                // the element will not give us reliable results, since it depends on the type of
                // node following the start tag).
                valueStart = FindClosingAngleBracket(new Location(reader).Shift(1)); // +1 to get past the ">"
                if (valueStart is null)
                {
                    ThrowUnexpectedFormatException(tagStart);
                }

                // Read through the start tag
                if (!reader.Read())
                {
                    ThrowUnexpectedFormatException(tagStart);
                }

                // Unfortunately, simply doing a ReadInnerXml() will take us too far.  We need to know
                // exactly where the value ends in the text, so we'll read manually.

                while (reader.NodeType != XmlNodeType.EndElement
                       || !fullyQualifiedPropertyName.Equals(reader.Name, StringComparison.Ordinal))
                {
                    if (!reader.Read())
                    {
                        // End tag not found
                        ThrowUnexpectedFormatException(tagStart);
                    }
                }

                // Reader is at location 'x' of </xyz>.  So we want -2 from this location.
                Location currentPosition2 = new(reader);
                Location valueEndPlusOne = new Location(reader).Shift(-2);

                // Get the inner text and unescape it.
                string innerText = GetText(valueStart, valueEndPlusOne);
                unescapedValue = UnescapeXmlString(innerText).Trim();

                return new XamlPropertyInPropertyElementSyntax(_vsTextLines, valueStart, valueEndPlusOne, unescapedValue);
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the location where a new attribute can be added to the
    /// Application root element.  Returns <see langword="null"/> if can't find the
    /// correct position.
    /// </summary>
    private Location? FindLocationToAddNewApplicationAttribute()
    {
        using BufferLock bufferLock = new(_vsTextLines, this);

        XmlTextReader reader = CreateXmlTextReader();
        MoveToApplicationRootElement(reader);
        return FindClosingAngleBracket(new Location(reader));
    }

    private void SetApplicationPropertyValue(string propertyName, string? value)
    {
        if (value is null)
        {
            value = string.Empty;
        }

        using BufferLock bufferLock = new(_vsTextLines, this);

        XmlTextReader reader = CreateXmlTextReader();
        XamlProperty? prop = FindApplicationPropertyInXaml(reader, propertyName);

        if (prop is not null)
        {
            // The property is already in the .xaml.
            prop.SetProperty(this, value);
        }
        else
        {
            // It's not in the .xaml yet.  We'll add this xxx=yyy definition
            // as the last attribute in the Application element.
            if (value.Length == 0)
            {
                // The new value is blank, just like the current value.
                // Don't change anything.
                return;
            }

            Location? replaceStart = FindLocationToAddNewApplicationAttribute();
            if (replaceStart is null)
            {
                ThrowUnexpectedFormatException(new Location(0, 0));
            }
            Location replaceEnd = replaceStart;
            string newText = propertyName + "=\"" + EscapeXmlString(value) + "\"";

            // Is the anything non-whitespace on the line where we're adding the
            // new code?  If so, put in a CR/LF pair before it.
            if (replaceStart.CharIndex > 0)
            {
                string lineTextBeforeInsertion = GetText(replaceStart.LineIndex, 0, replaceStart.LineIndex, replaceStart.CharIndex);
                if (lineTextBeforeInsertion.Trim().Length > 0)
                {
                    newText = "\r\n" + newText;
                }
            }

            // We know where to replace, so go ahead and do it.
            ((IReplaceText)this).ReplaceText(replaceStart, replaceEnd, newText);
        }
    }

    /// <summary>
    /// Replace the text at the given location in the buffer with new text.
    /// </summary>
    private void ReplaceText(Location sourceStart, int sourceLength, string newText)
    {
        ((IReplaceText)this).ReplaceText(sourceStart, new Location(sourceStart.LineIndex, sourceStart.CharIndex + sourceLength), newText);
    }

    /// <summary>
    /// Replace the text at the given location in the buffer with new text.
    /// </summary>
    void IReplaceText.ReplaceText(Location sourceStart, Location sourceEnd, string newText)
    {
        IntPtr bstrNewText = Marshal.StringToBSTR(newText);
        try
        {
            ErrorHandler.ThrowOnFailure(_vsTextLines.ReplaceLines(
                sourceStart.LineIndex, sourceStart.CharIndex,
                sourceEnd.LineIndex, sourceEnd.CharIndex,
                bstrNewText, newText.Length, null));
        }
        finally
        {
            Marshal.FreeBSTR(bstrNewText);
        }
    }

    /// <summary>
    /// Given the location of the start of an element tag, makes sure that it has an end tag.
    /// If the element tag is closed by "/>" instead of an end element, it is expanded
    /// into a start and end tag.
    /// </summary>
    /// <param name="tagStartLocation"></param>
    /// <param name="elementName">The name of the element at the given location</param>
    private void MakeSureElementHasStartAndEndTag(Location tagStartLocation, string elementName)
    {
        if (!"<".Equals(GetText(tagStartLocation, 1), StringComparison.Ordinal))
        {
            DiagnosticsDebug.Fail("MakeSureElementHasStartAndEndTags: The start location doesn't point to the start of an element tag");
            ThrowUnexpectedFormatException(tagStartLocation);
        }

        Location? startTagEndingBracketLocation = FindClosingAngleBracket(tagStartLocation);
        if (startTagEndingBracketLocation is null)
        {
            ThrowUnexpectedFormatException(tagStartLocation);
        }

        if (">".Equals(GetText(startTagEndingBracketLocation, 1), StringComparison.Ordinal))
        {
            // The element tag is of the <xxx> form.  We assume that there is an ending </xxx> tag, and
            // we don't need to do anything.
        }
        else
        {
            // It must be an empty tag of the <xxx/> form.
            string slashAndEndBracket = "/>";
            if (!slashAndEndBracket.Equals(GetText(startTagEndingBracketLocation, slashAndEndBracket.Length), StringComparison.Ordinal))
            {
                DiagnosticsDebug.Fail("FindClosingAngleBracket returned the wrong location?");
                ThrowUnexpectedFormatException(startTagEndingBracketLocation);
            }

            // We need to change <xxx attributes/> into <xxx attributes></xxx>
            string newText = "></" + elementName + ">";
            ReplaceText(startTagEndingBracketLocation, slashAndEndBracket.Length, newText);
        }
    }

    /// <summary>
    /// Finds the value of the StartupUri property inside the xaml file.  If
    /// the property is not set in the xaml, an empty string is returned.
    /// </summary>
    public string? GetStartupUri()
    {
        return GetApplicationPropertyValue(StartupUriPropertyName);
    }

    public void SetStartupUri(string value)
    {
        SetApplicationPropertyValue(StartupUriPropertyName, value);
    }

    /// <summary>
    /// Finds the value of the ShutdownMode property inside the xaml file.  If
    /// the property is not set in the xaml, an empty string is returned.
    /// </summary>
    public string? GetShutdownMode()
    {
        return GetApplicationPropertyValue(ShutdownModePropertyName);
    }

    public void SetShutdownMode(string value)
    {
        SetApplicationPropertyValue(ShutdownModePropertyName, value);
    }

    /// <summary>
    /// Throw an exception for an unexpected format, with line/col information
    /// </summary>
    [DoesNotReturn]
    private static void ThrowUnexpectedFormatException(Location location)
    {
        throw new XamlReadWriteException(string.Format(VSResources.WPFApp_Xaml_UnexpectedFormat_2, location.LineIndex + 1, location.CharIndex + 1));
    }

    /// <summary>
    /// Verify the validity of the Application.xaml file, and throw an exception if
    /// problems are found.
    /// </summary>
    private void VerifyAppXamlIsValidAndThrowIfNot()
    {
        using BufferLock bufferLock = new(_vsTextLines, this);

        XmlTextReader reader = CreateXmlTextReader();
        MoveToApplicationRootElement(reader);

        // Read through the Application element, including any child elements, to
        // ensure everything is properly closed.
        // The name of the element to find is irrelevant, as there shouldn't be
        // any elements following Application.
        reader.ReadToFollowing("Dummy Element");

        // If we made it to here, the .xaml file should be well-formed enough for us to read
        // it properly.  As a final check, though, try getting some common properties.
        GetStartupUri();
        GetShutdownMode();
    }

    void IDebugLockCheck.OnBufferLock()
    {
        _debugBufferLockCount++;
    }

    void IDebugLockCheck.OnBufferUnlock()
    {
        _debugBufferLockCount--;
        DiagnosticsDebug.Assert(_debugBufferLockCount >= 0, "Extra buffer unlock");
    }

    /// <summary>
    /// Represents a position in a text buffer.
    /// </summary>
    private class Location
    {
        /// <summary>
        /// Zero-based line #
        /// </summary>
        public int LineIndex;
        /// <summary>
        /// Zero-based character on line
        /// </summary>
        public int CharIndex;

        public Location(int lineIndex, int charIndex)
        {
            Requires.Range(lineIndex >= 0, nameof(lineIndex));
            Requires.Range(charIndex >= 0, nameof(charIndex));

            LineIndex = lineIndex;
            CharIndex = charIndex;
        }

        /// <summary>
        /// Creates a location corresponding to the current location of the
        /// XmlReader
        /// </summary>
        public Location(XmlReader reader)
            : this(((IXmlLineInfo)reader).LineNumber - 1, ((IXmlLineInfo)reader).LinePosition - 1)
        {
        }

        public Location Shift(int charIndexToAdd)
        {
            return new Location(LineIndex, CharIndex + charIndexToAdd);
        }
    }

    /// <summary>
    /// A simple interface to allow XamlProperty to ask the AppDotXamlDocument to
    /// make text replacements.
    /// </summary>
    private interface IReplaceText
    {
        /// <summary>
        /// Replace the text at the given location in the buffer with new text.
        /// </summary>
        void ReplaceText(Location sourceStart, Location sourceEnd, string newText);
    }

    /// <summary>
    /// Used by the document to verify BufferLock is used when it's needed
    /// </summary>
    private interface IDebugLockCheck
    {
        void OnBufferLock();
        void OnBufferUnlock();
    }

    /// <summary>
    /// We need to make sure the buffer doesn't change while we're looking up properties
    /// and changing them.  Our XmlReader() needs to be in sync with the actual text
    /// in the buffer. This class keeps the buffer locked until it is disposed.
    /// </summary>
    private class BufferLock : IDisposable
    {
        private bool _isDisposed;
        private IVsTextLines? _buffer;
        private readonly IDebugLockCheck _debugLockCheck;

        public BufferLock(IVsTextLines buffer, IDebugLockCheck debugLockCheck)
        {
            Requires.NotNull(buffer, nameof(buffer));
            Requires.NotNull(debugLockCheck, nameof(debugLockCheck));

            _buffer = buffer;
            _debugLockCheck = debugLockCheck;

            _buffer.LockBuffer();
            _debugLockCheck.OnBufferLock();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _buffer?.UnlockBuffer();
                    _debugLockCheck.OnBufferUnlock();
                    _buffer = null;
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents a property value found in the XAML file.
    /// </summary>
    [DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")]
    private abstract class XamlProperty
    {
        protected IVsTextLines VsTextLines;

        private readonly bool _definitionIncludesQuotes;
        /// <summary>
        /// Unescaped, translated value of the property from the XmlReader
        /// </summary>
        private readonly string _unescapedValue;
        private readonly Location _startLocation;
        /// <summary>
        /// Points to the index *after* the last character in the range, just like IVsTextLines expects
        /// </summary>
        private readonly Location _endLocationPlusOne;

        public XamlProperty(IVsTextLines vsTextLines, Location startLocation, Location endLocation, string? unescapedValue, bool definitionIncludesQuotes)
        {
            Requires.NotNull(vsTextLines, nameof(vsTextLines));
            Requires.NotNull(startLocation, nameof(startLocation));
            Requires.NotNull(endLocation, nameof(endLocation));

            unescapedValue ??= string.Empty;

            _startLocation = startLocation;
            _endLocationPlusOne = endLocation;
            _unescapedValue = unescapedValue;
            VsTextLines = vsTextLines;
            _definitionIncludesQuotes = definitionIncludesQuotes;
        }

        /// <summary>
        /// Retrieves the actual text for the value of the property, unescaped, as it
        /// appears in the .xaml file.  If DefinitionIncludesQuotes=True, then this
        /// includes the beginning/ending quote
        /// </summary>
        public virtual string ActualDefinitionText
        {
            get
            {
                ErrorHandler.ThrowOnFailure(VsTextLines.GetLineText(DefinitionStart.LineIndex, DefinitionStart.CharIndex, DefinitionEndPlusOne.LineIndex, DefinitionEndPlusOne.CharIndex, out string buffer));
                return buffer;
            }
        }

        public string UnescapedValue => _unescapedValue;
        public Location DefinitionStart => _startLocation;
        public Location DefinitionEndPlusOne => _endLocationPlusOne;

        /// <summary>
        /// Replace the property's value in the XAML
        /// </summary>
        public virtual void SetProperty(IReplaceText replaceTextInstance, string value)
        {
            if (UnescapedValue.Equals(value, StringComparison.Ordinal))
            {
                // The property value is not changing. Leave things alone.
                return;
            }

            // Replace just the string in the buffer with the new value.
            Location replaceStart = DefinitionStart;
            Location replaceEnd = DefinitionEndPlusOne;
            string newText = EscapeXmlString(value);
            if (_definitionIncludesQuotes)
            {
                newText = "\"" + newText + "\"";
            }

            // We know where to replace, so go ahead and do it.
            replaceTextInstance.ReplaceText(replaceStart, replaceEnd, newText);
        }
    }

    /// <summary>
    /// Represents a property that was found in the XAML file in attribute syntax
    /// </summary>
    private class XamlPropertyInAttributeSyntax : XamlProperty
    {
        public XamlPropertyInAttributeSyntax(IVsTextLines vsTextLines, Location definitionStart, Location definitionEnd, string unescapedValue)
            : base(vsTextLines, definitionStart, definitionEnd, unescapedValue, definitionIncludesQuotes: true)
        {
        }
    }

    /// <summary>
    /// Represents a property that was found in property element syntax with a start and end tag.
    /// </summary>
    private class XamlPropertyInPropertyElementSyntax : XamlProperty
    {
        public XamlPropertyInPropertyElementSyntax(IVsTextLines vsTextLines, Location valueStart, Location valueEnd, string unescapedValue)
            : base(vsTextLines, valueStart, valueEnd, unescapedValue, definitionIncludesQuotes: false)
        {
        }
    }

    /// <summary>
    /// Represents a property that was found in property element syntax with an empty tag,
    /// e.g. &lt;Application.StartupUri/>
    /// </summary>
    private class XamlPropertyInPropertyElementSyntaxWithEmptyTag : XamlPropertyInPropertyElementSyntax
    {
        private readonly string _fullyQualifiedPropertyName;

        public XamlPropertyInPropertyElementSyntaxWithEmptyTag(IVsTextLines vsTextLines, string fullyQualifiedPropertyName, Location elementStart, Location elementEnd)
            : base(vsTextLines, elementStart, elementEnd, unescapedValue: string.Empty)
        {
            _fullyQualifiedPropertyName = fullyQualifiedPropertyName;
        }

        public override string ActualDefinitionText => string.Empty;

        public override void SetProperty(IReplaceText replaceTextInstance, string value)
        {
            if (UnescapedValue.Equals(value, StringComparison.Ordinal))
            {
                // The property value is not changing. Leave things alone.
                return;
            }

            // Replace the empty tag in the buffer with a start/end element tag
            // and the new value
            string newText =
                $"""
                <{_fullyQualifiedPropertyName}>
                {EscapeXmlString(value)}
                </{_fullyQualifiedPropertyName}>
                """;

            // We know where to replace, so go ahead and do it.
            replaceTextInstance.ReplaceText(DefinitionStart, DefinitionEndPlusOne, newText);
        }
    }

    private class XamlReadWriteException : Exception
    {
        public XamlReadWriteException(string message)
            : base(message)
        {
        }
    }
}
