' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

'*************************
'
'  These values must match those in VisualStudioEditorsID.h
'
'*************************

'Note: Common shell ID's taken from public\internal\VSCommon\inc\vsshlids.h and stdidcmd.h

Namespace Microsoft.VisualStudio.Editors

    Partial Public Class Constants
        Friend NotInheritable Class MenuConstants

            Private Sub New()
            End Sub

            ' Constants for menu command IDs and GUIDs. 
            ' *** These must match the constants in designerui\VisualStudioEditorsID.h *****

            Friend Shared ReadOnly GuidVSStd97 As New Guid("5efc7975-14bc-11cf-9b2b-00aa00573819")
            Friend Shared ReadOnly GuidVSStd2K As New Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2")
            Private Const CmdIdCut As Integer = 16
            Friend Const CmdIdFileClose As Integer = 223
            Friend Const CmdIdSave As Integer = 110
            Friend Const CmdIdSaveAs As Integer = 111
            Friend Const CmdIdSaveProjectItemAs As Integer = 226
            Friend Const CmdIdSaveProjectItem As Integer = 331

            Friend Shared ReadOnly CommandIDVSStd97cmdidCut As New CommandID(GuidVSStd97, CmdIdCut)

        End Class
    End Class

End Namespace

