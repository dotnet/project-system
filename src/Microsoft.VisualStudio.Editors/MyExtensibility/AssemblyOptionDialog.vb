' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.ComponentModel
Imports System.Reflection

Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil

Imports Res = My.Resources.MyExtensibilityRes

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;AssemblyOptionDialog
    ''' <summary>
    ''' Asking the user to add/remove the extension templates / code files
    ''' and remember the option for the trigger assembly.
    ''' </summary>
    Friend Class AssemblyOptionDialog
#If False Then ' To edit in WinForms Designer: Change False -> True and checkboxOption to System.System.Windows.Forms.Checkbox
        Inherits System.System.Windows.Forms.Form

        Public Sub New()
            MyBase.New()
            Me.InitializeComponent()
        End Sub
#Else
        Inherits DesignerFramework.BaseDialog

        ''' ;GetAssemblyOptionDialog
        ''' <summary>
        ''' Shared method to return an instance of Add / Remove extension templates / code files dialog.
        ''' </summary>
        Public Shared Function GetAssemblyOptionDialog(
                assemblyName As String,
                serviceProvider As IServiceProvider,
                objects As IList,
                extensionAction As AddRemoveAction) As AssemblyOptionDialog

            Debug.Assert(Not String.IsNullOrEmpty(assemblyName), "NULL or empty: assemblyName!")
            Debug.Assert(serviceProvider IsNot Nothing, "NULL serviceProvider!")
            Debug.Assert(objects IsNot Nothing AndAlso objects.Count > 0, "Nothing to display!")
            Debug.Assert(extensionAction = AddRemoveAction.Add OrElse extensionAction = AddRemoveAction.Remove, "Invalid ExtensionAction!")

            assemblyName = GetAssemblyName(assemblyName)

            Dim dialog As New AssemblyOptionDialog(serviceProvider, objects)
            If extensionAction = AddRemoveAction.Add Then
                dialog.Text = Res.AssemblyOptionDialog_Add_Text
                dialog.labelQuestion.Text = String.Format(Res.AssemblyOptionDialog_Add_Question, assemblyName)
            Else
                dialog.Text = Res.AssemblyOptionDialog_Remove_Text
                dialog.labelQuestion.Text = String.Format(Res.AssemblyOptionDialog_Remove_Question, assemblyName)
            End If

            dialog.checkBoxOption.Text = String.Format(Res.AssemblyOptionDialog_Option, assemblyName)
            Return dialog
        End Function

        ''' ;GetAssemblyName
        ''' <summary>
        ''' Return the assembly name from the assembly full name.
        ''' </summary>
        Private Shared Function GetAssemblyName(assemblyFullName As String) As String
            If StringIsNullEmptyOrBlank(assemblyFullName) Then
                Return String.Empty
            End If
            Return New AssemblyName(assemblyFullName).Name
        End Function

        Private Sub New()
        End Sub

        Private Sub New(serviceProvider As IServiceProvider,
                objects As IList)
            MyBase.New(serviceProvider)
            InitializeComponent()

            F1Keyword = HelpIDs.Dlg_AddMyNamespaceExtensions

            Debug.Assert(objects IsNot Nothing, "Nothing to display!")
            For Each listObject As Object In objects
                Dim namedObject As INamedDescribedObject = TryCast(listObject, INamedDescribedObject)
                Debug.Assert(namedObject IsNot Nothing, "Invalid object in list!")
                If namedObject IsNot Nothing Then
                    listBoxItems.Items.Add(namedObject.DisplayName)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Click handler for the Help button. DevDiv Bugs 110807.
        ''' </summary>
        Private Sub AssemblyOptionDialog_HelpButtonClicked(
                sender As Object, e As CancelEventArgs) _
                Handles MyBase.HelpButtonClicked
            e.Cancel = True
            ShowHelp()
        End Sub
#End If

        Public ReadOnly Property OptionChecked As Boolean
            Get
                Return checkBoxOption.Checked
            End Get
        End Property

        Private Sub buttonYes_Click(sender As Object, e As EventArgs) Handles buttonYes.Click
            Close()
            DialogResult = System.Windows.Forms.DialogResult.Yes
        End Sub

#Region "Windows Form Designer generated code"
        Friend WithEvents labelQuestion As System.Windows.Forms.Label
        Friend WithEvents tableLayoutOverarching As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents listBoxItems As System.Windows.Forms.ListBox
        Friend WithEvents tableLayoutYesNoButtons As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents buttonYes As System.Windows.Forms.Button
        Friend WithEvents checkBoxOption As DesignerFramework.WrapCheckBox
        Friend WithEvents buttonNo As System.Windows.Forms.Button

        Private Sub InitializeComponent()
            Dim resources As ComponentResourceManager = New ComponentResourceManager(GetType(AssemblyOptionDialog))
            tableLayoutOverarching = New System.Windows.Forms.TableLayoutPanel
            labelQuestion = New System.Windows.Forms.Label
            listBoxItems = New System.Windows.Forms.ListBox
            tableLayoutYesNoButtons = New System.Windows.Forms.TableLayoutPanel
            buttonYes = New System.Windows.Forms.Button
            buttonNo = New System.Windows.Forms.Button
            checkBoxOption = New DesignerFramework.WrapCheckBox
            tableLayoutOverarching.SuspendLayout()
            tableLayoutYesNoButtons.SuspendLayout()
            SuspendLayout()
            '
            'tableLayoutOverarching
            '
            resources.ApplyResources(tableLayoutOverarching, "tableLayoutOverarching")
            tableLayoutOverarching.Controls.Add(labelQuestion, 0, 0)
            tableLayoutOverarching.Controls.Add(checkBoxOption, 0, 2)
            tableLayoutOverarching.Controls.Add(tableLayoutYesNoButtons, 0, 3)
            tableLayoutOverarching.Controls.Add(listBoxItems, 0, 1)
            tableLayoutOverarching.Name = "tableLayoutOverarching"
            '
            'labelQuestion
            '
            resources.ApplyResources(labelQuestion, "labelQuestion")
            labelQuestion.Name = "labelQuestion"
            '
            'listBoxItems
            '
            resources.ApplyResources(listBoxItems, "listBoxItems")
            listBoxItems.FormattingEnabled = True
            listBoxItems.Name = "listBoxItems"
            '
            'tableLayoutYesNoButtons
            '
            resources.ApplyResources(tableLayoutYesNoButtons, "tableLayoutYesNoButtons")
            tableLayoutYesNoButtons.Controls.Add(buttonYes, 0, 0)
            tableLayoutYesNoButtons.Controls.Add(buttonNo, 1, 0)
            tableLayoutYesNoButtons.Name = "tableLayoutYesNoButtons"
            '
            'buttonYes
            '
            resources.ApplyResources(buttonYes, "buttonYes")
            buttonYes.Name = "buttonYes"
            buttonYes.UseVisualStyleBackColor = True
            '
            'buttonNo
            '
            buttonNo.DialogResult = System.Windows.Forms.DialogResult.Cancel
            resources.ApplyResources(buttonNo, "buttonNo")
            buttonNo.Name = "buttonNo"
            buttonNo.UseVisualStyleBackColor = True
            '
            'checkBoxOption
            '
            resources.ApplyResources(checkBoxOption, "checkBoxOption")
            checkBoxOption.Name = "checkBoxOption"
            checkBoxOption.UseVisualStyleBackColor = True
            '
            'AssemblyOptionDialog
            '
            AcceptButton = buttonYes
            CancelButton = buttonNo
            resources.ApplyResources(Me, "$this")
            Controls.Add(tableLayoutOverarching)
            HelpButton = True
            MaximizeBox = False
            MinimizeBox = False
            Name = "AssemblyOptionDialog"
            ShowIcon = False
            ShowInTaskbar = False
            tableLayoutOverarching.ResumeLayout(False)
            tableLayoutOverarching.PerformLayout()
            tableLayoutYesNoButtons.ResumeLayout(False)
            ResumeLayout(False)

        End Sub
#End Region

    End Class
End Namespace
