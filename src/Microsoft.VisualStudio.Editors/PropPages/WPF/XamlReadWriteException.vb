' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.Serialization

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    <Serializable>
    Friend Class XamlReadWriteException
        Inherits PropertyPageException

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        ''' <summary>
        ''' Deserialization constructor.  Required for serialization/remotability support
        '''   (not that we expect this to be needed).
        ''' </summary>
        ''' <param name="Info"></param>
        ''' <param name="Context"></param>
        ''' <remarks>
        '''See .NET Framework Developer's Guide, "Custom Serialization" for more information
        ''' </remarks>
        Protected Sub New(Info As SerializationInfo, Context As StreamingContext)
            MyBase.New(Info, Context)
        End Sub

    End Class

End Namespace
