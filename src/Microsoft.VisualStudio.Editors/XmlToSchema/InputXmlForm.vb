' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Infer On
Imports System.IO
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports System.Xml
Imports System.Xml.Schema
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.Editors.XmlToSchema

    Friend NotInheritable Class InputXmlForm
        Private ReadOnly _project As EnvDTE.Project
        Private ReadOnly _projectPath As String
        Private ReadOnly _schemaFileName As String

        Public Sub New(project As EnvDTE.Project, projectPath As String, schemaFileName As String)
            MyBase.New(Nothing)

            InitializeComponent()
            _project = project
            _projectPath = projectPath
            _schemaFileName = Path.GetFileNameWithoutExtension(schemaFileName)
            If String.IsNullOrEmpty(_schemaFileName) Then
                _schemaFileName = "XmlToSchema"
            End If
            _picutreBox.Image = _imageList2.Images(0)

            Common.TelemetryLogger.LogInputXmlFormEvent(Common.TelemetryLogger.InputXmlFormEvent.FormOpened)
        End Sub

        Protected Overrides Sub ScaleControl(factor As Drawing.SizeF, specified As BoundsSpecified)
            'First do standard DPI scaling logic
            MyBase.ScaleControl(factor, specified)

            'Prevent the dialog from getting too big
            MaximumSize = Screen.FromHandle(Handle).WorkingArea.Size
        End Sub

        Private Function ContainsFile(filePath As String) As Boolean
            Dim fileUri As New Uri(filePath, UriKind.RelativeOrAbsolute)
            For Each item As ListViewItem In _listView.Items
                If Uri.IsWellFormedUriString(item.Text, UriKind.RelativeOrAbsolute) Then
                    Dim itemUri As New Uri(item.SubItems(1).Text, UriKind.RelativeOrAbsolute)
                    If Uri.Compare(fileUri, itemUri, UriComponents.AbsoluteUri, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) = 0 Then
                        Return True
                    End If
                End If
            Next
            Return False
        End Function

        Private Sub AddFile(filePath As String)
            If Not String.IsNullOrEmpty(filePath) AndAlso Not ContainsFile(filePath) Then
                Dim item As New ListViewItem("File") With {.Tag = filePath}
                item.SubItems.Add(filePath)
                _listView.Items.Add(item)
            End If
        End Sub

        Private Sub _addFromFileButton_Click(sender As Object, e As EventArgs) Handles _addFromFileButton.Click
            Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                _xmlFileDialog.InitialDirectory = _projectPath
                If _xmlFileDialog.ShowDialog() = DialogResult.OK Then
                    Dim anyInvalid = False
                    Try
                        For Each fileName In _xmlFileDialog.FileNames
                            UseWaitCursor = True
                            XElement.Load(fileName)
                        Next
                    Catch ex As Exception
                        If FilterException(ex) Then
                            ShowWarning(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.XmlToSchema_ErrorLoadingXml, ex.Message))
                            anyInvalid = True
                        Else
                            Throw
                        End If
                    Finally
                        UseWaitCursor = False
                    End Try

                    If Not anyInvalid Then
                        For Each fileName In _xmlFileDialog.FileNames
                            AddFile(fileName)
                        Next
                    End If
                End If

                Common.TelemetryLogger.LogInputXmlFormEvent(Common.TelemetryLogger.InputXmlFormEvent.FromFileButtonClicked)
            End Using
        End Sub

        Private Sub _addFromWebButton_Click(sender As Object, e As EventArgs) Handles _addFromWebButton.Click
            Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                Using dialog As New WebUrlDialog()
                    dialog.ServiceProvider = ServiceProvider
                    Dim uiService As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                    If uiService.ShowDialog(dialog) = DialogResult.OK Then
                        If Not ContainsFile(dialog.Url) Then
                            Dim item As New ListViewItem("URL") With {.Tag = dialog.Xml}
                            item.SubItems.Add(dialog.Url)
                            _listView.Items.Add(item)
                        End If
                    End If

                    Common.TelemetryLogger.LogInputXmlFormEvent(Common.TelemetryLogger.InputXmlFormEvent.FromWebButtonClicked)
                End Using
            End Using
        End Sub

        Private Sub _addAsTextButton_Click(sender As Object, e As EventArgs) Handles _addAsTextButton.Click
            Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                Using dialog As New PasteXmlDialog()
                    dialog.ServiceProvider = ServiceProvider
                    Dim uiService As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                    If uiService.ShowDialog(dialog) = DialogResult.OK Then
                        Dim item As New ListViewItem("XML") With {.Tag = dialog.Xml}
                        Dim xmlText = dialog.Xml.ToString(SaveOptions.DisableFormatting)
                        If xmlText.Length > 128 Then
                            xmlText = xmlText.Substring(0, 128)
                        End If
                        item.SubItems.Add(xmlText)
                        _listView.Items.Add(item)
                    End If

                    Common.TelemetryLogger.LogInputXmlFormEvent(Common.TelemetryLogger.InputXmlFormEvent.AsTextButtonClicked)
                End Using
            End Using
        End Sub

        Private Sub _okButtonClick(sender As Object, e As EventArgs) Handles _okButton.Click
            If _listView.Items.Count = 0 Then
                Return
            End If
            Try
                UseWaitCursor = True
                Application.DoEvents()

                ' Infer schemas from XML sources.
                Dim schemaSet As New XmlSchemaSet
                Dim infer As New XmlSchemaInference
                For Each item As ListViewItem In _listView.Items
                    Dim element = TryCast(item.Tag, XElement)
                    Using reader = If(element Is Nothing, GetXmlTextReaderWithDtdProcessingProhibited(CStr(item.Tag)), element.CreateReader)

                        infer.InferSchema(reader, schemaSet)
                    End Using
                Next

                ' Add inferred schemas to the project.
                Dim settings As New XmlWriterSettings() With {.Indent = True}
                Dim index As Integer = 0
                For Each schema As XmlSchema In schemaSet.Schemas()
                    ' Find unused file name to save the schema.
                    Dim schemaFilePath As String
                    Do
                        schemaFilePath = Path.Combine(_projectPath, _schemaFileName & If(index > 0, CStr(index), "") & ".xsd")
                        index += 1
                    Loop While File.Exists(schemaFilePath)

                    ' Write inferred schema to the file.
                    Using writer = XmlWriter.Create(schemaFilePath, settings)
                        schema.Write(writer)
                    End Using

                    ' Add schema file to the project.
                    _project.ProjectItems.AddFromFile(schemaFilePath)
                Next
            Catch ex As Exception
                Common.TelemetryLogger.LogInputXmlFormException(ex)
                If FilterException(ex) Then
                    ShowWarning(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.XmlToSchema_ErrorInXmlInference, ex.Message))
                Else
                    Throw
                End If
            Finally
                UseWaitCursor = False
            End Try
        End Sub

        Private Shared Function GetXmlTextReaderWithDtdProcessingProhibited(element As String) As XmlTextReader
            ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
            Return New XmlTextReader(element) With {
                .DtdProcessing = DtdProcessing.Prohibit
            }
        End Function

        Private Sub _listViewKeyPress(o As Object, e As KeyEventArgs) Handles _listView.KeyDown
            If e.KeyCode = Keys.Delete Then
                Dim toDelete = New List(Of ListViewItem)
                For Each cur As ListViewItem In _listView.SelectedItems
                    toDelete.Add(cur)
                Next

                For Each cur In toDelete
                    _listView.Items.Remove(cur)
                Next
            End If
        End Sub

    End Class

End Namespace
