' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.Serialization

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A general-purpose exception for use by the property pages.  These exceptions are
    '''   intended to be shown to the end user in an error dialog, rather than to be
    '''   programmatically manipulated (although that of course is possible).
    ''' </summary>
    <Serializable>
    Public Class PropertyPageException
        Inherits ApplicationException

        Private _showHeaderAndFooterInErrorControl As Boolean = True

        Public Sub New(message As String)
            Me.New(message, Nothing, DirectCast(Nothing, Exception))
        End Sub

        Public Sub New(message As String, helpLink As String)
            Me.New(message, helpLink, Nothing)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            Me.New(message, Nothing, innerException)
        End Sub

        Public Sub New(message As String, helpLink As String, innerException As Exception)
            MyBase.New(message, innerException)
            Me.HelpLink = helpLink
        End Sub

        Public Sub New(message As String, innerException As Exception, ShowHeaderAndFooterInErrorControl As Boolean)
            MyBase.New(message, innerException)
            _showHeaderAndFooterInErrorControl = ShowHeaderAndFooterInErrorControl
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

        Public Property ShowHeaderAndFooterInErrorControl As Boolean
            Get
                Return _showHeaderAndFooterInErrorControl
            End Get
            Set
                _showHeaderAndFooterInErrorControl = value
            End Set
        End Property

    End Class

End Namespace
