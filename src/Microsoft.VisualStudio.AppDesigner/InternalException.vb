' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.Package

    ''' <summary>
    ''' Use this class sparingly.  It should only be used when there really shouldn't be any reason that the code path
    '''   can be hit, even given invalid user input, etc.  The default message for this error is "Unexpected error." 
    '''   (obviously not very helpful, but it doesn't make sense to localize and doc messages for errors which shouldn't
    '''   be happening).
    ''' </summary>
    <Serializable>
    Public Class InternalException
        Inherits ApplicationException

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <remarks>Message defaults to "Unexpected error."</remarks>
        Public Sub New()
            Me.New(My.Resources.Designer.RSE_Err_InternalException, Nothing)
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Message">The message for the exception.</param>
        Public Sub New(Message As String)
            Me.New(Message, Nothing)
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="InnerException">The inner exception, if any (Nothing = none).</param>
        ''' <remarks>Message defaults to "Unexpected error."</remarks>
        Public Sub New(InnerException As Exception)
            Me.New(My.Resources.Designer.RSE_Err_InternalException, InnerException)
        End Sub

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="Message">The message for the exception.</param>
        ''' <param name="InnerException">The inner exception, if any (Nothing = none).</param>
        Public Sub New(Message As String, InnerException As Exception)
            MyBase.New(Message, InnerException)
        End Sub

#Region "Serialization support"

        ''' <summary>
        '''  Constructor used for serialization
        ''' </summary>
        ''' <param name="info"></param>
        ''' <param name="context"></param>
        <EditorBrowsable(EditorBrowsableState.Advanced)>
        Protected Sub New(info As System.Runtime.Serialization.SerializationInfo, context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub

#End Region

    End Class

End Namespace
