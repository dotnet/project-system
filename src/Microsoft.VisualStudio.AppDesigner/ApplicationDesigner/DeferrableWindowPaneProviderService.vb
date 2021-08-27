' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ' <devdoc>
    '     This class provides a Window pane provider service that can
    '     create a ApplicationDesignerWindowPane
    '     This allows us to have more control of the WindowPane
    ' </devdoc>
    Public NotInheritable Class DeferrableWindowPaneProviderService
        Inherits WindowPaneProviderService

        Private ReadOnly _docData As DocData

        ' <devdoc>
        '     Creates a new DeferrableWindowPaneProviderService.
        ' </devdoc>
        Public Sub New(provider As IServiceProvider, docData As DocData)
            MyBase.New(provider, Nothing)
            _docData = docData
        End Sub

        ' <devdoc>
        '     We override this to always get the current extension from the doc data.
        ' </devdoc>
        Protected Overrides ReadOnly Property Extension As String
            Get
                If _docData Is Nothing Then
                    Return ""
                End If
                Return _docData.Name
            End Get
        End Property

        ' <devdoc>
        '     We override this.  If the file extension is empty, it means
        '     we don't have a file yet so we should create a deferred pane.
        '     If we do have a file, we just forward to the base implementation
        '     because we're already loaded and there is no need to interfere.
        ' </devdoc>
        Public Overrides Function CreateWindowPane(surface As DesignSurface) As DesignerWindowPane
            Return New ApplicationDesignerWindowPane(surface)
        End Function

    End Class

End Namespace
