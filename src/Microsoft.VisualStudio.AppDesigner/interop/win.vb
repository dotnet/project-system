' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.AppDesInterop

    <System.Runtime.InteropServices.ComVisible(False)>
    Public Class Win32Constant
        Public Const CLSCTX_INPROC_SERVER As Integer = &H1
        Public Const CLSCTX_INPROC_HANDLER As Integer = &H2
        Public Const CLSCTX_LOCAL_SERVER As Integer = &H4
        Public Const CLSCTX_INPROC_SERVER16 As Integer = &H8
        Public Const CLSCTX_REMOTE_SERVER As Integer = &H10
        Public Const CLSCTX_INPROC_HANDLER16 As Integer = &H20
        Public Const CLSCTX_INPROC_SERVERX86 As Integer = &H40
        Public Const CLSCTX_INPROC_HANDLERX86 As Integer = &H80
        Public Const CLSCTX_ESERVER_HANDLER As Integer = &H100
        Public Const CLSCTX_RESERVED As Integer = &H200
        Public Const CLSCTX_NO_CODE_DOWNLOAD As Integer = &H400
        Public Const DISPID_UNKNOWN As Integer = -1
        Public Const DISP_E_MEMBERNOTFOUND As Integer = &H80020003
        Public Const DLGC_WANTTAB As Integer = &H2
        Public Const EM_UNDO As Integer = &HC7
        Public Const FNERR_BUFFERTOOSMALL As Integer = &H3003
        Public Const [FALSE] As Integer = 0
        Public Const FACILITY_WIN32 As Integer = 7
        Public Const GW_CHILD As UInteger = 5
        Public Const HDI_TEXT As Integer = &H2
        Public Const HDI_FORMAT As Integer = &H4
        Public Const HDI_IMAGE As Integer = &H20
        Public Const HDF_STRING As Integer = &H4000
        Public Const HDF_BITMAP_ON_RIGHT As Integer = &H1000
        Public Const HDF_IMAGE As Integer = &H800
        Public Const HDM_SETITEMW As Integer = &H1200 + 12
        Public Const HDM_SETIMAGELIST As Integer = &H1200 + 8
        Public Const LVM_EDITLABELA As Integer = &H1000 + 23
        Public Const LVM_EDITLABELW As Integer = &H1000 + 118
        Public Const LVM_GETHEADER As Integer = &H1000 + 31
        Public Const MAX_PATH As Integer = 260
        Public Const OLE_E_PROMPTSAVECANCELLED As Integer = &H8004000C
        Public Const OLECMDERR_E_NOTSUPPORTED As Integer = &H80040100
        Public Const QS_KEY As Integer = &H1
        Public Const QS_MOUSEMOVE As Integer = &H2
        Public Const QS_MOUSEBUTTON As Integer = &H4
        Public Const QS_POSTMESSAGE As Integer = &H8
        Public Const QS_TIMER As Integer = &H10
        Public Const QS_PAINT As Integer = &H20
        Public Const QS_SENDMESSAGE As Integer = &H40
        Public Const QS_HOTKEY As Integer = &H80
        Public Const QS_ALLPOSTMESSAGE As Integer = &H100
        Public Const QS_MOUSE As Integer = QS_MOUSEMOVE Or QS_MOUSEBUTTON
        Public Const QS_INPUT As Integer = QS_MOUSE Or QS_KEY
        Public Const QS_ALLEVENTS As Integer = QS_INPUT Or QS_POSTMESSAGE Or QS_TIMER Or QS_PAINT Or QS_HOTKEY
        Public Const QS_ALLINPUT As Integer = QS_INPUT Or QS_POSTMESSAGE Or QS_TIMER Or QS_PAINT Or QS_HOTKEY Or QS_SENDMESSAGE
        Public Const SC_CONTEXTHELP As Integer = &HF180
        Public Const SPI_GETSCREENREADER As Integer = 70
        Public Const [TRUE] As Integer = 1
        Public Const TVIF_STATE As Integer = &H8
        Public Const TVIS_STATEIMAGEMASK As Integer = &HF000
        Public Const TVM_SETITEMA As Integer = &H1100 + 13
        Public Const WAVE_FORMAT_PCM As Integer = &H1
        Public Const WAVE_FORMAT_ADPCM As Integer = &H2
        Public Const WAVE_FORMAT_IEEE_FLOAT As Integer = &H3
        Public Const WM_SETFOCUS As Integer = &H7
        Public Const WM_SYSCOLORCHANGE As Integer = &H15
        Public Const WM_SETTINGCHANGE As Integer = &H1A
        Public Const WM_HELP As Integer = &H53
        Public Const WM_CONTEXTMENU As Integer = &H7B
        Public Const WM_GETDLGCODE As Integer = &H87
        Public Const WM_KEYDOWN As Integer = &H100
        Public Const WM_KEYUP As Integer = &H101
        Public Const WM_CHAR As Integer = &H102
        Public Const WM_SYSKEYDOWN As Integer = &H104
        Public Const WM_SYSKEYUP As Integer = &H105
        Public Const WM_SYSCHAR As Integer = &H106
        Public Const WM_SYSCOMMAND As Integer = &H112
        Public Const WM_RBUTTONDOWN As Integer = &H204
        Public Const WM_RBUTTONUP As Integer = &H205
        Public Const WM_PASTE As Integer = &H302
        Public Const WM_PALETTECHANGED As Integer = &H311
        Public Const WM_THEMECHANGED = &H31A
        Public Const WM_USER As Integer = &H400

    End Class 'win

End Namespace
