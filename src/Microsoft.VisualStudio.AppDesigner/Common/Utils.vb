' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design
Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports VB = Microsoft.VisualBasic
Imports System.Reflection
Imports System.Threading

Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    Friend Module Utils

        'The transparent color used for all bitmaps in the resource editor is lime (R=0, G=255, B=0).
        '  Any pixels of this color will be converted to transparent if StandardTransparentColor
        '  is passed to GetManifestBitmap
        Public ReadOnly StandardTransparentColor As Color = Color.Lime

        Public VBPackageInstance As IVBPackage = Nothing

        ' The maximal amount of files that can be added at one shot. (copied from other VS features)
        Private Const s_VSDPLMAXFILES As Integer = 200

        'Property page GUIDs.  These are used only for sorting the tabs in the project designer, and for providing a
        '  unique ID for SQM.  Both cases are optional (we handle getting property pages with GUIDs we don't recognize).
        'PERF: NOTE: Initializing GUIDs from numeric values as below is a lot faster than initializing from strings.
        Public Class KnownPropertyPageGuids
            Public Shared ReadOnly GuidApplicationPage_VB As New Guid(&H8998E48EUI, &HB89AUS, &H4034US, &HB6, &H6E, &H35, &H3D, &H8C, &H1F, &HDC, &H2E)
            Public Shared ReadOnly GuidApplicationPage_VB_WPF As New Guid(&HAA1F44UI, &H2BA3US, &H4EAAUS, &HB5, &H4A, &HCE, &H18, &H0, &HE, &H6C, &H5D)
            Public Shared ReadOnly GuidApplicationPage_CS As New Guid(&H5E9A8AC2UI, &H4F34US, &H4521US, &H85, &H8F, &H4C, &H24, &H8B, &HA3, &H15, &H32)
            Public Shared ReadOnly GuidApplicationPage_JS As Guid = GuidApplicationPage_CS
            Public Shared ReadOnly GuidSigningPage As New Guid(&HF8D6553FUI, &HF752US, &H4DBFUS, &HAC, &HB6, &HF2, &H91, &HB7, &H44, &HA7, &H92)
            Public Shared ReadOnly GuidReferencesPage_VB As New Guid(&H4E43F4ABUI, &H9F03US, &H4129US, &H95, &HBF, &HB8, &HFF, &H87, &HA, &HF6, &HAB)
            Public Shared ReadOnly GuidServicesPropPage As New Guid(&H43E38D2EUI, &H4EB8US, &H4204US, &H82, &H25, &H93, &H57, &H31, &H61, &H37, &HA4)
            Public Shared ReadOnly GuidSecurityPage As New Guid(&HDF8F7042UI, &HBB1US, &H47D1US, &H8E, &H6D, &HDE, &HB3, &HD0, &H76, &H98, &HBD)
            Public Shared ReadOnly GuidSecurityPage_WPF As New Guid(&HA2C8FEUI, &H3844US, &H41BEUS, &H96, &H37, &H16, &H74, &H54, &HA7, &HF1, &HA7)
            Public Shared ReadOnly GuidPublishPage As New Guid(&HCC4014F5UI, &HB18DUS, &H439CUS, &H93, &H52, &HF9, &H9D, &H98, &H4C, &HCA, &H85)
            Public Shared ReadOnly GuidDebugPage As New Guid(&H6185191FUI, &H1008US, &H4FB2US, &HA7, &H15, &H3A, &H4E, &H4F, &H27, &HE6, &H10)
            Public Shared ReadOnly GuidCompilePage_VB As New Guid(&HEDA661EAUI, &HDC61US, &H4750US, &HB3, &HA5, &HF6, &HE9, &HC7, &H40, &H60, &HF5)
            Public Shared ReadOnly GuidBuildPage_CS As New Guid(&HA54AD834UI, &H9219US, &H4AA6US, &HB5, &H89, &H60, &H7A, &HF2, &H1C, &H3E, &H26)
            Public Shared ReadOnly GuidBuildPage_JS As New Guid(&H8ADF8DB1UI, &HA8B8US, &H4E04US, &HA6, &H16, &H2E, &HFC, &H59, &H5F, &H27, &HF4)
            Public Shared ReadOnly GuidReferencePathsPage As New Guid(&H31911C8UI, &H6148US, &H4E25US, &HB1, &HB1, &H44, &HBC, &HA9, &HA0, &HC4, &H5C)
            Public Shared ReadOnly GuidBuildEventsPage As New Guid(&H1E78F8DBUI, &H6C07US, &H4D61US, &HA1, &H8F, &H75, &H14, &H1, &HA, &HBD, &H56)
            Public Shared ReadOnly GuidDatabasePage_SQL As New Guid(&H87F6ADCEUI, &H9161US, &H489FUS, &H90, &H7E, &H39, &H30, &HA6, &H42, &H96, &H9)
            Public Shared ReadOnly GuidFxCopPage As New Guid(&H984AE51AUI, &H4B21US, &H44E7US, &H82, &H2C, &HDD, &H5E, &H4, &H68, &H93, &HEF)
            Public Shared ReadOnly GuidDeployPage As New Guid(&H29AB1D1BUI, &H10E8US, &H4511US, &HA3, &H62, &HEF, &H15, &H71, &HB8, &H44, &H3C)
            Public Shared ReadOnly GuidDevicesPage_VSD As New Guid(&H7B74AADFUI, &HACA4US, &H410EUS, &H8D, &H4B, &HAF, &HE1, &H19, &H83, &H5B, &H99)
            Public Shared ReadOnly GuidDebugPage_VSD As New Guid(&HAC5FAEC7UI, &HD452US, &H4AC1US, &HBC, &H44, &H2D, &H7E, &HCE, &H6D, &HF0, &H6C)
            Public Shared ReadOnly GuidMyExtensionsPage As New Guid(&HF24459FCUI, &HE883US, &H4A8EUS, &H9D, &HA2, &HAE, &HF6, &H84, &HF0, &HE1, &HF4)
            Public Shared ReadOnly GuidOfficePublishPage As New Guid(&HCC7369A8UI, &HB9B0US, &H439CUS, &HB1, &H36, &HBA, &H95, &H58, &H19, &HF7, &HF8)
            Public Shared ReadOnly GuidServicesPage As New Guid(&H43E38D2EUI, &H43B8US, &H4204US, &H82, &H25, &H93, &H57, &H31, &H61, &H37, &HA4)
            Public Shared ReadOnly GuidWAPWebPage As New Guid(&H909D16B3UI, &HC8E8US, &H43D1US, &HA2, &HB8, &H26, &HEA, &HD, &H4B, &H6B, &H57)
        End Class


        ''' <summary>
        ''' Helper to convert ItemIds or other 32 bit ID values
        ''' where it is sometimes treated as an Int32 and sometimes UInt32
        ''' ItemId is sometimes marshaled as a VT_INT_PTR, and often declared 
        ''' UInt in the interop assemblies. Otherwise we get overflow exceptions converting 
        ''' negative numbers to UInt32.  We just want raw bit translation.
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function NoOverflowCUInt(ByVal obj As Object) As UInteger
            Return NoOverflowCUInt(CLng(obj))
        End Function

        ''' <summary>
        ''' Masks the top 32 bits to get just the lower 32bit number
        ''' </summary>
        ''' <param name="LongValue"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function NoOverflowCUInt(ByVal LongValue As Long) As UInteger
            Return CUInt(LongValue And UInt32.MaxValue)
        End Function

        Public Function NoOverflowCInt(ByVal LongValue As Long) As Integer
            If LongValue <= UInt32.MaxValue Then
                Return CInt(LongValue)
            End If
            Return CInt(LongValue And UInt32.MaxValue)
        End Function

        ''' <summary>
        ''' Retrieves a given bitmap from the manifest resources (unmodified)
        ''' </summary>
        ''' <param name="BitmapID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <param name="assembly">Name of the assembly containing the resource</param>
        ''' <returns>The retrieved bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestBitmap(ByVal BitmapID As String, Optional ByRef assembly As Assembly = Nothing) As Bitmap
            Return DirectCast(GetManifestImage(BitmapID, assembly), Bitmap)
        End Function


        ''' <summary>
        ''' Retrieves a transparent copy of a given bitmap from the manifest resources.
        ''' </summary>
        ''' <param name="BitmapID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <param name="TransparentColor">The color that represents transparent in the bitmap</param>
        ''' <param name="assembly">Name of the assembly containing the bitmap resource</param>
        ''' <returns>The retrieved transparent bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestBitmapTransparent(ByVal BitmapID As String, ByRef TransparentColor As Color, Optional ByVal assembly As Assembly = Nothing) As Bitmap
            Dim Bitmap As Bitmap = GetManifestBitmap(BitmapID, assembly)
            If Bitmap IsNot Nothing Then
                Bitmap.MakeTransparent(TransparentColor)
                Return Bitmap
            Else
                Debug.Fail("Couldn't find internal resource")
                Throw New Package.InternalException(String.Format(SR.RSE_Err_Unexpected_NoResource_1Arg, BitmapID))
            End If
        End Function


        ''' <summary>
        ''' Retrieves a transparent copy of a given bitmap from the manifest resources.
        ''' </summary>
        ''' <param name="BitmapID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <param name="assembly">Name of assembly containing the manifest resource</param>
        ''' <returns>The retrieved transparent bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestBitmapTransparent(ByVal BitmapID As String, Optional ByRef assembly As Assembly = Nothing) As Bitmap
            Return GetManifestBitmapTransparent(BitmapID, StandardTransparentColor, assembly)
        End Function

        ''' <summary>
        ''' Retrieves a given image from the manifest resources.
        ''' </summary>
        ''' <param name="ImageID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <param name="assembly"></param>
        ''' <returns>The retrieved bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestImage(ByVal ImageID As String, Optional ByRef assembly As Assembly = Nothing) As Image
            Dim BitmapStream As Stream = GetType(Microsoft.VisualStudio.Editors.AppDesCommon.Utils).Assembly.GetManifestResourceStream(ImageID)
            If assembly IsNot Nothing Then
                BitmapStream = assembly.GetManifestResourceStream(ImageID)
            End If
            If BitmapStream IsNot Nothing Then
                Dim Image As Image = Drawing.Image.FromStream(BitmapStream)
                If Not Image Is Nothing Then
                    Return Image
                End If
            End If
            Debug.Fail("Unable to find image resource from manifest: " & ImageID)
            Throw New Package.InternalException(String.Format(SR.RSE_Err_Unexpected_NoResource_1Arg, ImageID))
        End Function


        ''' <summary>
        ''' Logical implies.  Often useful in Debug.Assert's.  Essentially, it is to be
        '''   read as "a being true implies that b is true".  Therefore, the function returns
        '''  False if a is true and b is false.  Otherwise it returns True (as there's no
        '''   evidence to suggest that the implication is incorrect).
        ''' </summary>
        ''' <remarks></remarks>
        Public Function Implies(ByVal a As Boolean, ByVal b As Boolean) As Boolean
            Return Not (a And Not b)
        End Function


        ''' <summary>
        ''' Retrieves the error message from an exception in a manner appropriate for the build.  For release, simply
        '''   retrieves ex.Message (just the message, no call stack).  For debug builds, appends the callstack and
        '''   also the inner exception, if any.
        ''' </summary>
        ''' <param name="ex"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function DebugMessageFromException(ByVal ex As Exception) As String
