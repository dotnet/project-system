' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml

Imports Microsoft.VisualStudio.Editors.Common.ArgumentValidation
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    ''' <summary>
    ''' When reading and writing the Application.xaml file, we have a requirement that
    '''   we do not change the user's formatting, comments, etc.  Thus we can't simply
    '''   read in the XML, modify it, and write it back out.  Instead, we have to
    '''   modify just the parts we need to directly in the text buffer.  This class
    '''   handles that surprisingly complex job.
    ''' </summary>
    Friend Class AppDotXamlDocument
        Implements IDebugLockCheck
        Implements IDisposable
        Implements IReplaceText

        'REFERENCES:
        '  XAML Overviews: http://windowssdk.msdn.microsoft.com/en-us/library/ms744825.aspx
        '  Property element syntax: http://windowssdk.msdn.microsoft.com/en-us/library/ms788723(VS.80).aspx#PESyntax

#Region "Interface IReplaceText"

        ''' <summary>
        ''' A simple interface to allow XamlProperty to ask the AppDotXamlDocument to 
        '''   make text replacements.
        ''' </summary>
        Friend Interface IReplaceText

            ''' <summary>
            ''' Replace the text at the given location in the buffer with new text.
            ''' </summary>
            ''' <param name="sourceStart"></param>
            ''' <param name="sourceEnd"></param>
            ''' <param name="newText"></param>
            Sub ReplaceText(sourceStart As Location, sourceEnd As Location, newText As String)

        End Interface

#End Region

#Region "Nested class 'Location'"

        ''' <summary>
        ''' Represents a position in a text buffer
        ''' </summary>
        Friend Class Location
            Public LineIndex As Integer 'Zero-based line #
            Public CharIndex As Integer 'Zero-based character on line

            Public Sub New(lineIndex As Integer, charOnLineIndex As Integer)
                If lineIndex < 0 Then
                    Throw CreateArgumentException(NameOf(lineIndex))
                End If
                If charOnLineIndex < 0 Then
                    Throw CreateArgumentException(NameOf(charOnLineIndex))
                End If

                Me.LineIndex = lineIndex
                CharIndex = charOnLineIndex
            End Sub

            ''' <summary>
            ''' Creates a location corresponding to the current location of the
            ''' XmlReader
            ''' </summary>
            ''' <param name="reader"></param>
            Public Sub New(reader As XmlReader)
                Me.New(CType(reader, IXmlLineInfo).LineNumber - 1, CType(reader, IXmlLineInfo).LinePosition - 1)
            End Sub

            Public Function Shift(charIndexToAdd As Integer) As Location
                Return New Location(LineIndex, CharIndex + charIndexToAdd)
            End Function

        End Class

#End Region

#Region "Nested class BufferLock"

        ''' <summary>
        ''' Used by the document to verify BufferLock is used when it's needed
        ''' </summary>
        Public Interface IDebugLockCheck
            Sub OnBufferLock()
            Sub OnBufferUnlock()
        End Interface

        ''' <summary>
        ''' We need to make sure the buffer doesn't change while we're looking up properties
        '''  and changing them.  Our XmlReader() needs to be in sync with the actual text
        '''  in the buffer.
        ''' This class keeps the buffer locked until it is disposed.
        ''' </summary>
        Private Class BufferLock
            Implements IDisposable

            Private _isDisposed As Boolean
            Private _buffer As IVsTextLines
            Private ReadOnly _debugLockCheck As IDebugLockCheck 'Used by the document to verify BufferLock is used when it's needed

            Public Sub New(buffer As IVsTextLines, debugLockCheck As IDebugLockCheck)
                Requires.NotNull(buffer, NameOf(buffer))
                Requires.NotNull(debugLockCheck, NameOf(debugLockCheck))

                _buffer = buffer
                _debugLockCheck = debugLockCheck

                _buffer.LockBuffer()
                _debugLockCheck.OnBufferLock()
            End Sub

#If DEBUG Then
            Protected Overrides Sub Finalize()
                Debug.Assert(_isDisposed, "Didn't dispose a BufferLock object")
            End Sub
#End If

#Region "IDisposable Support"

            Protected Overridable Sub Dispose(disposing As Boolean)
                Try
                    If disposing Then
                        If Not _isDisposed Then
                            _buffer.UnlockBuffer()
                            _debugLockCheck.OnBufferUnlock()
                            _buffer = Nothing
                        End If
                        _isDisposed = True
                    End If
                Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(Dispose), NameOf(AppDotXamlDocument))
                    Throw
                End Try
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub

#End Region

        End Class

#End Region

