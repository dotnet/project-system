' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

'*************************
'
'  These values must match those in VisualStudioEditorsID.h
'
'*************************

'Note: Common shell ID's taken from public\internal\VSCommon\inc\vsshlids.h and stdidcmd.h

Namespace Microsoft.VisualStudio.Editors

    Partial Friend NotInheritable Class Constants
        Friend NotInheritable Class MenuConstants

            ' Constants for menu command IDs and GUIDs. 
            ' *** These must match the constants in designerui\VisualStudioEditorsID.h *****

            ' *********************************************************************
            ' Menu IDs (0x01??)
            ' *********************************************************************

            Private Const IDM_CTX_RESX_ContextMenu As Integer = &H100
            Private Const IDM_CTX_SETTINGSDESIGNER_ContextMenu As Integer = &H110
            Public Const IDM_VS_TOOLBAR_Settings As Integer = &H210
            Public Const IDM_VS_TOOLBAR_Resources As Integer = &H211
            Public Const IDM_VS_TOOLBAR_Resources_ResW As Integer = &H212

            ' *********************************************************************
            ' Command Group IDs (0x1???)
            ' *********************************************************************

            ' *********************************************************************
            ' Command IDs (0x2???)
            ' *********************************************************************

            Private Const CmdIdRESXImport As Integer = &H2003
            Private Const CmdIdRESXExport As Integer = &H2004
            Private Const CmdIdRESXPlay As Integer = &H2005

            Private Const CmdIdRESXAddFixedMenuCommand As Integer = &H2019
            Private Const CmdIdRESXAddExistingFile As Integer = &H2020
            Private Const CmdIdRESXAddNewString As Integer = &H2021
            Private Const CmdIdRESXAddNewImagePNG As Integer = &H2022
            Private Const CmdIdRESXAddNewImageBMP As Integer = &H2023
            Private Const CmdIdRESXAddNewImageGIF As Integer = &H2024
            Private Const CmdIdRESXAddNewImageJPEG As Integer = &H2025
            Private Const CmdIdRESXAddNewImageTIFF As Integer = &H2026
            Private Const CmdIdRESXAddNewIcon As Integer = &H2027
            Private Const CmdIdRESXAddNewTextFile As Integer = &H2028
            Private Const CmdIdRESXAddDefaultResource As Integer = &H2030

            Private Const CmdIdRESXResTypeStrings As Integer = &H2040
            Private Const CmdIdRESXResTypeImages As Integer = &H2041
            Private Const CmdIdRESXResTypeIcons As Integer = &H2042
            Private Const CmdIdRESXResTypeAudio As Integer = &H2043
            Private Const CmdIdRESXResTypeFiles As Integer = &H2044
            Private Const CmdIdRESXResTypeOther As Integer = &H2045

            Private Const CmdIdIDRESXViewsFixedMenuCommand As Integer = &H2018
            Private Const CmdIdRESXViewsList As Integer = &H2050
            Private Const CmdIdRESXViewsDetails As Integer = &H2051
            Private Const CmdIdRESXViewsThumbnails As Integer = &H2052

            Private Const CmdIdRESXGenericRemove As Integer = &H2060

            Private Const CmdIdRESXAccessModifierCombobox As Integer = &H2061
            Private Const CmdIdRESXGetAccessModifierOptions As Integer = &H2062

            Private Const CmdIdSETTINGSDESIGNERViewCode As Integer = &H2104
            Private Const CmdIdSETTINGSDESIGNERSynchronize As Integer = &H2105
            Private Const CmdIdSETTINGSDESIGNERAccessModifierCombobox As Integer = &H2106
            Private Const CmdIdSETTINGSDESIGNERGetAccessModifierOptions As Integer = &H2107
            Private Const CmdIdSETTINGSDESIGNERLoadWebSettings As Integer = &H2108

            Private Const CmdIdCOMMONEditCell As Integer = &H2F00
            Private Const CmdIdCOMMONAddRow As Integer = &H2F01
            Private Const CmdIdCOMMONRemoveRow As Integer = &H2F02

            ' *********************************************************************

            'Some common stuff
            Public Shared ReadOnly GuidVSStd97 As New Guid("5efc7975-14bc-11cf-9b2b-00aa00573819")
            Public Shared ReadOnly GuidVSStd2K As New Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2")
            Private Const CmdIdCopy As Integer = 15
            Public Const CmdIdCut As Integer = 16
            Private Const CmdIdDelete As Integer = 17
            Public Const CmdIdRedo As Integer = 29
            Public Const CmdIdMultiLevelRedo As Integer = 30
            Public Const CmdIdMultiLevelRedoList As Integer = 299
            Public Const CmdIdUndo As Integer = 43
            Public Const CmdIdMultiLevelUndo As Integer = 44
            Public Const CmdIdMultiLevelUndoList As Integer = 299
            Private Const CmdIdRemove As Integer = 168
            Private Const CmdIdPaste As Integer = 26
            Private Const CmdIdOpen As Integer = 261
            Private Const CmdIdOpenWith As Integer = 199
            Private Const CmdIdRename As Integer = 150
            Private Const CmdIdSelectAll As Integer = 31
            Public Const CmdIdFileClose As Integer = 223
            Public Const CmdIdSave As Integer = 110
            Public Const CmdIdSaveAs As Integer = 111
            Public Const CmdIdSaveProjectItemAs As Integer = 226
            Public Const CmdIdSaveProjectItem As Integer = 331
            Public Const CmdIdViewCode As Integer = 333
            Public Const CmdIdEditLabel As Integer = 338
            Public Const ECMD_CANCEL As Integer = 103

            Public Shared ReadOnly CommandIDVSStd97cmdidCut As New CommandID(GuidVSStd97, CmdIdCut)
            Public Shared ReadOnly CommandIDVSStd97cmdidCopy As New CommandID(GuidVSStd97, CmdIdCopy)
            Public Shared ReadOnly CommandIDVSStd97cmdidPaste As New CommandID(GuidVSStd97, CmdIdPaste)
            Public Shared ReadOnly CommandIDVSStd97cmdidDelete As New CommandID(GuidVSStd97, CmdIdDelete)
            Public Shared ReadOnly CommandIDVSStd97cmdidRemove As New CommandID(GuidVSStd97, CmdIdRemove)
            Public Shared ReadOnly CommandIDVSStd97cmdidRename As New CommandID(GuidVSStd97, CmdIdRename)
            Public Shared ReadOnly CommandIDVSStd97cmdidSelectAll As New CommandID(GuidVSStd97, CmdIdSelectAll)
            Public Shared ReadOnly CommandIDVSStd97cmdidEditLabel As New CommandID(GuidVSStd97, CmdIdEditLabel)
            Public Shared ReadOnly CommandIDVSStd97cmdidViewCode As New CommandID(GuidVSStd97, CmdIdViewCode)
            Public Shared ReadOnly CommandIDVSStd2kECMD_CANCEL As New CommandID(GuidVSStd2K, ECMD_CANCEL)

            ' GUID constants.
            Private Shared ReadOnly s_GUID_RESX_CommandID As New Guid("66BD4C1D-3401-4bcc-A942-E4990827E6F7")
            'The Command GUID for the resource editor.  It is required for us to correctly hook up key bindings,
            '  and must be returned from the editor factory.
            Public Shared ReadOnly GUID_RESXEditorCommandUI As New Guid("fea4dcc9-3645-44cd-92e7-84b55a16465c")
            Public Const GUID_RESXEditorCommandUIString As String = "fea4dcc9-3645-44cd-92e7-84b55a16465c"

            Public Shared ReadOnly GUID_RESX_MenuGroup As New Guid("54869924-25F5-4878-A9C9-1C7198D99A8A")
            Public Shared ReadOnly GUID_SETTINGSDESIGNER_MenuGroup As New Guid("42b7a61f-81fd-4283-9678-6c448a827e56")
            Private Shared ReadOnly s_GUID_SETTINGSDESIGNER_CommandID As New Guid("c2013470-51ac-4278-9ac5-389c72a1f926")
            'The Command GUID for the settings designer.  It is required for us to correctly hook up key bindings,
            '  and must be returned from the editor factory.
            Public Shared ReadOnly GUID_SETTINGSDESIGNER_CommandUI As New Guid("515231ad-c9dc-4aa3-808f-e1b65e72081c")
            Public Const GUID_SETTINGSDESIGNER_CommandUIString As String = "515231ad-c9dc-4aa3-808f-e1b65e72081c"

            Private Shared ReadOnly s_GUID_MS_VS_Editors_CommandId As New Guid("E4B9BB05-1963-4774-8CFC-518359E3FCE3")

            ' Command ID = GUID + cmdid.
            Public Shared ReadOnly CommandIDVSStd97Open As New CommandID(GuidVSStd97, CmdIdOpen)
            Public Shared ReadOnly CommandIDVSStd97OpenWith As New CommandID(GuidVSStd97, CmdIdOpenWith)
            Public Shared ReadOnly CommandIDResXImport As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXImport)
            Public Shared ReadOnly CommandIDResXExport As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXExport)
            Public Shared ReadOnly CommandIDResXPlay As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXPlay)

            Public Shared ReadOnly CommandIDRESXAddFixedMenuCommand As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddFixedMenuCommand)
            Public Shared ReadOnly CommandIDRESXAddExistingFile As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddExistingFile)
            Public Shared ReadOnly CommandIDRESXAddNewString As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewString)
            Public Shared ReadOnly CommandIDRESXAddNewImagePNG As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewImagePNG)
            Public Shared ReadOnly CommandIDRESXAddNewImageBMP As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewImageBMP)
            Public Shared ReadOnly CommandIDRESXAddNewImageGIF As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewImageGIF)
            Public Shared ReadOnly CommandIDRESXAddNewImageJPEG As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewImageJPEG)
            Public Shared ReadOnly CommandIDRESXAddNewImageTIFF As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewImageTIFF)
            Public Shared ReadOnly CommandIDRESXAddNewIcon As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewIcon)
            Public Shared ReadOnly CommandIDRESXAddNewTextFile As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddNewTextFile)
            Public Shared ReadOnly CommandIDRESXAddDefaultResource As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAddDefaultResource)

            Public Shared ReadOnly CommandIDRESXResTypeStrings As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXResTypeStrings)
            Public Shared ReadOnly CommandIDRESXResTypeImages As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXResTypeImages)
            Public Shared ReadOnly CommandIDRESXResTypeIcons As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXResTypeIcons)
            Public Shared ReadOnly CommandIDRESXResTypeAudio As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXResTypeAudio)
            Public Shared ReadOnly CommandIDRESXResTypeFiles As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXResTypeFiles)
            Public Shared ReadOnly CommandIDRESXResTypeOther As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXResTypeOther)
            Public Shared ReadOnly CommandIDRESXViewsFixedMenuCommand As New CommandID(s_GUID_RESX_CommandID, CmdIdIDRESXViewsFixedMenuCommand)
            Public Shared ReadOnly CommandIDRESXViewsList As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXViewsList)
            Public Shared ReadOnly CommandIDRESXViewsDetails As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXViewsDetails)
            Public Shared ReadOnly CommandIDRESXViewsThumbnails As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXViewsThumbnails)

            Public Shared ReadOnly CommandIDRESXGenericRemove As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXGenericRemove)
            Public Shared ReadOnly CommandIDRESXAccessModifierCombobox As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXAccessModifierCombobox)
            Public Shared ReadOnly CommandIDRESXGetAccessModifierOptions As New CommandID(s_GUID_RESX_CommandID, CmdIdRESXGetAccessModifierOptions)

            Public Shared ReadOnly ResXContextMenuID As New CommandID(GUID_RESX_MenuGroup, IDM_CTX_RESX_ContextMenu)
            Public Shared ReadOnly SettingsDesignerContextMenuID As New CommandID(GUID_SETTINGSDESIGNER_MenuGroup, IDM_CTX_SETTINGSDESIGNER_ContextMenu)
            Public Shared ReadOnly SettingsDesignerToolbar As New CommandID(GUID_SETTINGSDESIGNER_MenuGroup, IDM_VS_TOOLBAR_Settings)

            Public Shared ReadOnly CommandIDSettingsDesignerViewCode As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, CmdIdSETTINGSDESIGNERViewCode)
            Public Shared ReadOnly CommandIDSettingsDesignerSynchronize As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, CmdIdSETTINGSDESIGNERSynchronize)
            Public Shared ReadOnly CommandIDSettingsDesignerAccessModifierCombobox As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, CmdIdSETTINGSDESIGNERAccessModifierCombobox)
            Public Shared ReadOnly CommandIDSettingsDesignerGetAccessModifierOptions As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, CmdIdSETTINGSDESIGNERGetAccessModifierOptions)
            Public Shared ReadOnly CommandIDSettingsDesignerLoadWebSettings As New CommandID(s_GUID_SETTINGSDESIGNER_CommandID, CmdIdSETTINGSDESIGNERLoadWebSettings)

            ' Shared commands
            Public Shared ReadOnly CommandIDCOMMONEditCell As New CommandID(s_GUID_MS_VS_Editors_CommandId, CmdIdCOMMONEditCell)
            Public Shared ReadOnly CommandIDCOMMONAddRow As New CommandID(s_GUID_MS_VS_Editors_CommandId, CmdIdCOMMONAddRow)
            Public Shared ReadOnly CommandIDCOMMONRemoveRow As New CommandID(s_GUID_MS_VS_Editors_CommandId, CmdIdCOMMONRemoveRow)

