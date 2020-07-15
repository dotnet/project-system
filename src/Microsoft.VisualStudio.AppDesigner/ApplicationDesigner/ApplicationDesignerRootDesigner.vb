' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ' {E18B7249-8322-44c3-9A57-FE5FF3889F89}
    'static const GUID <<name>> = 
    '{ 0xe18b7249, 0x8322, 0x44c3, { 0x9a, 0x57, 0xfe, 0x5f, 0xf3, 0x88, 0x9f, 0x89 } };

    ''' <summary>
    ''' This is the designer for the top-level resource editor component (ApplicationDesigner).  I.e., this
    ''' is the top-level designer.  
    ''' </summary>
    Public NotInheritable Class ApplicationDesignerRootDesigner
        Inherits AppDesDesignerFramework.BaseRootDesigner
        Implements IRootDesigner

        'The view associated with this root designer.
        Private _view As ApplicationDesignerView

        ''' <summary>
        ''' Returns the ApplicationDesignerRootComponent component that is being edited by this designer.
        ''' </summary>
        ''' <value>The ApplicationDesignerRootComponent object.</value>
        Public Shadows ReadOnly Property Component As ApplicationDesignerRootComponent
            Get
                Dim RootComponent As ApplicationDesignerRootComponent = CType(MyBase.Component, ApplicationDesignerRootComponent)
                Debug.Assert(RootComponent IsNot Nothing)
                Return RootComponent
            End Get
        End Property

        ''' <summary>
        ''' Designer initialization code
        ''' </summary>
        ''' <param name="component"></param>
        ''' <remarks>Defers to base class </remarks>
        Public Overrides Sub Initialize(component As IComponent)
            MyBase.Initialize(component)
        End Sub

        ''' <summary>
        ''' Commits any current changes in the editor to the backing docdata 
        ''' the docdata is then persisted separately
        ''' </summary>
        ''' <remarks>
        '''This should be done before attempting to persist.
        ''' </remarks>
        Public Sub CommitAnyPendingChanges()
            GetView().CommitAnyPendingChanges()
        End Sub

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
                _view = New ApplicationDesignerView(Me)
                'Delaying out designer load prevents our IRootDesigner.GetView from getting called
                'before we have initialized the m_View local
                'm_View.InitView()
            End If

            Return _view
        End Function

        ''' <summary>
        ''' Wrapper function to expose our UI object
        ''' </summary>
        Public Function GetView() As ApplicationDesignerView
            Return CType(RootDesigner_GetView(ViewTechnology.Default), ApplicationDesignerView)
        End Function

        ''' <summary>
        '''  Exposes GetService from ComponentDesigner to other classes in this assembly to get a service.
        ''' </summary>
        ''' <param name="ServiceType">The type of the service being asked for.</param>
        ''' <returns>The requested service, if it exists.</returns>
        Public Shadows Function GetService(ServiceType As Type) As Object
            Return MyBase.GetService(ServiceType)
        End Function

#If 0 Then
        Private Sub NextTabHandler(sender As Object, ev As EventArgs)
            'Dim AppDesignerView As ApplicationDesignerView
            'AppDesignerView = TryCast(view.Controls(0), ApplicationDesignerView)
            'If AppDesignerView IsNot Nothing Then
            '    AppDesignerView.SwitchTab(True)
            'End If
        End Sub

        Private Sub PrevTabHandler(sender As Object, ev As EventArgs)
            'Dim AppDesignerView As ApplicationDesignerView
            'AppDesignerView = TryCast(view.Controls(0), ApplicationDesignerView)
            'If AppDesignerView IsNot Nothing Then
            '    AppDesignerView.SwitchTab(False)
            'End If
        End Sub

        Private Shared ReadOnly CMDSETID_StandardCommandSet97 As New Guid("5efc7975-14bc-11cf-9b2b-00aa00573819")
        Private Shared ReadOnly guidVSStd97 As Guid = CMDSETID_StandardCommandSet97
        Private Const cmdidNextDocument As Integer = 628
        Private Const cmdidPrevDocument As Integer = 629
        Public Shared ReadOnly CommandIDVSStd97cmdidNextDocument As New CommandID(guidVSStd97, cmdidNextDocument)
        Public Shared ReadOnly CommandIDVSStd97cmdidPrevDocument As New CommandID(guidVSStd97, cmdidPrevDocument)

        Private Sub AddTabSwitchCommands()
            Dim NextTabVerb, PrevTabVerb As DesignerVerb

            Dim MCS As IMenuCommandService = CType(Me.GetService(GetType(IMenuCommandService)), IMenuCommandService)

            With MCS.Verbs
                .Add(New DesignerVerb("cmdidNextTab", New EventHandler(AddressOf Me.NextTabHandler), CommandIDVSStd97cmdidNextDocument))
                .Add(New DesignerVerb("cmdidPrevTab", New EventHandler(AddressOf Me.PrevTabHandler), CommandIDVSStd97cmdidPrevDocument))
            End With
        End Sub
#End If

    End Class

End Namespace
