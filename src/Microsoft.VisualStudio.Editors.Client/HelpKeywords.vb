' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

' All help keywords for anything inside this assembly should be defined here,
'   so that it is easier for UE to find them.
'

Option Explicit On
Option Strict On
Option Compare Binary

'****************************************************
'*****  Resource Editor Help IDs
'****************************************************

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend NotInheritable Class HelpIDs

        'General errors
        Public Const Err_CantFindResourceFile As String = "msvse_resedit.Err.CantFindResourceFile"
        Public Const Err_LoadingResource As String = "msvse_resedit.Err.LoadingResource"
        Public Const Err_NameBlank As String = "msvse_resedit.Err.NameBlank"
        Public Const Err_InvalidName As String = "msvse_resedit.Err.InvalidName"
        Public Const Err_DuplicateName As String = "msvse_resedit.Err.DuplicateName"
        Public Const Err_UnexpectedResourceType As String = "msvse_resedit.Err.UnexpectedResourceType"
        Public Const Err_CantCreateNewResource As String = "msvse_resedit.Err.CantCreateNewResource"
        Public Const Err_CantPlay As String = "msvse_resedit.Err.CantPlay"
        Public Const Err_CantConvertFromString As String = "msvse_resedit.Err.CantConvertFromString"
        Public Const Err_EditFormResx As String = "msvse_resedit.Err.EditFormResx"
        Public Const Err_CantAddFileToDeviceProject As String = "msvse_resedit.Err.CantAddFileToDeviceProject"
        Public Const Err_TypeIsNotSupported As String = "msvse_resedit.Err.TypeIsNotSupported"
        Public Const Err_CantSaveBadResouceItem As String = "msvse_resedit.Err.CantSaveBadResouceItem "
        Public Const Err_MaxFilesLimitation As String = "msvse_resedit.Err.MaxFilesLimitation"

        'Task list errors
        Public Const Task_BadLink As String = "msvse_resedit.tasklist.BadLink"
        Public Const Task_CantInstantiate As String = "msvse_resedit.tasklist.CantInstantiate"
        Public Const Task_NonrecommendedName As String = "msvse_resedit.tasklist.NonrecommendedName"
        Public Const Task_CantChangeCustomToolOrNamespace As String = "msvse_resedit.tasklist.CantChangeCustomToolOrNamespace"

        'Dialogs
        Public Const Dlg_OpenEmbedded As String = "msvse_resedit.dlg.OpenEmbedded"
        Public Const Dlg_QueryName As String = "msvse_resedit.dlg.QueryName"
        Public Const Dlg_OpenFileWarning As String = "msvse_resedit.dlg.OpenFileWarning"
    End Class

End Namespace
