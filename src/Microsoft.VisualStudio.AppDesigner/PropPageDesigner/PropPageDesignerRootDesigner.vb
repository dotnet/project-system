' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ' {E18B7249-8322-44c3-9A57-FE5FF3889F89}
    'static const GUID <<name>> = 
    '{ 0xe18b7249, 0x8322, 0x44c3, { 0x9a, 0x57, 0xfe, 0x5f, 0xf3, 0x88, 0x9f, 0x89 } };

    ''' <summary>
    ''' This is the designer for the top-level resource editor component (PropPageDesigner).  I.e., this
    ''' is the top-level designer.  
    ''' </summary>
    Public NotInheritable Class PropPageDesignerRootDesigner
        Inherits AppDesDesignerFramework.BaseRootDesigner
        Implements IRootDesigner

        'The view associated with this root designer.
        Private _view As PropPageDesignerView

        ''' <summary>
        ''' Returns the PropPageDesignerRootComponent component that is being edited by this designer.
        ''' </summary>
        ''' <value>The PropPageDesignerRootComponent object.</value>
        Public Shadows ReadOnly Property Component As PropPageDesignerRootComponent
            Get
                Dim RootComponent As PropPageDesignerRootComponent = CType(MyBase.Component, PropPageDesignerRootComponent)
                Debug.Assert(RootComponent IsNot Nothing)
                Return RootComponent
            End Get
        End Property

        ''' <summary>
        ''' Commits any current changes in the editor to the backing docdata 
        ''' the docdata is then persisted separately
        ''' </summary>
        ''' <remarks>
        '''This should be done before attempting to persist.
        ''' </remarks>
        Public Shared Sub CommitAnyPendingChanges()
            'CONSIDER: We should force an apply here
            'GetView().CommitAnyPendingChanges()
        End Sub

        ''' <summary>
        ''' Called by the managed designer mechanism to determine what kinds of view technologies we support.
        ''' We currently support only Windows Forms technology (i.e., our designer view, ResourceEditorView,
        ''' inherits from System.Windows.Forms.Control)
        ''' </summary>
        ''' <remarks>
        ''' The view technology we support, which is currently only Windows Forms
        ''' </remarks>
        Private ReadOnly Property IRootDesigner_SupportedTechnologies As ViewTechnology() Implements IRootDesigner.SupportedTechnologies
            Get
                Return New ViewTechnology() {ViewTechnology.Default}
            End Get
        End Property

        ''' <summary>
        '''   Called by the managed designer technology to get our view, or the actual control that implements
        '''   our resource editor's designer surface.  In this case, we return an instance of ResourceEditorView.
        ''' </summary>
        ''' <param name="Technology"></param>
        ''' <remarks>
        '''   The newly-instantiated ResourceEditorView object.
        ''' </remarks>
        Private Function RootDesigner_GetView(Technology As ViewTechnology) As Object Implements IRootDesigner.GetView
            If Technology <> ViewTechnology.Default Then
                Throw New ArgumentException("Not a supported view technology", NameOf(Technology))
            End If

            If _view Is Nothing Then
                _view = New PropPageDesignerView(Me)
            End If

            Return _view
        End Function

        ''' <summary>
        ''' Wrapper function to expose our UI object
        ''' </summary>
        Public Function GetView() As PropPageDesignerView
            Return CType(RootDesigner_GetView(ViewTechnology.Default), PropPageDesignerView)
        End Function

        ''' <summary>
        '''  Exposes GetService from ComponentDesigner to other classes in this assembly to get a service.
        ''' </summary>
        ''' <param name="ServiceType">The type of the service being asked for.</param>
        ''' <returns>The requested service, if it exists.</returns>
        Public Shadows Function GetService(ServiceType As Type) As Object
            Return MyBase.GetService(ServiceType)
        End Function

#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Disposes of the root designer
        ''' </summary>
        ''' <param name="Disposing"></param>
        Protected Overloads Overrides Sub Dispose(Disposing As Boolean)
            If Disposing Then
                If _view IsNot Nothing Then
                    _view.Dispose()
                    _view = Nothing
                End If
            End If

            MyBase.Dispose(Disposing)
        End Sub
#End Region

    End Class

End Namespace
