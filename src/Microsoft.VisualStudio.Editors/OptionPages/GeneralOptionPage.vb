' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.OptionPages
    ''' <summary>
    ''' Implements the Tools | Options | Projects and Solutions | SDK-Style Projects page.
    ''' </summary>
    ''' <remarks>
    ''' The UI itself is implemented by <see cref="GeneralOptionPageControl"/>.
    ''' The options are handled by <see cref="SDKStyleProjectOptionsData"/>.
    ''' This type is responsible for exposing the UI through the <see cref="Child"/>
    ''' property and the settings through the <see cref="AutomationObject"/>
    ''' property.
    ''' Note that at any given time there are up to two copies of <see cref="SDKStyleProjectOptionsData"/>
    ''' in play: the "main" instance holding the values used by other parts of VS,
    ''' and a copy backing the UI. When the user changes a value in the UI only the
    ''' UI copy is updated immediately; the main copy is only updated when the user
    ''' saves their changes. This type is responsible for coordinating the two
    ''' copies.
    ''' </remarks>
    <Guid("2E6DB64B-DA09-4B9F-A334-37A86FECDA6A")>
    <ComVisible(True)>
    Friend NotInheritable Class GeneralOptionPage
        Inherits UIElementDialogPage

        ''' <summary>
        ''' Normally when the main instance of our options changes we need to update
        ''' the copy backing the UI. However, there are times we need to suspend those
        ''' updates, such as when we're copying values *from* the UI copy *to* the main
        ''' instance.
        ''' </summary>
        Private ReadOnly _optionsControlUpdateSuspender As Suspender

        Private _shouldUpdateOptionsControlOnPropertyChange As Boolean = True
        Private _optionsControl As GeneralOptionPageControl

        Public Sub New()
            _optionsControlUpdateSuspender = New Suspender(Sub() _shouldUpdateOptionsControlOnPropertyChange = True)

            AddHandler SDKStyleProjectOptionsData.MainInstance.PropertyChanged, AddressOf OptionsDataPropertyChangedHandler
        End Sub

        ''' <summary>
        ''' Exposes the main instance of the options on this page as the AutomationObject
        ''' for the page. By doing so we can make use of default behavior for loading
        ''' and saving the settings and automatically expose them through the DTE
        ''' interfaces.
        ''' </summary>
        Public Overrides ReadOnly Property AutomationObject As Object
            Get
                Return SDKStyleProjectOptionsData.MainInstance
            End Get
        End Property

        ''' <summary>
        ''' Creates and returns the WPF UIElement for the page.
        ''' The UIElement is backed by a copy of the main options; when/if the user
        ''' saves their changes we'll copy the options back to the main instance.
        ''' </summary>
        Protected Overrides ReadOnly Property Child As System.Windows.UIElement
            Get
                If _optionsControl Is Nothing Then
                    ' Get a snapshot of the current settings for the page to modify. When the user
                    ' clicks "OK" OnApply() will be called and we'll copy the control's data back to
                    ' the current settings.
                    Dim settings = SDKStyleProjectOptionsData.MainInstance.Clone()

                    _optionsControl = New GeneralOptionPageControl() With {
                        .DataContext = settings
                    }
                End If

                Return _optionsControl
            End Get
        End Property

        ''' <summary>
        ''' Called when the user clicks "OK" in Tools | Options. Copies the options
        ''' from the control back to the main instance.
        ''' </summary>
        Protected Overrides Sub OnApply(e As PageApplyEventArgs)
            ' Normally we copy changes from the main instance of the options to the
            ' control's instance. Here we're copying them in the other direction, so we
            ' need to suspend the normal upates.
            Using SuspendOptionsControlUpdates()
                SDKStyleProjectOptionsData.MainInstance.CopyFrom(DataContextOptions)
            End Using

            MyBase.OnApply(e)
        End Sub

        ''' <summary>
        ''' Called when Tools | Options closes. Resets the control's options to match
        ''' those in the main instance. If the user clicked "OK" this will be called
        ''' immediately after <see cref="OnApply(PageApplyEventArgs)"/>, in which case
        ''' the options will already be in sync and this will change nothing. If the
        ''' user clicked "Cancel" this will undo any changes they made and leave the
        ''' page with the correct content the next time it is shown.
        ''' </summary>
        Protected Overrides Sub OnClosed(e As EventArgs)
            MyBase.OnClosed(e)

            DataContextOptions.CopyFrom(SDKStyleProjectOptionsData.MainInstance)
        End Sub

        ''' <summary>
        ''' Updates the UI copy of the options when the main instances changes.
        ''' </summary>
        Private Sub OptionsDataPropertyChangedHandler(sender As Object, e As PropertyChangedEventArgs)
            ' The main copy of the options has changed. Update the copy used by the
            ' Tools | Options UI, if has been created.
            If _shouldUpdateOptionsControlOnPropertyChange AndAlso _optionsControl IsNot Nothing Then
                ThreadHelper.JoinableTaskFactory.RunAsync(
                    Async Function() As Task(Of TaskListItem)
                        Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync()
                        DataContextOptions.CopyFrom(SDKStyleProjectOptionsData.MainInstance)
                    End Function)
            End If
        End Sub

        Private ReadOnly Property DataContextOptions As SDKStyleProjectOptionsData
            Get
                Return DirectCast(_optionsControl.DataContext, SDKStyleProjectOptionsData)
            End Get
        End Property

        Private Function SuspendOptionsControlUpdates() As IDisposable
            _shouldUpdateOptionsControlOnPropertyChange = False
            Return _optionsControlUpdateSuspender.Suspend()
        End Function
    End Class
End Namespace