#Region "My Extension feature menus"
            ' GUID for My Extension feature menus.
            Private Shared ReadOnly s_GUID_MYEXTENSION_Menu As New Guid("6C37AED7-D987-4fdf-ADF5-B71EB3F7236C")
            ' ID for My Extension context menu.
            Private Const IDM_CTX_MYEXTENSION_ContextMenu As Integer = &H110
            ' ID for My Extension menu buttons.
            Private Const CmdIdMYEXTENSIONAddExtension As Integer = &H2001
            Private Const CmdIdMYEXTENSIONRemoveExtension As Integer = &H2002
            ' Command IDs to use in My Extension Property Page
            Public Shared ReadOnly CommandIDMYEXTENSIONContextMenu As New CommandID(s_GUID_MYEXTENSION_Menu, IDM_CTX_MYEXTENSION_ContextMenu)
            Public Shared ReadOnly CommandIDMyEXTENSIONAddExtension As New CommandID(s_GUID_MYEXTENSION_Menu, CmdIdMYEXTENSIONAddExtension)
            Public Shared ReadOnly CommandIDMyEXTENSIONRemoveExtension As New CommandID(s_GUID_MYEXTENSION_Menu, CmdIdMYEXTENSIONRemoveExtension)
#End Region

        End Class
    End Class

End Namespace