#If DEBUG Then
            Dim ErrorMessage =
$"{ex.Message}


[SHOWN In DEBUG ONLY] STACK TRACE:
{ ex.StackTrace}"
            If ex.InnerException IsNot Nothing Then
                ErrorMessage &=
$"

INNER EXCEPTION: 

{ex.InnerException.ToString()}"
            End If

            Return ErrorMessage
#Else
            Return ex.Message
#End If
        End Function


        ''' <summary>
        ''' Attempts to create a string represention of an object, for debug purposes.  Under retail,
        '''   returns an empty string.
        ''' </summary>
        ''' <param name="Value">The value to turn into a displayable string.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function DebugToString(ByVal Value As Object) As String
#If DEBUG Then
            Dim StringValue As String = ""
            Try
                If Value Is Nothing Then
                    Return "<Nothing>"
                ElseIf TypeOf Value Is String Then
                    Return """" & CStr(Value) & """"
                ElseIf TypeOf Value Is Control Then
                    Dim c As Control = DirectCast(Value, Control)
                    If c.Name <> "" Then
                        Return c.Name & " (Text=""" & c.Text & """)"
                    Else
                        Return "[" & c.GetType.Name & "] (Text=""" & c.Text & """)"
                    End If
                Else
                    Return Value.ToString()
                End If
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Return "[" & ex.GetType.Name & "]"
            End Try
#Else
            Return ""
#End If
        End Function


        ''' <summary>
        ''' Given an exception, returns True if it is an "unrecoverable" exception.
        ''' </summary>
        ''' <param name="ex">The exception to check rethrow if it's unrecoverable</param>
        ''' <param name="IgnoreOutOfMemory">If True, out of memory will not be considered unrecoverable.</param>
        ''' <remarks></remarks>
        Public Function IsUnrecoverable(ByVal ex As Exception, Optional ByVal IgnoreOutOfMemory As Boolean = False) As Boolean
            Return (Not IgnoreOutOfMemory AndAlso TypeOf ex Is OutOfMemoryException) OrElse
                    TypeOf ex Is StackOverflowException OrElse
                    TypeOf ex Is ThreadAbortException OrElse
                    TypeOf ex Is AccessViolationException
        End Function


        ''' <summary>
        ''' Given an exception, returns True if it is a CheckOut exception.
        ''' </summary>
        ''' <param name="ex">The exception to check rethrow if it's caused by cancaling checkout</param>
        ''' <remarks></remarks>
        Public Function IsCheckoutCanceledException(ByVal ex As Exception) As Boolean
            If (TypeOf ex Is CheckoutException AndAlso ex.Equals(CheckoutException.Canceled)) OrElse
                (TypeOf ex Is COMException AndAlso DirectCast(ex, COMException).ErrorCode = AppDesInterop.win.OLE_E_PROMPTSAVECANCELLED) Then

                Return True
            End If

            If ex.InnerException IsNot Nothing Then Return IsCheckoutCanceledException(ex.InnerException)
            Return False
        End Function


        ''' <summary>
        ''' Given an exception, rethrows it if it is an "unrecoverable" exception.  Otherwise does nothing.
        ''' </summary>
        ''' <param name="ex">The exception to check rethrow if it's unrecoverable</param>
        ''' <param name="IgnoreOutOfMemory">If True, out of memory will not be considered unrecoverable.</param>
        ''' <remarks></remarks>
        Public Sub RethrowIfUnrecoverable(ByVal ex As Exception, Optional ByVal IgnoreOutOfMemory As Boolean = False)
            If IsUnrecoverable(ex, IgnoreOutOfMemory) Then Throw ex
        End Sub


        ''' <summary>
        ''' If the given string is Nothing, return "", else return the original string.
        ''' </summary>
        ''' <param name="Str"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function NothingToEmptyString(ByVal Str As String) As String
            Return If(Str, String.Empty)
        End Function


        ''' <summary>
        ''' If the given string is "", return Nothing, else return the original string.
        ''' </summary>
        ''' <param name="Str"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function EmptyStringToNothing(ByVal Str As String) As String
            If Str Is Nothing OrElse Str.Length = 0 Then Return Nothing
            Return Str
        End Function

        ''' <summary>
        ''' A better IIf
        ''' </summary>
        ''' <param name="Condition">The condition to test.</param>
        ''' <param name="TrueExpression">What to return if the condition is True</param>
        ''' <param name="FalseExpression">What to return if the condition is False</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function IIf(Of T)(ByVal Condition As Boolean, ByVal TrueExpression As T, ByVal FalseExpression As T) As T
            Return If(Condition, TrueExpression, FalseExpression)
        End Function


        ''' <summary>
        ''' Set the drop-down width of a combobox wide enough to show the text of all entries in it
        ''' </summary>
        ''' <param name="ComboBox">The combobox to change the width for</param>
        ''' <remarks></remarks>
        Public Sub SetComboBoxDropdownWidth(ByVal ComboBox As ComboBox)
            If ComboBox IsNot Nothing Then
                ComboBox.DropDownWidth = Math.Max(MeasureMaxTextWidth(ComboBox, ComboBox.Items), ComboBox.Width)
            Else
                Debug.Fail($"{NameOf(SetComboBoxDropdownWidth)}: No combobox specified")
            End If
        End Sub

        ''' <summary>
        ''' Set the drop-down width of a datagridviewcomboboxcolumn wide enough to show the text of all entries in it
        ''' </summary>
        ''' <param name="column">The columnto change the width for</param>
        ''' <remarks>
        ''' This does not take the current cell style into account - it uses the font from the parent datagridview (if any)
        ''' It also makes room for the scrollbar even though it may not be visible...
        ''' </remarks>
        Public Sub SetComboBoxColumnDropdownWidth(ByVal column As DataGridViewComboBoxColumn)
            If column IsNot Nothing AndAlso column.DataGridView IsNot Nothing Then
                column.DropDownWidth = Math.Max(MeasureMaxTextWidth(column.DataGridView, column.Items) + SystemInformation.VerticalScrollBarWidth, column.Width)
            Else
                Debug.Fail($"{NameOf(SetComboBoxColumnDropdownWidth)}: No combobox column specified, or the column didn't have a parent datagridview!")
            End If
        End Sub

        ''' <summary>
        ''' Check whether the screen reader is running
        ''' </summary>
        Public Function IsScreenReaderRunning() As Boolean
            Dim pvParam As IntPtr = Marshal.AllocCoTaskMem(4)
            Try
                If AppDesInterop.NativeMethods.SystemParametersInfo(AppDesInterop.win.SPI_GETSCREENREADER, 0, pvParam, 0) <> 0 Then
                    Dim result As Int32 = Marshal.ReadInt32(pvParam)
                    Return result <> 0
                End If
            Finally
                Marshal.FreeCoTaskMem(pvParam)
            End Try
            Return False
        End Function

        ''' <summary>
        ''' Sets error code and error message through IVsUIShell interface
        ''' </summary>
        ''' <param name="hr">error code</param>
        ''' <param name="error message">error message</param>
        Public Sub SetErrorInfo(ByVal sp As Shell.ServiceProvider, ByVal hr As Integer, ByVal errorMessage As String)
            Dim vsUIShell As Shell.Interop.IVsUIShell = Nothing

            If sp IsNot Nothing Then
                vsUIShell = CType(sp.GetService(GetType(Shell.Interop.IVsUIShell)), Shell.Interop.IVsUIShell)
            End If

            If vsUIShell Is Nothing AndAlso Not VBPackageInstance IsNot Nothing Then
                vsUIShell = CType(VBPackageInstance.GetService(GetType(Shell.Interop.IVsUIShell)), Shell.Interop.IVsUIShell)
            End If

            If vsUIShell IsNot Nothing Then
                vsUIShell.SetErrorInfo(hr, errorMessage, 0, Nothing, Nothing)
            Else
                Debug.Fail("Could not get " & NameOf(Shell.Interop.IVsUIShell) & " from service provider. Can't set specific error message.")
            End If
        End Sub


        ''' <summary>
        ''' Sets focus to the first (or last) control inside of a parent HWND.
        ''' </summary>
        ''' <param name="HwndParent">The container HWND.</param>
        ''' <param name="First">If True, sets focus to the first control, otherwise the last.</param>
        ''' <remarks></remarks>
        Public Function FocusFirstOrLastTabItem(ByVal HwndParent As IntPtr, ByVal First As Boolean) As Boolean
            If HwndParent.Equals(IntPtr.Zero) Then
                Return False
            End If

            Dim c As Control = Control.FromChildHandle(HwndParent)
            If c IsNot Nothing Then
                'WinForms controls don't set WS_TABSTOP so GetNextDlgTabItem doesn't work well for them.

                Dim TabStopOnly As Boolean = True
                Dim Nested As Boolean = True
                Dim Wrap As Boolean = True
                If c.SelectNextControl(Nothing, First, TabStopOnly, Nested, Wrap) Then
                    Dim cc As ContainerControl = TryCast(c, ContainerControl)
                    If cc IsNot Nothing AndAlso cc.ActiveControl IsNot Nothing Then
                        cc.ActiveControl.Focus()
                    End If

                    Return True
                End If

                'Perhaps all the controls are disabled
                Return False
            End If

            'Use standard Win32 function for native dialog pages
            Dim FirstTabStop As IntPtr = AppDesInterop.NativeMethods.GetNextDlgTabItem(HwndParent, IntPtr.Zero, False)
            If FirstTabStop.Equals(IntPtr.Zero) Then
                Return False
            End If

            Dim NextTabStop As IntPtr
            If First Then
                NextTabStop = FirstTabStop
            Else
                NextTabStop = AppDesInterop.NativeMethods.GetNextDlgTabItem(HwndParent, FirstTabStop, True)
            End If

            If NextTabStop.Equals(IntPtr.Zero) Then
                Return False
            End If

            AppDesInterop.NativeMethods.SetFocus(NextTabStop)
            Return True
        End Function

        ''' <summary>
        ''' Returns a given path with a backslash at the end, if not already there.
        ''' </summary>
        ''' <param name="Path">The path to add a backslash to.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function AppendBackslash(ByVal Path As String) As String
            If Path <> "" AndAlso Path(Path.Length - 1) <> IO.Path.DirectorySeparatorChar AndAlso Path(Path.Length - 1) <> IO.Path.AltDirectorySeparatorChar Then
                Return Path & IO.Path.DirectorySeparatorChar
            Else
                Return Path
            End If
        End Function


        ''' <summary>
        ''' Browses for a File.
        ''' </summary>
        ''' <param name="ServiceProvider">Service Provider</param>
        ''' <param name="ParentWindow">Window Handle of the parent window</param>
        ''' <param name="InitialDirectory">The initial directory for the dialog.  Can be Nothing or empty.</param>
        ''' <param name="DialogTitle">The title to use for the browse dialog.</param>
        ''' <param name="Filter">file type filter</param>
        ''' <param name="FilterIndex"></param>
        ''' <param name="MutiSelect">Whether we should support multi-selection</param>
        ''' <param name="NeedThrowError">Throw error when the dialog fails unexpectedly</param>
        ''' <returns>a collection of files</returns>
        ''' <remarks></remarks>
        Public Function GetFilesViaBrowse(
                                          ServiceProvider As IServiceProvider,
                                          ParentWindow As IntPtr,
                                          InitialDirectory As String,
                                          DialogTitle As String,
                                          Filter As String,
                                          FilterIndex As UInteger,
                                          MutiSelect As Boolean,
                                          Optional DefaultFileName As String = Nothing,
                                          Optional NeedThrowError As Boolean = False
                                         ) As ArrayList

            Dim uishell = CType(ServiceProvider.GetService(GetType(Shell.Interop.IVsUIShell)), Shell.Interop.IVsUIShell)

            Dim fileNames As New ArrayList()

            InitialDirectory = NormalizeInitialDirectory(InitialDirectory)
            If InitialDirectory = "" Then
                InitialDirectory = Nothing
            End If

            Filter = GetNativeFilter(Filter)

            Dim MaxPathName As Integer = AppDesInterop.win.MAX_PATH + 1
            If MutiSelect Then
                MaxPathName = (AppDesInterop.win.MAX_PATH + 1) * s_VSDPLMAXFILES
            End If

            Dim vsOpenFileName As Shell.Interop.VSOPENFILENAMEW()

            Dim defaultName(MaxPathName) As Char
            If DefaultFileName IsNot Nothing Then
                DefaultFileName.CopyTo(0, defaultName, 0, DefaultFileName.Length)
            End If

            Dim stringMemPtr As IntPtr = Marshal.AllocHGlobal(MaxPathName * 2 + 2)
            Marshal.Copy(defaultName, 0, stringMemPtr, defaultName.Length)

            Try
                vsOpenFileName = New Shell.Interop.VSOPENFILENAMEW(0) {}
                With vsOpenFileName(0)
                    .lStructSize = CUInt(Marshal.SizeOf(vsOpenFileName(0)))
                    .hwndOwner = ParentWindow
                    .pwzDlgTitle = DialogTitle
                    .nMaxFileName = CUInt(MaxPathName)
                    .pwzFileName = stringMemPtr
                    .pwzInitialDir = InitialDirectory
                    .pwzFilter = Filter
                    .nFilterIndex = FilterIndex
                    .nFileOffset = 0
                    .nFileExtension = 0
                    .dwHelpTopic = 0
                End With

                If MutiSelect Then
                    vsOpenFileName(0).dwFlags = &H200   'OFN_ALLOWMULTISELECT
                Else
                    vsOpenFileName(0).dwFlags = 0
                End If

                Dim hr As Integer = uishell.GetOpenFileNameViaDlg(vsOpenFileName)
                If VSErrorHandler.Succeeded(hr) Then
                    Dim buffer(MaxPathName) As Char
                    Marshal.Copy(stringMemPtr, buffer, 0, buffer.Length)
                    Dim path As String = Nothing
                    Dim i As Integer = 0
                    For j As Integer = 0 To buffer.Length - 1
                        If buffer(j) = Chr(0) Then
                            If i = j Then
                                Exit For
                            End If
                            If i = 0 Then
                                path = New String(buffer, 0, j)
                            Else
                                fileNames.Add(path & IO.Path.DirectorySeparatorChar & New String(buffer, i, j - i))
                            End If
                            i = j + 1
                        End If
                    Next

                    If fileNames.Count = 0 AndAlso path IsNot Nothing Then
                        fileNames.Add(path)
                    End If
                ElseIf NeedThrowError Then
                    If hr = AppDesInterop.win.OLE_E_PROMPTSAVECANCELLED Then
                        'We shouldn't thrown error, if User cancelled out of dialog
                    Else
                        VSErrorHandler.ThrowOnFailure(hr)
                    End If
                End If
            Finally
                Marshal.FreeHGlobal(stringMemPtr)
            End Try

            Return fileNames
        End Function


        '@ <summary>
        '@ Change the Filter String to the format we can use in IVsUIShell function
        '@ </summary>
        '@ <param name="Filter">file type filter</param>
        '@ <returns>a native filter string</returns>
        Private Function GetNativeFilter(ByVal Filter As String) As String
            If Filter IsNot Nothing Then
                Dim length As Integer = Filter.Length
                Dim buf As Char() = New Char(length) {}

                Filter.CopyTo(0, buf, 0, length)

                For i As Integer = 0 To length - 1
                    If buf(i) = "|"c Then
                        buf(i) = Chr(0)
                    End If
                Next
                Filter = New String(buf)
            End If
            Return Filter
        End Function

        '@ <summary>
        '@ Change the InitialDirectory path to the format we can use in IVsUIShell function
        '@ </summary>
        '@ <param name="InitialDirectory">The initial directory for the dialog.  Can be Nothing or empty.</param>
        '@ <returns>a directory path</returns>
        Private Function NormalizeInitialDirectory(ByVal InitialDirectory As String) As String
            If InitialDirectory IsNot Nothing Then
                InitialDirectory = Trim(InitialDirectory)
                If InitialDirectory = "" Then
                    InitialDirectory = String.Empty
                Else
                    Try
                        'Path needs a backslash at the end, or it will be interpreted as a directory + filename
                        InitialDirectory = Path.GetFullPath(AppendBackslash(InitialDirectory))
                    Catch ex As Exception
                        RethrowIfUnrecoverable(ex)
                        InitialDirectory = String.Empty
                    End Try
                End If
            Else
                InitialDirectory = String.Empty
            End If
            Return InitialDirectory
        End Function


        ''' <summary>
        ''' Helper method to measure the maximum width of a collection of strings given a particular font...
        ''' </summary>
        ''' <param name="ctrl"></param>
        ''' <param name="items"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function MeasureMaxTextWidth(ByVal ctrl As Control, ByVal items As IEnumerable) As Integer
            Dim MaxEntryWidth As Integer = 0
            Using g As Graphics = ctrl.CreateGraphics()
                For Each Entry As Object In items
                    Dim EntryText As String = ""
                    If Entry Is Nothing Then
                        EntryText = ""
                    ElseIf TypeOf Entry Is String Then
                        EntryText = DirectCast(Entry, String)
                    Else
                        'CONSIDER: should try type converter first
                        EntryText = Entry.ToString()
                    End If

                    Dim Width As Integer = CInt(g.MeasureString(EntryText, ctrl.Font).Width)
                    MaxEntryWidth = Math.Max(MaxEntryWidth, Width)
                Next
            End Using
            Return MaxEntryWidth
        End Function

#Region "SQM data point helpers"
        Public Class SQMData

            Private Sub New()
                ' Non-creatable class
            End Sub

            'A list of known editor guids
            ' Each property page will be reported back to SQM with the 1-based index in which it is present 
            ' in this list. All unknown entries will be reported as &hFF
            '
            ' Add more entries to the end of this list. Do *not* put any new entries in the middle of the list!
            Private Shared s_sqmOrder() As Guid = {
                KnownPropertyPageGuids.GuidApplicationPage_VB,
                KnownPropertyPageGuids.GuidApplicationPage_CS,
                KnownPropertyPageGuids.GuidApplicationPage_JS,
                KnownPropertyPageGuids.GuidCompilePage_VB,
                KnownPropertyPageGuids.GuidBuildPage_CS,
                KnownPropertyPageGuids.GuidBuildPage_JS,
                KnownPropertyPageGuids.GuidBuildEventsPage,
                KnownPropertyPageGuids.GuidDebugPage,
                KnownPropertyPageGuids.GuidReferencesPage_VB,
                KnownPropertyPageGuids.GuidReferencePathsPage,
                 KnownPropertyPageGuids.GuidSigningPage,
                KnownPropertyPageGuids.GuidSecurityPage,
                KnownPropertyPageGuids.GuidPublishPage,
                KnownPropertyPageGuids.GuidDatabasePage_SQL,
                KnownPropertyPageGuids.GuidFxCopPage,
                KnownPropertyPageGuids.GuidDeployPage,
                KnownPropertyPageGuids.GuidDevicesPage_VSD,
                KnownPropertyPageGuids.GuidDebugPage_VSD,
                KnownPropertyPageGuids.GuidApplicationPage_VB_WPF,
                KnownPropertyPageGuids.GuidSecurityPage_WPF,
                KnownPropertyPageGuids.GuidMyExtensionsPage,
                KnownPropertyPageGuids.GuidOfficePublishPage,
                KnownPropertyPageGuids.GuidServicesPage,
                KnownPropertyPageGuids.GuidWAPWebPage
            }

            Public Const UNKNOWN_PAGE As Byte = &HFF
            Public Const DEFAULT_PAGE As Byte = 0

            ''' <summary>
            ''' Map a known property page or designer id to a unique unsigned char in order
            ''' to report back to SQM what the values are...
            ''' </summary>
            ''' <param name="guid"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Shared Function PageGuidToId(ByVal guid As Guid) As Byte
                For i As Integer = 0 To s_sqmOrder.Length - 1
                    If s_sqmOrder(i).Equals(guid) Then
                        Return CByte(i + 1)
                    End If
                Next
                Return UNKNOWN_PAGE
            End Function

        End Class
#End Region


    End Module
End Namespace