#Region "Nested class XamlProperty"

        ''' <summary>
        ''' Represents a property value found in the XAML file.
        ''' </summary>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")>
        Friend MustInherit Class XamlProperty
            Protected VsTextLines As IVsTextLines
            Private ReadOnly _definitionIncludesQuotes As Boolean
            Private ReadOnly _unescapedValue As String 'Unescaped, translated value of the property from the XmlReader
            Private ReadOnly _startLocation As Location
            Private ReadOnly _endLocationPlusOne As Location 'Points to the index *after* the last character in the range, just like IVsTextLines expects

            Public Sub New(vsTextLines As IVsTextLines, startLocation As Location, endLocation As Location, unescapedValue As String, definitionIncludesQuotes As Boolean)
                Requires.NotNull(vsTextLines, NameOf(vsTextLines))
                Requires.NotNull(startLocation, NameOf(startLocation))
                Requires.NotNull(endLocation, NameOf(endLocation))

                If unescapedValue Is Nothing Then
                    unescapedValue = ""
                End If

                _startLocation = startLocation
                _endLocationPlusOne = endLocation
                _unescapedValue = unescapedValue
                Me.VsTextLines = vsTextLines
                _definitionIncludesQuotes = definitionIncludesQuotes
            End Sub

            ''' <summary>
            ''' Retrieves the actual text for the value of the property, unescaped, as it
            '''   appears in the .xaml file.  If DefinitionIncludesQuotes=True, then this
            '''   includes the beginning/ending quote
            ''' </summary>
            Public Overridable ReadOnly Property ActualDefinitionText As String
                Get
                    Dim buffer As String = Nothing
                    ErrorHandler.ThrowOnFailure(VsTextLines.GetLineText(DefinitionStart.LineIndex, DefinitionStart.CharIndex, DefinitionEndPlusOne.LineIndex, DefinitionEndPlusOne.CharIndex, buffer))
                    Return buffer
                End Get
            End Property

            Public ReadOnly Property UnescapedValue As String
                Get
                    Return _unescapedValue
                End Get
            End Property

            Public ReadOnly Property DefinitionStart As Location
                Get
                    Return _startLocation
                End Get
            End Property

            Public ReadOnly Property DefinitionEndPlusOne As Location
                Get
                    Return _endLocationPlusOne
                End Get
            End Property

            ''' <summary>
            ''' Replace the property's value in the XAML
            ''' </summary>
            ''' <param name="replaceTextInstance"></param>
            ''' <param name="value"></param>
            Public Overridable Sub SetProperty(replaceTextInstance As IReplaceText, value As String)
                If UnescapedValue.Equals(value, StringComparison.Ordinal) Then
                    'The property value is not changing.  Leave things alone.
                    Return
                End If

                'Replace just the string in the buffer with the new value.
                Dim replaceStart As Location = DefinitionStart
                Dim replaceEnd As Location = DefinitionEndPlusOne
                Dim newText As String = EscapeXmlString(value)
                If _definitionIncludesQuotes Then
                    newText = """" & newText & """"
                End If

                'We know where to replace, so go ahead and do it.
                replaceTextInstance.ReplaceText(replaceStart, replaceEnd, newText)
            End Sub

        End Class

        ''' <summary>
        ''' Represents a property that was found in the XAML file in attribute syntax
        ''' </summary>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")>
        Friend Class XamlPropertyInAttributeSyntax
            Inherits XamlProperty

            Public Sub New(vsTextLines As IVsTextLines, definitionStart As Location, definitionEnd As Location, unescapedValue As String)
                MyBase.New(vsTextLines, definitionStart, definitionEnd, unescapedValue, definitionIncludesQuotes:=True)
            End Sub

        End Class

        ''' <summary>
        ''' Represents a property that was found in property element syntax with a start and end tag.
        ''' </summary>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")>
        Friend Class XamlPropertyInPropertyElementSyntax
            Inherits XamlProperty

            Public Sub New(vsTextLines As IVsTextLines, valueStart As Location, valueEnd As Location, unescapedValue As String)
                MyBase.New(vsTextLines, valueStart, valueEnd, unescapedValue, definitionIncludesQuotes:=False)
            End Sub
        End Class

        ''' <summary>
        ''' Represents a property that was found in property element syntax with an empty tag.
        ''' </summary>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")>
        Friend Class XamlPropertyInPropertyElementSyntaxWithEmptyTag
            Inherits XamlPropertyInPropertyElementSyntax

            'This class represents a property that was found in property element syntax with an empty tag,
            '  e.g. <Application.StartupUri/>

            Private ReadOnly _fullyQualifiedPropertyName As String

            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <param name="vsTextLines"></param>
            ''' <param name="elementStart"></param>
            ''' <param name="elementEnd"></param>
            ''' <remarks>
            ''' In the case of XamlPropertyInPropertyElementSyntaxWithEmptyTag, the elementStart/elementEnd
            '''   location pair indicates the start/end of the tag itself, not the value (since there is no
            '''   value - the element is an empty tag).
            ''' </remarks>
            Public Sub New(vsTextLines As IVsTextLines, fullyQualifiedPropertyName As String, elementStart As Location, elementEnd As Location)
                MyBase.New(vsTextLines, elementStart, elementEnd, unescapedValue:="")

                Requires.NotNull(fullyQualifiedPropertyName, NameOf(fullyQualifiedPropertyName))

                _fullyQualifiedPropertyName = fullyQualifiedPropertyName
            End Sub

            Public Overrides ReadOnly Property ActualDefinitionText As String
                Get
                    Return ""
                End Get
            End Property

            Public Overrides Sub SetProperty(replaceTextInstance As IReplaceText, value As String)
                If UnescapedValue.Equals(value, StringComparison.Ordinal) Then
                    'The property value is not changing.  Leave things alone.
                    Return
                End If

                'Replace the empty tag in the buffer with a start/end element tag
                '  and the new value
                Dim replaceStart As Location = DefinitionStart
                Dim replaceEnd As Location = DefinitionEndPlusOne
                Dim newText As String =
                    "<" & _fullyQualifiedPropertyName & ">" _
                    & EscapeXmlString(value) _
                    & "</" & _fullyQualifiedPropertyName & ">"

                'We know where to replace, so go ahead and do it.
                replaceTextInstance.ReplaceText(replaceStart, replaceEnd, newText)
            End Sub

        End Class

#End Region

        Private Const ELEMENT_APPLICATION As String = "Application"
        Private Const PROPERTY_STARTUPURI As String = "StartupUri"
        Private Const PROPERTY_SHUTDOWNMODE As String = "ShutdownMode"

        'A pointer to the text buffer as IVsTextLines
        Private _vsTextLines As IVsTextLines
        Private _isDisposed As Boolean

#Region "Constructor"

        Public Sub New(vsTextLines As IVsTextLines)
            Requires.NotNull(vsTextLines, NameOf(vsTextLines))
            _vsTextLines = vsTextLines
        End Sub

#End Region

#Region "Dispose"

        Protected Overrides Sub Finalize()
            Debug.Assert(_isDisposed, "Didn't dispose an AppDotXamlDocument object")
        End Sub

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _isDisposed Then
                If disposing Then
                    Debug.Assert(_debugBufferLockCount = 0, "Missing buffer unlock")
                End If

                _vsTextLines = Nothing
                _isDisposed = True
            End If
            _isDisposed = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

#Region "GetText/GetAllText utilities"

        ''' <summary>
        ''' Retrieves the text between the given buffer line/char points
        ''' </summary>
        ''' <param name="startLine"></param>
        ''' <param name="startIndex"></param>
        ''' <param name="endLine"></param>
        ''' <param name="endIndex"></param>
        Private Function GetText(startLine As Integer, startIndex As Integer, endLine As Integer, endIndex As Integer) As String
            Dim text As String = Nothing
            ErrorHandler.ThrowOnFailure(_vsTextLines.GetLineText(startLine, startIndex, endLine, endIndex, text))
            Return text
        End Function

        ''' <summary>
        ''' Retrieves the text starting at the given point and with the given length
        ''' </summary>
        ''' <param name="startLine"></param>
        ''' <param name="startIndex"></param>
        ''' <param name="count">Count of characters to return</param>
        Private Function GetText(startLine As Integer, startIndex As Integer, count As Integer) As String
            Return GetText(startLine, startIndex, startLine, startIndex + count)
        End Function

        ''' <summary>
        ''' Retrieves the text between the given buffer line/char points
        ''' </summary>
        Private Function GetText(startLocation As Location, endLocation As Location) As String
            Return GetText(startLocation.LineIndex, startLocation.CharIndex, endLocation.LineIndex, endLocation.CharIndex)
        End Function

        ''' <summary>
        ''' Retrieves the text starting at the given point and with the given length
        ''' </summary>
        Private Function GetText(startLocation As Location, count As Integer) As String
            Return GetText(startLocation.LineIndex, startLocation.CharIndex, count)
        End Function

        ''' <summary>
        ''' Retrieves all of the text in the buffer
        ''' </summary>
        Private Function GetAllText() As String
            Dim lastLine, lastIndex As Integer
            ErrorHandler.ThrowOnFailure(_vsTextLines.GetLastLineIndex(lastLine, lastIndex))
            Return GetText(0, 0, lastLine, lastIndex)
        End Function

#End Region

#Region "Escaping/Unescaping XML strings"

        ''' <summary>
        ''' Escape a string in XML format, including double and single quotes
        ''' </summary>
        ''' <param name="value"></param>
        Public Shared Function EscapeXmlString(value As String) As String
            If value Is Nothing Then
                value = String.Empty
            End If

            Dim sb As New StringBuilder()
            Dim settings As New XmlWriterSettings With {
                .ConformanceLevel = ConformanceLevel.Fragment
            }
            Dim xmlWriter As XmlWriter = XmlWriter.Create(sb, settings)
            xmlWriter.WriteString(value)
            xmlWriter.Close()
            Dim escapedString As String = sb.ToString()

            'Now escape double and single quotes
            Return escapedString.Replace("""", "&quot;").Replace("'", "&apos;")
        End Function

        ''' <summary>
        ''' Unescapes an element's content value from escaped XML format.
        ''' </summary>
        ''' <param name="value"></param>
        Public Shared Function UnescapeXmlContent(value As String) As String
            If value Is Nothing Then
                value = String.Empty
            End If

            'Escape any double quotes

            'Make as content of an element
            Dim xml As String = "<a>" & value & "</a>"
            Dim stringReader As New StringReader(xml)
            Dim settings As New XmlReaderSettings With {
                .ConformanceLevel = ConformanceLevel.Fragment
            }
            Dim xmlReader As XmlReader = XmlReader.Create(stringReader, settings)
            xmlReader.ReadToFollowing("a")

            Dim content As String = xmlReader.ReadElementContentAsString()
            Return content
        End Function

#End Region

        ''' <summary>
        ''' Finds the Application element that all application.xaml files must include as
        '''   the single root node.
        ''' </summary>
        ''' <param name="reader"></param>
        Public Shared Sub MoveToApplicationRootElement(reader As XmlTextReader)
            'XAML files must have only one root element.  For app.xaml, it must be "Application"
            If reader.MoveToContent() = XmlNodeType.Element And reader.Name = ELEMENT_APPLICATION Then
                'Okay
                Return
            End If

            Throw New XamlReadWriteException(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_Xaml_CouldntFindRootElement, ELEMENT_APPLICATION))
        End Sub

