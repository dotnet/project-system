' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Windows

Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.OptionPages
    ' This code is based on corresponding functionality in dotnet\roslyn: https:'github.com/dotnet/roslyn/blob/master/src/VisualStudio/Core/Impl/Options/AbstractOptionPage.cs

    <DesignerCategory("code")>
    Friend MustInherit Class OptionPage
        Inherits UIElementDialogPage

        Private _pageControl As OptionPageControl
        Private _needsLoadOnNextActivate As Boolean = True

        Protected Overrides ReadOnly Property Child As UIElement
            Get
                EnsureOptionPageCreated()
                Return _pageControl
            End Get
        End Property

        Public Overrides Sub LoadSettingsFromStorage()
            ' This gets called in two situations:
            '
            ' 1) during the initial page load when you first activate the page, before OnActivate
            '    Is called.
            ' 2) during the closing of the dialog via Cancel/close when options don't need to be
            '    saved. The intent here Is the settings get reloaded so the next time you open the
            '    page they are properly populated.
            '
            ' This second one Is tricky, because we don't actually want to update our controls
            ' right then, because they'd be wrong the next time the page opens -- it's possible
            ' they may have been changed programmatically. Therefore, we'll set a flag so we load
            ' next time
            _needsLoadOnNextActivate = True
        End Sub

        Public Overrides Sub SaveSettingsToStorage()
            EnsureOptionPageCreated()
            _pageControl.SaveSettings()

            ' Make sure we load the next time the page Is activated, in case if options changed
            ' programmatically between now And the next time the page Is activated
            _needsLoadOnNextActivate = True
        End Sub

        Protected MustOverride Function CreateOptionPage(serviceProvider As IServiceProvider) As OptionPageControl

        Protected Overrides Sub OnActivate(e As CancelEventArgs)
            If (_needsLoadOnNextActivate) Then
                EnsureOptionPageCreated()
                _pageControl.LoadSettings()

                _needsLoadOnNextActivate = False
            End If
        End Sub

        Protected Overrides Sub OnClosed(e As EventArgs)
            MyBase.OnClosed(e)

            If (_pageControl IsNot Nothing) Then
                _pageControl.Close()
            End If
        End Sub

        Private Sub EnsureOptionPageCreated()
            If (_pageControl Is Nothing) Then
                _pageControl = CreateOptionPage(Site)
            End If
        End Sub
    End Class
End Namespace
