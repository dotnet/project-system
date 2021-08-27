' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Infer On
Imports System.IO
Imports System.Windows.Forms.Design

Imports EnvDTE

Imports Microsoft.VisualStudio.TemplateWizard
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.Editors.XmlToSchema
    Public NotInheritable Class Wizard
        Implements IWizard

        Public Sub BeforeOpeningFile(projectItem As ProjectItem) Implements IWizard.BeforeOpeningFile
        End Sub

        Public Sub ProjectFinishedGenerating(project As Project) Implements IWizard.ProjectFinishedGenerating
        End Sub

        Public Sub ProjectItemFinishedGenerating(projectItem As ProjectItem) Implements IWizard.ProjectItemFinishedGenerating
        End Sub

        Public Sub RunFinished() Implements IWizard.RunFinished
        End Sub

        Public Sub RunStarted(automationObject As Object,
                              replacementsDictionary As Dictionary(Of String, String),
                              runKind As WizardRunKind,
                              customParams() As Object) Implements IWizard.RunStarted
            If automationObject Is Nothing OrElse replacementsDictionary Is Nothing Then
                Return
            End If
            Try
                Dim dte = CType(automationObject, DTE)

                Dim activeProjects As Array = TryCast(dte.ActiveSolutionProjects, Array)
                If activeProjects Is Nothing OrElse activeProjects.Length = 0 Then
                    ShowWarning(My.Resources.Microsoft_VisualStudio_Editors_Designer.XmlToSchema_NoProjectSelected)
                    Return
                End If

                Dim acitveProject = TryCast(activeProjects.GetValue(0), Project)
                If acitveProject Is Nothing Then
                    ShowWarning(My.Resources.Microsoft_VisualStudio_Editors_Designer.XmlToSchema_NoProjectSelected)
                    Return
                End If

                Dim savePath = Path.GetDirectoryName(acitveProject.FullName)
                If Not Directory.Exists(savePath) Then
                    'For Website projects targeting IIS/IIS Express activeProject.FullName returns http path which is not a Valid Directory.
                    'Instead we will use activeProject.Properties.Item(FullPath).Value to give a chance for Website projects(IIS/IIS Express)
                    'to see if valid directory exists before showing warning. We will keep this logic inside try/catch block
                    'since for projects which dont support "FullPath" property exception can be thrown.
                    Try
                        savePath = acitveProject.Properties.Item("FullPath").Value.ToString()
                    Catch 'Eat any exception
                    End Try
                    If Not Directory.Exists(savePath) Then
                        ShowWarning(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.XmlToSchema_InvalidProjectPath, savePath))
                        Return
                    End If
                End If

                Dim fileName = replacementsDictionary("$rootname$")
                If String.IsNullOrEmpty(fileName) Then
                    ShowWarning(My.Resources.Microsoft_VisualStudio_Editors_Designer.XmlToSchema_InvalidEmptyItemName)
                    Return
                End If

                Dim inputForm As New InputXmlForm(acitveProject, savePath, fileName) With {
                    .ServiceProvider = Common.ShellUtil.GetServiceProvider(dte)
                }
                Dim uiService As IUIService = CType(inputForm.ServiceProvider.GetService(GetType(IUIService)), IUIService)
                Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                    uiService.ShowDialog(inputForm)
                End Using

            Catch ex As Exception
                If FilterException(ex) Then
                    ShowWarning(ex)
                Else
                    Throw
                End If
            End Try
        End Sub

        Public Function ShouldAddProjectItem(filePath As String) As Boolean Implements IWizard.ShouldAddProjectItem
            Return False
        End Function
    End Class
End Namespace