#Region "CreateXmlTextReader"

        ''' <summary>
        ''' Creates an XmlTextReader for the text
        '''   in the buffer.
        ''' </summary>
        Private Function CreateXmlTextReader() As XmlTextReader
            Debug.Assert(_debugBufferLockCount > 0, "Should be using BufferLock!")
            Dim stringReader As New StringReader(GetAllText())
            ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
            Dim xmlTextReader As XmlTextReader = New XmlTextReader(stringReader) With {
                .DtdProcessing = DtdProcessing.Prohibit
            }
            Return xmlTextReader
        End Function

#End Region

#Region "Helpers for reading properties"

        ''' <summary>
        ''' Finds the value of the given property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        Public Function GetApplicationPropertyValue(propertyName As String) As String
            Using New BufferLock(_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                Dim prop As XamlProperty = FindApplicationPropertyInXaml(reader, propertyName)
                If prop IsNot Nothing Then
                    Return prop.UnescapedValue
                Else
                    Return String.Empty
                End If
            End Using
        End Function

        ''' <summary>
        ''' Find the closing angle bracket on a single line.
        ''' See comments on FindClosingAngleBracket.
        ''' </summary>
        ''' <param name="line"></param>
        ''' <returns>The index on the line where the closing angle bracket is found, or -1 if not found.</returns>
        Public Shared Function FindClosingAngleBracketHelper(line As String) As Integer
            Dim index As Integer = 0

            Const SingleQuote As Char = "'"c
            Const DoubleQuote As Char = """"c
            While index < line.Length
                'Find the next character of interest
                index = line.IndexOfAny(
                    New Char() {SingleQuote, DoubleQuote, "/"c, ">"c},
                    index)
                If index < 0 Then
                    Return -1
                End If

                Dim characterOfInterest As Char = line.Chars(index)
                Select Case characterOfInterest
                    Case SingleQuote, DoubleQuote
                        'We have a string.  Skip past it.
                        Dim closingQuote As Integer = line.IndexOf(
                            characterOfInterest,
                            index + 1)
                        If closingQuote < 0 Then
                            'String not terminated.
                            Return -1
                        Else
                            index = closingQuote + 1
                        End If
                    Case ">"c
                        'Found ">"
                        Return index
                    Case "/"c
                        If line.Substring(index).StartsWith("/>") Then
                            'Found "/>"
                            Return index
                        Else
                            'Keep searching past the '/'
                            index += 1
                        End If
                    Case Else
                        Debug.Fail("Shouldn't reach here")
                End Select

            End While

            Return -1
        End Function

        ''' <summary>
        ''' Searches forward from the given location, skipping quoted strings
        '''   (single and double quoted), until it finds a closing angle 
        '''   bracket (">" or "/">).
        ''' </summary>
        ''' <param name="startLocation"></param>
        ''' <returns>The location of the found ">" or "/>".  If it is not found, returns Nothing.</returns>
        ''' <remarks>
        ''' It's assumed that the XML is well-formed
        ''' </remarks>
        Public Function FindClosingAngleBracket(startLocation As Location) As Location
            Dim iLastLine, iLastIndex As Integer
            ErrorHandler.ThrowOnFailure(_vsTextLines.GetLastLineIndex(iLastLine, iLastIndex))

            For iLine As Integer = startLocation.LineIndex To iLastLine
                Dim iStartIndexForLine, iEndIndexForLine As Integer

                If iLine = startLocation.LineIndex Then
                    iStartIndexForLine = startLocation.CharIndex
                Else
                    iStartIndexForLine = 0
                End If

                If iLine = iLastLine Then
                    iEndIndexForLine = iLastIndex
                Else
                    Dim iLineLength As Integer
                    ErrorHandler.ThrowOnFailure(_vsTextLines.GetLengthOfLine(iLine, iLineLength))
                    iEndIndexForLine = iLineLength
                End If

                Dim lineText As String = GetText(iLine, iStartIndexForLine, iLine, iEndIndexForLine)

                Dim foundIndex As Integer = FindClosingAngleBracketHelper(lineText)
                If foundIndex >= 0 Then
                    Return New Location(iLine, iStartIndexForLine + foundIndex)
                End If
            Next

            'Not found
            Return Nothing
        End Function

        ''' <summary>
        ''' From the root of a document, finds the given attribute inside the Application
        '''   element, if it exists.  If not, returns Nothing.
        ''' </summary>
        ''' <param name="reader"></param>
        Public Function FindApplicationPropertyInXaml(reader As XmlTextReader, propertyName As String) As XamlProperty
            MoveToApplicationRootElement(reader)
            Dim prop As XamlProperty = FindPropertyAsAttributeInCurrentElement(reader, ELEMENT_APPLICATION, propertyName)
            If prop Is Nothing Then
                prop = FindPropertyAsChildElementInCurrentElement(reader, ELEMENT_APPLICATION, propertyName)
            End If

            Return prop
        End Function

        ''' <summary>
        ''' From the root of a document, finds the given attribute inside the Application
        '''   element, if it exists.  If not, returns Nothing.
        ''' </summary>
        ''' <param name="reader"></param>
        Private Function FindPropertyAsAttributeInCurrentElement(reader As XmlTextReader, optionalPropertyQualifier As String, propertyName As String) As XamlPropertyInAttributeSyntax
            'Look for either simple attribute syntax (StartupUri=xxx) or
            '  fully-qualified attribute syntax (Application.StartupUri=xxx)
            Dim fullyQualifiedPropertyName As String = Nothing
            If optionalPropertyQualifier <> "" Then
                fullyQualifiedPropertyName = optionalPropertyQualifier & "." & propertyName
            End If

            Dim foundPropertyName As String = Nothing
            If reader.MoveToAttribute(propertyName) Then
                foundPropertyName = propertyName
            ElseIf fullyQualifiedPropertyName <> "" AndAlso reader.MoveToAttribute(fullyQualifiedPropertyName) Then
                foundPropertyName = fullyQualifiedPropertyName
            Else
                'Not found
                Return Nothing
            End If

            Dim startLocation As New Location(reader)
            Dim boundedEndLocation As Location

            'Remember the quote character actually found in the XML
            Dim quoteCharacterUsedByAttribute As String = reader.QuoteChar

            'Remember the actual value of the property
            Dim unescapedValue As String = reader.Value

            'Find the end location of the attribute
            If reader.MoveToNextAttribute() Then
                boundedEndLocation = New Location(reader)
            Else
                reader.Read()
                boundedEndLocation = New Location(reader)
                Debug.Assert(boundedEndLocation.LineIndex >= startLocation.LineIndex)
                Debug.Assert(boundedEndLocation.LineIndex > startLocation.LineIndex _
                    OrElse boundedEndLocation.CharIndex > startLocation.CharIndex)
            End If

            'Now we have an approximate location.  Find the exact location.
            Dim vsTextFind As IVsTextFind = TryCast(_vsTextLines, IVsTextFind)
            If vsTextFind Is Nothing Then
                Debug.Fail("IVsTextFind not supported?")
                Throw New InvalidOperationException()
            Else
                'startLocation should be pointing to the attribute name.  Verify that.
                Dim afterAttributeName As New Location(startLocation.LineIndex, startLocation.CharIndex + foundPropertyName.Length)
#If DEBUG Then
                Dim attributeNameBuffer As String = Nothing
                If ErrorHandler.Failed(_vsTextLines.GetLineText(startLocation.LineIndex, startLocation.CharIndex, afterAttributeName.LineIndex, afterAttributeName.CharIndex, attributeNameBuffer)) _
                            OrElse Not foundPropertyName.Equals(attributeNameBuffer, StringComparison.Ordinal) Then
                    Debug.Fail("Didn't find the attribute name at the expected location")
                End If
#End If

                'Find the equals sign ('=')
                Dim equalsLine, equalsIndex As Integer
                Const EqualsSign As String = "="
                If ErrorHandler.Failed(vsTextFind.Find(EqualsSign, afterAttributeName.LineIndex, afterAttributeName.CharIndex, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, equalsLine, equalsIndex)) Then
                    ThrowUnexpectedFormatException(startLocation)
                End If
                Debug.Assert(EqualsSign.Equals(GetText(equalsLine, equalsIndex, 1), StringComparison.Ordinal))

                'Find the starting quote
                Dim startQuoteLine, startQuoteIndex As Integer
                If ErrorHandler.Failed(vsTextFind.Find(quoteCharacterUsedByAttribute, equalsLine, equalsIndex, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, startQuoteLine, startQuoteIndex)) Then
                    ThrowUnexpectedFormatException(startLocation)
                End If
                Debug.Assert(quoteCharacterUsedByAttribute.Equals(GetText(startQuoteLine, startQuoteIndex, 1), StringComparison.Ordinal))

                'Find the ending quote, assuming it's on the same line
                Dim endQuoteLine, endQuoteIndex As Integer
                If ErrorHandler.Failed(vsTextFind.Find(quoteCharacterUsedByAttribute, startQuoteLine, startQuoteIndex + 1, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, endQuoteLine, endQuoteIndex)) Then
                    ThrowUnexpectedFormatException(startLocation)
                End If
                Debug.Assert(quoteCharacterUsedByAttribute.Equals(GetText(endQuoteLine, endQuoteIndex, 1), StringComparison.Ordinal))

                'Now we have the start and end of the attribute's value definition
                Dim valueStart As New Location(startQuoteLine, startQuoteIndex)
                Dim valueEnd As New Location(endQuoteLine, endQuoteIndex + 1)
                Return New XamlPropertyInAttributeSyntax(_vsTextLines, valueStart, valueEnd, unescapedValue)
            End If
        End Function

        ''' <summary>
        ''' From the root of a document, finds the given attribute inside the Application
        '''   element, using property element syntax, if it exists.  If not, returns Nothing.
        ''' </summary>
        ''' <param name="reader"></param>
        Private Function FindPropertyAsChildElementInCurrentElement(reader As XmlTextReader, propertyQualifier As String, propertyName As String) As XamlPropertyInPropertyElementSyntax
            'See http://windowssdk.msdn.microsoft.com/en-us/library/ms788723(VS.80).aspx#PESyntax
            '
            'Looking for something of this form:
            '  <Application xmlns=...>
            '    <Application.StartupUri>MainWindow.xaml</Application.StartupUri>
            '  </Application>

            'In this case, the "Application." prefix is required, not optional.
            Dim fullyQualifiedPropertyName As String = propertyQualifier & "." & propertyName

            If reader.ReadToDescendant(fullyQualifiedPropertyName) Then
                'Found

                Dim tagStart As New Location(reader)
                Dim tagEnd As New Location(reader)

                Dim startTagEndingBracketLocation As Location = FindClosingAngleBracket(tagStart)
                If startTagEndingBracketLocation Is Nothing Then
                    ThrowUnexpectedFormatException(tagStart)
                End If

                If reader.IsEmptyElement Then
                    'It's an empty tag of the form <xyz/>.  The reader is at the 'x' in "xyz", so the
                    '  beginning is at -1 from that location.
                    Dim elementTagStart As Location = New Location(reader).Shift(-1)

                    'Read through the start tag
                    If Not reader.Read() Then
                        ThrowUnexpectedFormatException(tagStart)
                    End If

                    'The reader is now right after the empty element tag
                    Dim elementTagEndPlusOne As New Location(reader)
                    Return New XamlPropertyInPropertyElementSyntaxWithEmptyTag(
                        _vsTextLines, fullyQualifiedPropertyName,
                        elementTagStart, elementTagEndPlusOne)
                Else
                    Dim valueStart As Location
                    Dim unescapedValue As String

                    'Find the start of the content (reader's location after doing a Read through
                    '  the element will not give us reliable results, since it depends on the type of
                    '  node following the start tag).
                    valueStart = FindClosingAngleBracket(New Location(reader)).Shift(1) '+1 to get past the ">"

                    'Read through the start tag
                    If Not reader.Read() Then
                        ThrowUnexpectedFormatException(tagStart)
                    End If

                    'Unfortunately, simply doing a ReadInnerXml() will take us too far.  We need to know
                    '  exactly where the value ends in the text, so we'll read manually.

                    While reader.NodeType <> XmlNodeType.EndElement OrElse Not fullyQualifiedPropertyName.Equals(reader.Name, StringComparison.Ordinal)
                        If Not reader.Read() Then
                            'End tag not found
                            ThrowUnexpectedFormatException(tagStart)
                        End If
                    End While

                    'Reader is at location 'x' of </xyz>.  So we want -2 from this location.
                    Dim currentPosition2 As New Location(reader)
                    Dim valueEndPlusOne As Location = New Location(reader).Shift(-2)

                    'Get the inner text and unescape it.
                    Dim innerText As String = GetText(valueStart, valueEndPlusOne)
                    unescapedValue = UnescapeXmlContent(innerText).Trim()

                    Return New XamlPropertyInPropertyElementSyntax(_vsTextLines, valueStart, valueEndPlusOne, unescapedValue:=unescapedValue)
                End If
            End If

            Return Nothing
        End Function

#End Region

#Region "Helpers for writing properties"

        ''' <summary>
        ''' Returns the location where a new attribute can be added to the
        '''   Application root element.  Returns Nothing if can't find the
        '''   correct position.
        ''' </summary>
        Private Function FindLocationToAddNewApplicationAttribute() As Location
            Using New BufferLock(_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                MoveToApplicationRootElement(reader)
                Return FindClosingAngleBracket(New Location(reader))
            End Using
        End Function

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        Public Sub SetApplicationPropertyValue(propertyName As String, value As String)
            If value Is Nothing Then
                value = ""
            End If

            Using New BufferLock(_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                Dim prop As XamlProperty = FindApplicationPropertyInXaml(reader, propertyName)

                If prop IsNot Nothing Then
                    'The property is already in the .xaml.
                    prop.SetProperty(Me, value)
                Else
                    'It's not in the .xaml yet.  We'll add this xxx=yyy definition
                    '  as the last attribute in the Application element.
                    If value = "" Then
                        'The new value is blank, just like the current value.
                        '  Don't change anything.
                        Return
                    End If

                    Dim replaceStart As Location = FindLocationToAddNewApplicationAttribute()
                    If replaceStart Is Nothing Then
                        ThrowUnexpectedFormatException(New Location(0, 0))
                    End If
                    Dim replaceEnd As Location = replaceStart
                    Dim newText As String = propertyName & "=""" & EscapeXmlString(value) & """"

                    'Is the anything non-whitespace on the line where we're adding the
                    '  new code?  If so, put in a CR/LF pair before it.
                    If replaceStart.CharIndex > 0 Then
                        Dim lineTextBeforeInsertion As String = GetText(replaceStart.LineIndex, 0, replaceStart.LineIndex, replaceStart.CharIndex)
                        If lineTextBeforeInsertion.Trim().Length > 0 Then
                            newText = vbCrLf & newText
                        End If
                    End If

                    'We know where to replace, so go ahead and do it.
                    ReplaceText(replaceStart, replaceEnd, newText)
                End If

#If DEBUG Then
                Dim newPropertyValue As String = "(error)"
                Try
                    newPropertyValue = GetApplicationPropertyValue(propertyName)
                Catch ex As Exception When Common.ReportWithoutCrash(ex, "Got an exception trying to verify the new property value in SetApplicationPropertyValue", NameOf(AppDotXamlDocument))
                End Try

                If Not value.Equals(newPropertyValue, StringComparison.Ordinal) Then
                    Debug.Fail("SetApplicationPropertyValue() didn't seem to work properly.  New .xaml text: " & vbCrLf & GetAllText())
                End If
#End If
            End Using
        End Sub

        ''' <summary>
        ''' Replace the text at the given location in the buffer with new text.
        ''' </summary>
        Private Sub ReplaceText(sourceStart As Location, sourceLength As Integer, newText As String)
            ReplaceText(sourceStart, New Location(sourceStart.LineIndex, sourceStart.CharIndex + sourceLength), newText)
        End Sub

        ''' <summary>
        ''' Replace the text at the given location in the buffer with new text.
        ''' </summary>
        ''' <param name="sourceStart"></param>
        ''' <param name="sourceEnd"></param>
        ''' <param name="newText"></param>
        Private Sub ReplaceText(sourceStart As Location, sourceEnd As Location, newText As String) Implements IReplaceText.ReplaceText
            If newText Is Nothing Then
                newText = String.Empty
            End If

            Dim bstrNewText As IntPtr = Marshal.StringToBSTR(newText)
            Try
                ErrorHandler.ThrowOnFailure(_vsTextLines.ReplaceLines(
                    sourceStart.LineIndex, sourceStart.CharIndex,
                    sourceEnd.LineIndex, sourceEnd.CharIndex,
                    bstrNewText, newText.Length, Nothing))
            Finally
                Marshal.FreeBSTR(bstrNewText)
            End Try
        End Sub

        ''' <summary>
        ''' Given the location of the start of an element tag, makes sure that it has an end tag.
        '''   If the element tag is closed by "/>" instead of an end element, it is expanded
        '''   into a start and end tag.
        ''' </summary>
        ''' <param name="tagStartLocation"></param>
        ''' <param name="elementName">The name of the element at the given location</param>
        Public Sub MakeSureElementHasStartAndEndTags(tagStartLocation As Location, elementName As String)
            If Not "<".Equals(GetText(tagStartLocation, 1), StringComparison.Ordinal) Then
                Debug.Fail("MakeSureElementHasStartAndEndTags: The start location doesn't point to the start of an element tag")
                ThrowUnexpectedFormatException(tagStartLocation)
            End If

            Dim startTagEndingBracketLocation As Location = FindClosingAngleBracket(tagStartLocation)
            If startTagEndingBracketLocation Is Nothing Then
                ThrowUnexpectedFormatException(tagStartLocation)
            End If

            If ">".Equals(GetText(startTagEndingBracketLocation, 1), StringComparison.Ordinal) Then
                'The element tag is of the <xxx> form.  We assume that there is an ending </xxx> tag, and
                '  we don't need to do anything.
            Else
                'It must be an empty tag of the <xxx/> form.
                Const SlashAndEndBracket As String = "/>"
                If Not SlashAndEndBracket.Equals(GetText(startTagEndingBracketLocation, SlashAndEndBracket.Length), StringComparison.Ordinal) Then
                    Debug.Fail("FindClosingAngleBracket returned the wrong location?")
                    ThrowUnexpectedFormatException(startTagEndingBracketLocation)
                End If

                'We need to change <xxx attributes/> into <xxx attributes></xxx>
                Dim newText As String = "></" & elementName & ">"
                ReplaceText(startTagEndingBracketLocation, SlashAndEndBracket.Length, newText)
            End If
        End Sub

#End Region

#Region "StartupUri"

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        Public Function GetStartupUri() As String
            Return GetApplicationPropertyValue(PROPERTY_STARTUPURI)
        End Function

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        Public Sub SetStartupUri(value As String)
            SetApplicationPropertyValue(PROPERTY_STARTUPURI, value)
        End Sub

#End Region

#Region "ShutdownMode"

        ''' <summary>
        ''' Finds the value of the ShutdownMode property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        Public Function GetShutdownMode() As String
            Return GetApplicationPropertyValue(PROPERTY_SHUTDOWNMODE)
        End Function

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        Public Sub SetShutdownMode(value As String)
            SetApplicationPropertyValue(PROPERTY_SHUTDOWNMODE, value)
        End Sub

#End Region

#Region "Throwing exceptions"

        ''' <summary>
        ''' Throw an exception for an unexpected format, with line/col information
        ''' </summary>
        ''' <param name="location"></param>
        Private Shared Sub ThrowUnexpectedFormatException(location As Location)
            Throw New XamlReadWriteException(
                My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_Xaml_UnexpectedFormat_2Args,
                    CStr(location.LineIndex + 1), CStr(location.CharIndex + 1)))
        End Sub

#End Region

#Region "Validation"

        ''' <summary>
        ''' Verify the validity of the Application.xaml file, and throw an exception if
        '''   problems are found.
        ''' </summary>
        Public Sub VerifyAppXamlIsValidAndThrowIfNot()
            Using New BufferLock(_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                MoveToApplicationRootElement(reader)

                'Read through the Application element, including any child elements, to
                '  ensure everything is properly closed.
                'The name of the element to find is irrelevant, as there shouldn't be
                '  any elements following Application.
                reader.ReadToFollowing("Dummy Element")

                'If we made it to here, the .xaml file should be well-formed enough for us to read
                '  it properly.  As a final check, though, try getting some common properties.
                Call GetStartupUri()
                Call GetShutdownMode()
            End Using
        End Sub

#End Region

#Region "IDebugLockCheck"

        Private _debugBufferLockCount As Integer

        Public Sub OnBufferLock() Implements IDebugLockCheck.OnBufferLock
            _debugBufferLockCount += 1
        End Sub

        Public Sub OnBufferUnlock() Implements IDebugLockCheck.OnBufferUnlock
            _debugBufferLockCount -= 1
            Debug.Assert(_debugBufferLockCount >= 0, "Extra buffer unlock")
        End Sub

#End Region

    End Class

End Namespace
