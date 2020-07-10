' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Partial Class UnusedReferencePropPage

        Friend WithEvents ColHdr_Type As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Path As System.Windows.Forms.ColumnHeader
        Friend WithEvents UnusedReferenceList As System.Windows.Forms.ListView
        Friend WithEvents ColHdr_RefName As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Version As System.Windows.Forms.ColumnHeader
        Friend WithEvents UnusedReferencesListLabel As System.Windows.Forms.Label
        Friend WithEvents ColHdr_CopyLocal As System.Windows.Forms.ColumnHeader
        Private _components As System.ComponentModel.IContainer

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(UnusedReferencePropPage))
            Me.ColHdr_Type = New System.Windows.Forms.ColumnHeader("")
            Me.ColHdr_Path = New System.Windows.Forms.ColumnHeader("")
            Me.UnusedReferenceList = New System.Windows.Forms.ListView
            Me.ColHdr_RefName = New System.Windows.Forms.ColumnHeader(resources.GetString("UnusedReferenceList.Columns"))
            Me.ColHdr_Version = New System.Windows.Forms.ColumnHeader(resources.GetString("UnusedReferenceList.Columns1"))
            Me.ColHdr_CopyLocal = New System.Windows.Forms.ColumnHeader(resources.GetString("UnusedReferenceList.Columns2"))
            Me.UnusedReferencesListLabel = New System.Windows.Forms.Label
            Me.SuspendLayout()
            '
            'ColHdr_Type
            '
            resources.ApplyResources(Me.ColHdr_Type, "ColHdr_Type")
            '
            'ColHdr_Path
            '
            resources.ApplyResources(Me.ColHdr_Path, "ColHdr_Path")
            '
            'UnusedReferenceList
            '
            resources.ApplyResources(Me.UnusedReferenceList, "UnusedReferenceList")
            Me.UnusedReferenceList.AutoArrange = False
            Me.UnusedReferenceList.BackColor = System.Drawing.SystemColors.Window
            Me.UnusedReferenceList.CheckBoxes = True
            Me.UnusedReferenceList.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColHdr_RefName, Me.ColHdr_Type, Me.ColHdr_Version, Me.ColHdr_CopyLocal, Me.ColHdr_Path})
            Me.UnusedReferenceList.FullRowSelect = True
            Me.UnusedReferenceList.Margin = New System.Windows.Forms.Padding(0, 3, 0, 0)
            Me.UnusedReferenceList.MultiSelect = False
            Me.UnusedReferenceList.Name = "UnusedReferenceList"
            Me.UnusedReferenceList.View = System.Windows.Forms.View.LargeIcon
            '
            'ColHdr_RefName
            '
            resources.ApplyResources(Me.ColHdr_RefName, "ColHdr_RefName")
            '
            'ColHdr_Version
            '
            resources.ApplyResources(Me.ColHdr_Version, "ColHdr_Version")
            '
            'ColHdr_CopyLocal
            '
            resources.ApplyResources(Me.ColHdr_CopyLocal, "ColHdr_CopyLocal")
            '
            'UnusedReferencesListLabel
            '
            resources.ApplyResources(Me.UnusedReferencesListLabel, "UnusedReferencesListLabel")
            Me.UnusedReferencesListLabel.Margin = New System.Windows.Forms.Padding(0)
            Me.UnusedReferencesListLabel.Name = "UnusedReferencesListLabel"
            '
            'UnusedReferencePropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.UnusedReferencesListLabel)
            Me.Controls.Add(Me.UnusedReferenceList)
            Me.Name = "UnusedReferencePropPage"
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

    End Class

End Namespace

