' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.IO
Imports System.Xml.Serialization

Namespace Microsoft.VisualStudio.Editors.MyApplication

    ''' <summary>
    ''' Utility class to (de)serialize the contents of a DesignTimeSetting object 
    ''' given a stream reader/writer
    ''' </summary>
    Friend NotInheritable Class MyApplicationSerializer

        ''' <summary>
        '''  Deserialize XML stream of MyApplication data
        ''' </summary>
        ''' <param name="Reader">Text reader on stream containing object info</param>
        Public Shared Function Deserialize(Reader As TextReader) As MyApplicationData
            Dim serializer As XmlSerializer = New MyApplicationDataSerializer()
            'XmlSerializer(GetType(MyApplicationData))
            Dim xmlReader As Xml.XmlReader = Xml.XmlReader.Create(Reader)
            Return DirectCast(serializer.Deserialize(xmlReader), MyApplicationData)
        End Function

        ''' <summary>
        '''  Serialize MyApplication instance
        ''' </summary>
        ''' <param name="data">Instance to serialize</param>
        ''' <param name="Writer">Text writer on stream to serialize MyApplicationData to</param>
        Public Shared Sub Serialize(data As MyApplicationData, Writer As TextWriter)
            Dim serializer As XmlSerializer = New MyApplicationDataSerializer()
            'New XmlSerializer(GetType(MyApplicationData))
            serializer.Serialize(Writer, data)
        End Sub

    End Class
End Namespace
