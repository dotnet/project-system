' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Runtime.Serialization
Imports System.Runtime.Versioning
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Imaging.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Telemetry
Imports Microsoft.VSDesigner

Imports GelUtilities = Microsoft.Internal.VisualStudio.PlatformUI.Utilities

Namespace Microsoft.VisualStudio.Editors.Common

    Friend Module Utils

        'The transparent color used for all bitmaps in the resource editor is lime (R=0, G=255, B=0).
        '  Any pixels of this color will be converted to transparent if StandardTransparentColor
        '  is passed to GetManifestBitmap
        Public ReadOnly StandardTransparentColor As Color = Color.Lime

        ' The maximal amount of files that can be added at one shot. (copied from other VS features)
        Private Const VSDPLMAXFILES As Integer = 200

        Private s_imageService As IVsImageService2

        'Property page GUIDs.  These are used only for sorting the tabs in the project designer, and for providing a
        '  unique ID for SQM.  Both cases are optional (we handle getting property pages with GUIDs we don't recognize).
        'PERF: NOTE: Initializing GUIDs from numeric values as below is a lot faster than initializing from strings.
        Friend Class KnownPropertyPageGuids
            Friend Shared ReadOnly GuidApplicationPage_VB As Guid = New Guid(&H8998E48EUI, &HB89AUS, &H4034US, &HB6, &H6E, &H35, &H3D, &H8C, &H1F, &HDC, &H2E)
            Friend Shared ReadOnly GuidApplicationPage_VB_WPF As Guid = New Guid(&HAA1F44UI, &H2BA3US, &H4EAAUS, &HB5, &H4A, &HCE, &H18, &H0, &HE, &H6C, &H5D)
            Friend Shared ReadOnly GuidApplicationPage_CS As Guid = New Guid(&H5E9A8AC2UI, &H4F34US, &H4521US, CByte(&H85), CByte(&H8F), CByte(&H4C), CByte(&H24), CByte(&H8B), CByte(&HA3), CByte(&H15), CByte(&H32))
            Friend Shared ReadOnly GuidApplicationPage_JS As Guid = GuidApplicationPage_CS
            Friend Shared ReadOnly GuidSigningPage As Guid = New Guid(&HF8D6553FUI, &HF752US, &H4DBFUS, CByte(&HAC), CByte(&HB6), CByte(&HF2), CByte(&H91), CByte(&HB7), CByte(&H44), CByte(&HA7), CByte(&H92))
            Friend Shared ReadOnly GuidReferencesPage_VB As Guid = New Guid(&H4E43F4ABUI, &H9F03US, &H4129US, CByte(&H95), CByte(&HBF), CByte(&HB8), CByte(&HFF), CByte(&H87), CByte(&HA), CByte(&HF6), CByte(&HAB))
            Friend Shared ReadOnly GuidServicesPropPage As Guid = New Guid(&H43E38D2EUI, &H4EB8US, &H4204US, CByte(&H82), CByte(&H25), CByte(&H93), CByte(&H57), CByte(&H31), CByte(&H61), CByte(&H37), CByte(&HA4))
            Friend Shared ReadOnly GuidSecurityPage As Guid = New Guid(&HDF8F7042UI, &HBB1US, &H47D1US, CByte(&H8E), CByte(&H6D), CByte(&HDE), CByte(&HB3), CByte(&HD0), CByte(&H76), CByte(&H98), CByte(&HBD))
            Friend Shared ReadOnly GuidSecurityPage_WPF As Guid = New Guid(&HA2C8FEUI, &H3844US, &H41BEUS, CByte(&H96), CByte(&H37), CByte(&H16), CByte(&H74), CByte(&H54), CByte(&HA7), CByte(&HF1), CByte(&HA7))
            Friend Shared ReadOnly GuidPublishPage As Guid = New Guid(&HCC4014F5UI, &HB18DUS, &H439CUS, CByte(&H93), CByte(&H52), CByte(&HF9), CByte(&H9D), CByte(&H98), CByte(&H4C), CByte(&HCA), CByte(&H85))
            Friend Shared ReadOnly GuidDebugPage As Guid = New Guid(&H6185191FUI, &H1008US, &H4FB2US, CByte(&HA7), CByte(&H15), CByte(&H3A), CByte(&H4E), CByte(&H4F), CByte(&H27), CByte(&HE6), CByte(&H10))
            Friend Shared ReadOnly GuidCompilePage_VB As Guid = New Guid(&HEDA661EAUI, &HDC61US, &H4750US, CByte(&HB3), CByte(&HA5), CByte(&HF6), CByte(&HE9), CByte(&HC7), CByte(&H40), CByte(&H60), CByte(&HF5))
            Friend Shared ReadOnly GuidBuildPage_CS As Guid = New Guid(&HA54AD834UI, &H9219US, &H4AA6US, CByte(&HB5), CByte(&H89), CByte(&H60), CByte(&H7A), CByte(&HF2), CByte(&H1C), CByte(&H3E), CByte(&H26))
            Friend Shared ReadOnly GuidBuildPage_JS As Guid = New Guid(&H8ADF8DB1UI, &HA8B8US, &H4E04US, CByte(&HA6), CByte(&H16), CByte(&H2E), CByte(&HFC), CByte(&H59), CByte(&H5F), CByte(&H27), CByte(&HF4))
            Friend Shared ReadOnly GuidReferencePathsPage As Guid = New Guid(&H31911C8UI, &H6148US, &H4E25US, CByte(&HB1), CByte(&HB1), CByte(&H44), CByte(&HBC), CByte(&HA9), CByte(&HA0), CByte(&HC4), CByte(&H5C))
            Friend Shared ReadOnly GuidBuildEventsPage As Guid = New Guid(&H1E78F8DBUI, &H6C07US, &H4D61US, CByte(&HA1), CByte(&H8F), CByte(&H75), CByte(&H14), CByte(&H1), CByte(&HA), CByte(&HBD), CByte(&H56))
            Friend Shared ReadOnly GuidDatabasePage_SQL As Guid = New Guid(&H87F6ADCEUI, &H9161US, &H489FUS, CByte(&H90), CByte(&H7E), CByte(&H39), CByte(&H30), CByte(&HA6), CByte(&H42), CByte(&H96), CByte(&H9))
            Friend Shared ReadOnly GuidFxCopPage As Guid = New Guid(&H984AE51AUI, &H4B21US, &H44E7US, CByte(&H82), CByte(&H2C), CByte(&HDD), CByte(&H5E), CByte(&H4), CByte(&H68), CByte(&H93), CByte(&HEF))
            Friend Shared ReadOnly GuidDeployPage As Guid = New Guid(&H29AB1D1BUI, &H10E8US, &H4511US, CByte(&HA3), CByte(&H62), CByte(&HEF), CByte(&H15), CByte(&H71), CByte(&HB8), CByte(&H44), CByte(&H3C))
            Friend Shared ReadOnly GuidDevicesPage_VSD As Guid = New Guid(&H7B74AADFUI, &HACA4US, &H410EUS, CByte(&H8D), CByte(&H4B), CByte(&HAF), CByte(&HE1), CByte(&H19), CByte(&H83), CByte(&H5B), CByte(&H99))
            Friend Shared ReadOnly GuidDebugPage_VSD As Guid = New Guid(&HAC5FAEC7UI, &HD452US, &H4AC1US, CByte(&HBC), CByte(&H44), CByte(&H2D), CByte(&H7E), CByte(&HCE), CByte(&H6D), CByte(&HF0), CByte(&H6C))
            Friend Shared ReadOnly GuidMyExtensionsPage As Guid = New Guid(&HF24459FCUI, &HE883US, &H4A8EUS, CByte(&H9D), CByte(&HA2), CByte(&HAE), CByte(&HF6), CByte(&H84), CByte(&HF0), CByte(&HE1), CByte(&HF4))
            Friend Shared ReadOnly GuidOfficePublishPage As Guid = New Guid(&HCC7369A8UI, &HB9B0US, &H439CUS, CByte(&HB1), CByte(&H36), CByte(&HBA), CByte(&H95), CByte(&H58), CByte(&H19), CByte(&HF7), CByte(&HF8))
            Friend Shared ReadOnly GuidServicesPage As Guid = New Guid(&H43E38D2EUI, &H43B8US, &H4204US, CByte(&H82), CByte(&H25), CByte(&H93), CByte(&H57), CByte(&H31), CByte(&H61), CByte(&H37), CByte(&HA4))
            Friend Shared ReadOnly GuidWAPWebPage As Guid = New Guid(&H909D16B3UI, &HC8E8US, &H43D1US, CByte(&HA2), CByte(&HB8), CByte(&H26), CByte(&HEA), CByte(&HD), CByte(&H4B), CByte(&H6B), CByte(&H57))
        End Class

        ''' <summary>
        ''' test if vs in build process BUGFIX: Dev11#45255 
        ''' </summary>
        Public Function IsInBuildProgress() As Boolean
            Dim pfActive As Integer = 0
            Dim pdwCmdUICookie As UInteger
            Dim srpSelection As IVsMonitorSelection
            srpSelection = CType(VBPackage.Instance.GetService(GetType(IVsMonitorSelection)), IVsMonitorSelection)
            If srpSelection IsNot Nothing Then
                Dim hr As Integer = srpSelection.GetCmdUIContextCookie(VSConstants.UICONTEXT_SolutionBuilding, pdwCmdUICookie)
                If NativeMethods.Succeeded(hr) Then
                    hr = srpSelection.IsCmdUIContextActive(pdwCmdUICookie, pfActive)
                    If NativeMethods.Succeeded(hr) Then
                        If pfActive > 0 Then
                            Return True
                        Else
                            Return False
                        End If
                    End If
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Convert a variant to integer value
        ''' </summary>
        ''' <param name="obj"></param>
        ''' <param name="value"></param>
        ''' <return> return true if the variant is an integer type value</return>
        Public Function TryConvertVariantToInt(obj As Object, ByRef value As Integer) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            Dim objType As Type = obj.GetType()
            If objType Is GetType(UShort) OrElse
                objType Is GetType(Short) OrElse
                objType Is GetType(UInteger) OrElse
                objType Is GetType(Integer) OrElse
                objType Is GetType(ULong) OrElse
                objType Is GetType(Long) OrElse
                objType Is GetType(Byte) OrElse
                objType Is GetType(SByte) OrElse
                objType Is GetType(Single) OrElse
                objType Is GetType(Double) Then

                value = CInt(obj)
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Helper to convert ItemIds or other 32 bit ID values
        ''' where it is sometimes treated as an Int32 and sometimes UInt32
        ''' ItemId is sometimes marshaled as a VT_INT_PTR, and often declared 
        ''' UInt in the interop assemblies. Otherwise we get overflow exceptions converting 
        ''' negative numbers to UInt32.  We just want raw bit translation.
        ''' </summary>
        ''' <param name="obj"></param>
        Public Function NoOverflowCUInt(obj As Object) As UInteger
            Return NoOverflowCUInt(CLng(obj))
        End Function

        ''' <summary>
        ''' Masks the top 32 bits to get just the lower 32bit number
        ''' </summary>
        ''' <param name="LongValue"></param>
        Public Function NoOverflowCUInt(LongValue As Long) As UInteger
            Return CUInt(LongValue And UInteger.MaxValue)
        End Function

        ''' <summary>
        ''' Retrieves a given bitmap from the manifest resources (unmodified)
        ''' </summary>
        ''' <param name="BitmapID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <returns>The retrieved bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestBitmap(BitmapID As String) As Bitmap
            Return DirectCast(GetManifestImage(BitmapID), Bitmap)
        End Function

        ''' <summary>
        ''' Retrieves a transparent copy of a given bitmap from the manifest resources.
        ''' </summary>
        ''' <param name="BitmapID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <param name="TransparentColor">The color that represents transparent in the bitmap</param>
        ''' <returns>The retrieved transparent bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestBitmapTransparent(BitmapID As String, TransparentColor As Color) As Bitmap
            Dim Bitmap As Bitmap = GetManifestBitmap(BitmapID)
            If Bitmap IsNot Nothing Then
                Bitmap.MakeTransparent(TransparentColor)
                Return Bitmap
            Else
                Debug.Fail("Couldn't find internal resource")
                Throw New Package.InternalException(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_Unexpected_NoResource_1Arg, BitmapID))
            End If
        End Function

        ''' <summary>
        ''' Retrieves a transparent copy of a given bitmap from the manifest resources.
        ''' </summary>
        ''' <param name="BitmapID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <returns>The retrieved transparent bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestBitmapTransparent(BitmapID As String) As Bitmap
            Return GetManifestBitmapTransparent(BitmapID, StandardTransparentColor)
        End Function

        ''' <summary>
        ''' Retrieves a given image from the manifest resources.
        ''' </summary>
        ''' <param name="ImageID">Name of the bitmap resource (not including the assembly name, e.g. "Link.bmp")</param>
        ''' <returns>The retrieved bitmap</returns>
        ''' <remarks>Throws an internal exception if the bitmap cannot be found or loaded.</remarks>
        Public Function GetManifestImage(ImageID As String) As Image
            Dim BitmapStream As Stream = GetType(Utils).Assembly.GetManifestResourceStream(ImageID)
            If BitmapStream IsNot Nothing Then
                Dim Image As Image = Image.FromStream(BitmapStream)
                If Image IsNot Nothing Then
                    Return Image
                Else
                    Debug.Fail("Unable to find image resource from manifest: " & ImageID)
                End If
            Else
                Debug.Fail("Unable to find image resource from manifest: " & ImageID)
            End If

            Throw New Package.InternalException(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_Unexpected_NoResource_1Arg, ImageID))
        End Function

        Public Function GetImageFromImageService(imageMoniker As ImageMoniker, width As Integer, height As Integer, background As Color) As Image
            If ImageService IsNot Nothing Then
                Dim attributes As New Imaging.Interop.ImageAttributes With {
                    .StructSize = Marshal.SizeOf(GetType(Imaging.Interop.ImageAttributes)),
                    .ImageType = CType(_UIImageType.IT_Bitmap, UInteger),
                    .Format = CType(_UIDataFormat.DF_WinForms, UInteger),
                    .LogicalWidth = width,
                    .LogicalHeight = height
                }

                Dim backgroundValue As UInteger = ConvertColorToUInteger(background)
                attributes.Background = backgroundValue

                Dim flags As _ImageAttributesFlags = _ImageAttributesFlags.IAF_RequiredFlags Or _ImageAttributesFlags.IAF_Background

                ' The VB Compiler won't let you convert _ImageAttributesFlags.IAF_Background directly to UInteger, since it is negative
                ' Therefore, we convert to bytes and then reconvert to UInteger
                attributes.Flags = BitConverter.ToUInt32(BitConverter.GetBytes(flags), 0)

                Dim uIOjb As IVsUIObject
                uIOjb = ImageService.GetImage(imageMoniker, attributes)
                If uIOjb IsNot Nothing Then
                    Return CType(GelUtilities.GetObjectData(uIOjb), Bitmap)
                End If
            End If

            Return Nothing
        End Function

        Private Function ConvertColorToUInteger(color As Color) As UInteger
            Return CType(color.A, UInteger) << 24 Or CType(color.R, UInteger) << 16 Or CType(color.G, UInteger) << 8 Or CType(color.B, UInteger)
        End Function

        Private ReadOnly Property ImageService As IVsImageService2
            Get
                If s_imageService Is Nothing Then
                    Dim serviceProvider As ServiceProvider
                    serviceProvider = ServiceProvider.GlobalProvider
                    s_imageService = CType(serviceProvider.GetService(GetType(SVsImageService)), IVsImageService2)
                End If
                Return s_imageService
            End Get
        End Property

        ''' <summary>
        ''' Logical implies.  Often useful in Debug.Assert's.  Essentially, it is to be
        '''   read as "a being true implies that b is true".  Therefore, the function returns
        '''  False if a is true and b is false.  Otherwise it returns True (as there's no
        '''   evidence to suggest that the implication is incorrect).
        ''' </summary>
        Public Function Implies(a As Boolean, b As Boolean) As Boolean
            Return Not (a And Not b)
        End Function

        ''' <summary>
        ''' Retrieves the error message from an exception in a manner appropriate for the build.  For release, simply
        '''   retrieves ex.Message (just the message, no call stack).  For debug builds, appends the callstack and
        '''   also the inner exception, if any.
        ''' </summary>
        ''' <param name="ex"></param>
        Public Function DebugMessageFromException(ex As Exception) As String
#If DEBUG Then
            Dim ErrorMessage As String = ex.Message & vbCrLf & vbCrLf & vbCrLf & "[SHOWN IN DEBUG ONLY] STACK TRACE:" & vbCrLf & ex.StackTrace
            If ex.InnerException IsNot Nothing Then
                ErrorMessage &= vbCrLf & vbCrLf & "INNER EXCEPTION: " & vbCrLf & vbCrLf & ex.InnerException.ToString()
            End If

            Return ErrorMessage
#Else
            Return ex.Message
#End If
        End Function

        ''' <summary>
        ''' Attempts to create a string representation of an object, for debug purposes.  Under retail,
        '''   returns an empty string.
        ''' </summary>
        ''' <param name="Value">The value to turn into a displayable string.</param>
        Public Function DebugToString(Value As Object) As String
#If DEBUG Then
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
                Return "[" & ex.GetType.Name & "]"
            End Try
#Else
            Return ""
#End If
        End Function

        ''' <summary>
        ''' Logs the given exception and returns True so it can be used in an exception handler.
        ''' </summary>
        ''' <param name="ex">The exception to log.</param>
        ''' <param name="exceptionEventDescription">Additional description for the cause of the exception.</param>
        ''' <param name="throwingComponentName">Name of the component that threw that exception, generally the containing type name.</param>
        Public Function ReportWithoutCrash(ex As Exception,
                                           exceptionEventDescription As String,
                                           Optional throwingComponentName As String = "general") As Boolean
            Debug.Assert(ex IsNot Nothing)
            Debug.Assert(Not String.IsNullOrEmpty(throwingComponentName))
            Debug.Assert(Not String.IsNullOrEmpty(exceptionEventDescription))

            If IsCheckoutCanceledException(ex) Then Return True

            ' Follow naming convention for entity name: A string to identify the entity in the feature. E.g. open-project, build-project, fix-error.
            throwingComponentName = Regex.Replace(throwingComponentName, "([A-Z])", "-$1").TrimPrefix("-").ToLower() + "-fault"

            TelemetryService.DefaultSession.PostFault(
                eventName:="vs/ml/proppages/editors/" & throwingComponentName.ToLower,
                description:=exceptionEventDescription,
                exceptionObject:=ex)

            Debug.Fail(exceptionEventDescription & vbCrLf & $"Exception: {ex}")
            Return True
        End Function

        Public Function IsIOException(ex As Exception) As Boolean
            Return TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
        End Function

        ''' <summary>
        ''' Given an exception, returns True if it is a CheckOut exception.
        ''' </summary>
        ''' <param name="ex">The exception to check rethrow if it's caused by canceling checkout</param>
        Public Function IsCheckoutCanceledException(ex As Exception) As Boolean
            If (TypeOf ex Is CheckoutException AndAlso ex.Equals(CheckoutException.Canceled)) _
                OrElse
                (TypeOf ex Is COMException AndAlso DirectCast(ex, COMException).ErrorCode = Win32Constant.OLE_E_PROMPTSAVECANCELLED) _
            Then
                Return True
            End If

            If ex.InnerException IsNot Nothing Then
                Return IsCheckoutCanceledException(ex.InnerException)
            End If

            Return False
        End Function

        ''' <summary>
        ''' If the given string is Nothing, return "", else return the original string.
        ''' </summary>
        ''' <param name="Str"></param>
        Public Function NothingToEmptyString(Str As String) As String
            If Str Is Nothing Then
                Return String.Empty
            Else
                Return Str
            End If
        End Function

        ''' <summary>
        ''' If the given string is "", return Nothing, else return the original string.
        ''' </summary>
        ''' <param name="Str"></param>
        Public Function EmptyStringToNothing(Str As String) As String
            If Str Is Nothing OrElse Str.Length = 0 Then
                Return Nothing
            Else
                Return Str
            End If
        End Function

        ''' <summary>
        ''' Does the same as System.IO.Path.GetFullPath(), except that if the filename
        '''   is malformed, it returns the original file path.
        ''' </summary>
        ''' <param name="Path">The path to get the full path from.</param>
        Public Function GetFullPathTolerant(Path As String) As String
            Try
                Return IO.Path.GetFullPath(Path)
            Catch ex As ArgumentException
            Catch ex As NotSupportedException
            End Try

            'If we hit an exception we want to be tolerant of, just return the original path
            Return Path
        End Function

        ''' <summary>
        ''' Given two namespace (either or both of which might be empty), combine them together into a single
        '''   namespace.
        ''' </summary>
        ''' <param name="Namespace1">First namespace or name.  May be Nothing.</param>
        ''' <param name="Namespace2">Second namespace or name.  May be Nothing.</param>
        ''' <remarks>
        ''' Do not use this function to add a namespace onto a class (if the class name is empty, it will
        '''   return just the root namespace).  Instead, use AddNamespace for that.
        ''' Handles values of Nothing, never returns Nothing.
        ''' </remarks>
        Public Function CombineNamespaces(Namespace1 As String, Namespace2 As String) As String
            If Namespace1 = "" Then
                Return NothingToEmptyString(Namespace2)
            End If

            If Namespace2 = "" Then
                Return Namespace1
            End If

            Return Namespace1 & "." & Namespace2
        End Function

        ''' <summary>
        ''' Given a class name, prepends the given namespace, if any.  If the class given is empty,
        '''   returns empty.
        ''' </summary>
        ''' <param name="ClassName">The class name to prepend the namespace to.</param>
        ''' <remarks>Handles values of Nothing, never returns Nothing.</remarks>
        Public Function AddNamespace([Namespace] As String, ClassName As String) As String
            If ClassName = "" Then
                'If class name is missing, then namespace + class name must also be missing.
                Return ""
            ElseIf [Namespace] <> "" Then
                Return [Namespace] & "." & ClassName
            Else
                Return NothingToEmptyString(ClassName)
            End If
        End Function

        ''' <summary>
        ''' Given a fully-qualified namespace, and a root namespace, removes the root namespace
        '''   from the fully-qualified namespace, if it exists at the beginning of the string.
        '''   "Global" is not handled.
        ''' </summary>
        ''' <param name="FullyQualifiedNamespace">The fully-qualified namespace to remove the namespace from</param>
        ''' <param name="RootNamespace">The current root namespace to remove if it exists.</param>
        ''' <returns>FullyQualifiedNamespace without the root namespace.</returns>
        Public Function RemoveRootNamespace(FullyQualifiedNamespace As String, RootNamespace As String) As String
            Dim RootNamespaceLength As Integer = 0

            If RootNamespace Is Nothing Then
                RootNamespace = ""
            End If
            If FullyQualifiedNamespace Is Nothing Then
                FullyQualifiedNamespace = ""
            End If

            If RootNamespace <> "" Then
                'Append period for comparison check
                RootNamespace &= "."
                RootNamespaceLength = RootNamespace.Length
            End If

            If RootNamespaceLength > 0 AndAlso FullyQualifiedNamespace.Length > RootNamespaceLength Then
                If String.Compare(RootNamespace, 0, FullyQualifiedNamespace, 0, RootNamespaceLength, StringComparison.OrdinalIgnoreCase) = 0 Then
                    'Now remove the namespace that matched
                    Return FullyQualifiedNamespace.Substring(RootNamespaceLength)
                End If
            End If

            Return FullyQualifiedNamespace
        End Function

        ''' <summary>
        ''' Given a single filter text and a set of extensions, creates a single filter entry
        '''   for a file dialog.
        ''' </summary>
        ''' <param name="FilterText">The localized text for that filter entry, e.g. "Metafiles"</param>
        ''' <param name="Extensions">An array of extensions supported, e.g. {".wmf", ".emf").  Note that it does *not* include the '*', but it include or exclude the dot.</param>
        ''' <returns>A filter that combines the elements.  For example,
        ''' 
        '''     CreateDialogFilter("Metafiles", ".wmf", ".emf") 
        ''' 
        '''       returns the string 
        ''' 
        '''       "Metafiles (*.wmf, *.emf)|*.wmf;*.emf"
        ''' 
        ''' </returns>
        Public Function CreateDialogFilter(FilterText As String, ParamArray Extensions() As String) As String
            Dim Filter As New StringBuilder
            Dim i As Integer

            Debug.Assert(InStr(FilterText, "|") = 0, "FilterText for CreateDialogFilter should not contain '|'")

            'Build the user-friendly portion of the filter
            Filter.Append(FilterText & " (")
            For i = 0 To Extensions.Length - 1
                If i <> 0 Then
                    Filter.Append(", ")
                End If

                Dim Extension As String = Extensions(i)
                Debug.Assert(Left(Extension, 1) <> "*", "Extension should not include the '*'")
                If Left(Extension, 1) <> "." Then
                    Extension = "." & Extension
                End If
                Filter.Append("*" & Extension)
            Next

            'Build the programmatic portion
            Filter.Append(")|")
            For i = 0 To Extensions.Length - 1
                If i <> 0 Then
                    Filter.Append(";"c)
                End If

                Dim Extension As String = Extensions(i)
                If Left(Extension, 1) <> "." Then
                    Extension = "." & Extension
                End If
                Filter.Append("*" & Extension)
            Next

            Return Filter.ToString()
        End Function

        ''' <summary>
        ''' Returns a localized "All Files (*.*)|*.*" dialog filter string.  Can be used with CombineDialogFilters.
        ''' </summary>
        Public Function GetAllFilesDialogFilter() As String
            'We don't use CreateDialogFilter because we don't want *.* to be part of the user-friendly portion.
            '  We only want:  All Files|*.*
            Return My.Resources.Microsoft_VisualStudio_Editors_Designer.CMN_AllFilesFilter & "|*.*"
        End Function

        ''' <summary>
        ''' Give a set of filter lines for file dialogs (e.g., by using CreateSingleDialogFilter), combines them all into a single filter.
        ''' </summary>
        ''' <param name="Filters">The individual filter entries</param>
        Public Function CombineDialogFilters(ParamArray Filters() As String) As String
            Dim CombinedFilter As New StringBuilder

            For Each Filter As String In Filters
                If Filter <> "" Then
                    If CombinedFilter.Length <> 0 Then
                        CombinedFilter.Append("|"c)
                    End If
                    CombinedFilter.Append(Filter)
                End If
            Next

            Return CombinedFilter.ToString()
        End Function

        ''' <summary>
        ''' A better IIf
        ''' </summary>
        ''' <param name="Condition">The condition to test.</param>
        ''' <param name="TrueExpression">What to return if the condition is True</param>
        ''' <param name="FalseExpression">What to return if the condition is False</param>
        Public Function IIf(Of T)(Condition As Boolean, TrueExpression As T, FalseExpression As T) As T
            If Condition Then
                Return TrueExpression
            Else
                Return FalseExpression
            End If
        End Function

        ''' <summary>
        ''' Set the drop-down width of a combobox wide enough to show the text of all entries in it
        ''' </summary>
        ''' <param name="ComboBox">The combobox to change the width for</param>
        Public Sub SetComboBoxDropdownWidth(ComboBox As ComboBox)
            If ComboBox IsNot Nothing Then
                ComboBox.DropDownWidth = Math.Max(MeasureMaxTextWidth(ComboBox, ComboBox.Items), ComboBox.Width)
            Else
                Debug.Fail("SetComboBoxDropdownWidth: No combobox specified")
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
        Public Sub SetComboBoxColumnDropdownWidth(column As DataGridViewComboBoxColumn)
            If column IsNot Nothing AndAlso column.DataGridView IsNot Nothing Then
                column.DropDownWidth = Math.Max(MeasureMaxTextWidth(column.DataGridView, column.Items) + SystemInformation.VerticalScrollBarWidth, column.Width)
            Else
                Debug.Fail("SetComboBoxColumnDropdownWidth: No combobox column specified, or the column didn't have a parent datagridview!")
            End If
        End Sub

        ''' <summary>
        ''' Helper method to measure the maximum width of a collection of strings given a particular font...
        ''' </summary>
        ''' <param name="ctrl"></param>
        ''' <param name="items"></param>
        Public Function MeasureMaxTextWidth(ctrl As Control, items As IEnumerable) As Integer
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

        ''' <summary>
        ''' Returns a given path with a backslash at the end, if not already there.
        ''' </summary>
        ''' <param name="Path">The path to add a backslash to.</param>
        Friend Function AppendBackslash(Path As String) As String
            If Path <> "" AndAlso Right(Path, 1) <> IO.Path.DirectorySeparatorChar AndAlso Right(Path, 1) <> IO.Path.AltDirectorySeparatorChar Then
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
        Friend Function GetFilesViaBrowse(ServiceProvider As IServiceProvider, ParentWindow As IntPtr,
                InitialDirectory As String, DialogTitle As String,
                Filter As String, FilterIndex As UInteger, MutiSelect As Boolean,
                Optional DefaultFileName As String = Nothing,
                Optional NeedThrowError As Boolean = False) As ArrayList

            Dim uishell As IVsUIShell =
                CType(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)

            Dim fileNames As New ArrayList()

            InitialDirectory = NormalizeInitialDirectory(InitialDirectory)
            If InitialDirectory = "" Then
                InitialDirectory = Nothing
            End If

            Filter = GetNativeFilter(Filter)

            Dim MaxPathName As Integer = Win32Constant.MAX_PATH + 1
            If MutiSelect Then
                MaxPathName = (Win32Constant.MAX_PATH + 1) * VSDPLMAXFILES
            End If

            Dim vsOpenFileName As VSOPENFILENAMEW()

            Dim defaultName(MaxPathName) As Char
            If DefaultFileName IsNot Nothing Then
                DefaultFileName.CopyTo(0, defaultName, 0, DefaultFileName.Length)
            End If

            Dim stringMemPtr As IntPtr = Marshal.AllocHGlobal(MaxPathName * 2 + 2)
            Marshal.Copy(defaultName, 0, stringMemPtr, defaultName.Length)

            Try
                vsOpenFileName = New VSOPENFILENAMEW(0) {}
                vsOpenFileName(0).lStructSize = CUInt(Marshal.SizeOf(vsOpenFileName(0)))
                vsOpenFileName(0).hwndOwner = ParentWindow
                vsOpenFileName(0).pwzDlgTitle = DialogTitle
                vsOpenFileName(0).nMaxFileName = CUInt(MaxPathName)
                vsOpenFileName(0).pwzFileName = stringMemPtr
                vsOpenFileName(0).pwzInitialDir = InitialDirectory
                vsOpenFileName(0).pwzFilter = Filter
                vsOpenFileName(0).nFilterIndex = FilterIndex
                vsOpenFileName(0).nFileOffset = 0
                vsOpenFileName(0).nFileExtension = 0
                vsOpenFileName(0).dwHelpTopic = 0

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
                    If hr = Win32Constant.OLE_E_PROMPTSAVECANCELLED Then
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

        ''' <summary>
        ''' Change the InitialDirectory path to the format we can use in IVsUIShell function
        ''' </summary>
        ''' <param name="InitialDirectory">The initial directory for the dialog.  Can be Nothing or empty.</param>
        ''' <returns>a directory path</returns>
        Private Function NormalizeInitialDirectory(InitialDirectory As String) As String
            If InitialDirectory IsNot Nothing Then
                InitialDirectory = Trim(InitialDirectory)
                If InitialDirectory = "" Then
                    InitialDirectory = String.Empty
                Else
                    Try
                        'Path needs a backslash at the end, or it will be interpreted as a directory + filename
                        InitialDirectory = Path.GetFullPath(AppendBackslash(InitialDirectory))
                    Catch ex As Exception
                        InitialDirectory = String.Empty
                    End Try
                End If
            Else
                InitialDirectory = String.Empty
            End If
            Return InitialDirectory
        End Function

        ''' <summary>
        ''' Change the Filter String to the format we can use in IVsUIShell function
        ''' </summary>
        ''' <param name="Filter">file type filter</param>
        ''' <returns>a native filter string</returns>
        Private Function GetNativeFilter(Filter As String) As String
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

        ''' <summary>
        ''' Browses to save a File.
        ''' </summary>
        ''' <param name="ServiceProvider">Service Provider</param>
        ''' <param name="ParentWindow">Window Handle of the parent window</param>
        ''' <param name="InitialDirectory">The initial directory for the dialog.  Can be Nothing or empty.</param>
        ''' <param name="DialogTitle">The dialog title.</param>
        ''' <param name="Filter">The filter string to use.</param>
        ''' <param name="FilterIndex">The filter index to start out with.</param>
        ''' <param name="DefaultFileName">The default file name.</param>
        ''' <param name="OverwritePrompt">If true, Windows will ask the user to overwrite the file if it already exists.</param>
        ''' <returns>The selected file/path, or Nothing if the user canceled.</returns>
        Friend Function GetNewFileNameViaBrowse(ServiceProvider As IServiceProvider, ParentWindow As IntPtr,
                InitialDirectory As String, DialogTitle As String,
                Filter As String, FilterIndex As UInteger,
                Optional DefaultFileName As String = Nothing,
                Optional OverwritePrompt As Boolean = False) As String

            Dim uishell As IVsUIShell =
                CType(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell)

            InitialDirectory = NormalizeInitialDirectory(InitialDirectory)
            Filter = GetNativeFilter(Filter)

            Const MAX_PATH_NAME As Integer = 4096

            Dim defaultName(MAX_PATH_NAME) As Char
            If DefaultFileName IsNot Nothing Then
                DefaultFileName.CopyTo(0, defaultName, 0, DefaultFileName.Length)
            End If

            Dim vsSaveFileName As VSSAVEFILENAMEW()
            Dim stringMemPtr As IntPtr = Marshal.AllocHGlobal(MAX_PATH_NAME * 2 + 2)
            Marshal.Copy(defaultName, 0, stringMemPtr, defaultName.Length)

            Try
                vsSaveFileName = New VSSAVEFILENAMEW(0) {}
                vsSaveFileName(0).lStructSize = CUInt(Marshal.SizeOf(vsSaveFileName(0)))
                vsSaveFileName(0).hwndOwner = ParentWindow
                vsSaveFileName(0).pwzDlgTitle = DialogTitle
                vsSaveFileName(0).nMaxFileName = MAX_PATH_NAME
                vsSaveFileName(0).pwzFileName = stringMemPtr
                vsSaveFileName(0).pwzInitialDir = InitialDirectory
                vsSaveFileName(0).pwzFilter = Filter
                vsSaveFileName(0).nFilterIndex = FilterIndex
                vsSaveFileName(0).nFileOffset = 0
                vsSaveFileName(0).nFileExtension = FilterIndex
                vsSaveFileName(0).dwHelpTopic = 0
                vsSaveFileName(0).pSaveOpts = Nothing

                If OverwritePrompt Then
                    vsSaveFileName(0).dwFlags = &H2   'OFN_OVERWRITEPROMPT
                Else
                    vsSaveFileName(0).dwFlags = 0
                End If

                Dim hr As Integer = uishell.GetSaveFileNameViaDlg(vsSaveFileName)
                If VSErrorHandler.Succeeded(hr) Then
                    Dim sFileName As String = Marshal.PtrToStringUni(stringMemPtr)
                    Return sFileName
                End If

            Finally
                Marshal.FreeHGlobal(stringMemPtr)
            End Try

            Return Nothing
        End Function

        ''' <summary>
        '''  Returns a relative path from a given directory to another file or directory.
        ''' </summary>
        ''' <param name="baseDirectory">The path to start from, always assumed to be a directory.</param>
        ''' <param name="path">The path to get to.</param>
        ''' <returns>A String contains the relative path, '.' if baseDirectory and path are the same, 
        '''          or the given path if there is no relative path.</returns>
        ''' <exception cref="Path.GetFullPath">See IO.Path.GetFullPath: If the input path is invalid.</exception>
        ''' <remarks>
        '''  This works with UNC path. However, mapped drive will not be resolved to UNC path.
        '''  If baseDirectory / path exist, 8.3 paths will be resolved into long path format.
        '''  Samples:    GetRelativePath("C:\Foo\Foo1\Foo2\", "C:\Foo\Foo3\Foo4\") = "..\..\Foo3\Foo4"
        '''              GetRelativePath("C:\Foo\Foo1\Foo2", "C:\Foo\Foo3\Foo4") = "..\..\Foo3\Foo4"
        '''              GetRelativePath("C:\Foo\Foo1\Foo2", "D:\Bar1\Bar2") = "D:\Bar1\Bar2"
        ''' </remarks>
        Friend Function GetRelativePath(BaseDirectory As String, Path As String) As String
            ' Algorithm adapted from URI.MakeRelative (sources\ndp\fx\src\Net\System\Uri.cs).
            ' 1. Start from beginning, compare each character.
            '   -   If hit the end of a path or has 2 different characters, break.
            '   -   If both paths have a separator at the same index, update the Common Separator Index (CSI).
            ' 2. After the loop, if index is at the end of both part, return ".".
            ' 3. Otherwise, 
            '   -   Count the number of separators (if any) from the current index on base path, 
            '       append the same number of "..\" to result.
            '   -   Append the target path's sub string starting from the (CSI) to result.
            ' NOTE: We will append a separator ('\') to the end of each path to deal with the case
            '   "C:\Dir1\Dir2\Dir3\Dir4" and "C:\Dir1\Dir2" (or vice versa) correctly. (VSWhidbey 68955).

            BaseDirectory = NormalizePath(BaseDirectory)
            Path = NormalizePath(Path)

            ' If the 2 paths have different root paths, return Path. 
            ' It's harder to deal with UNC root path in the algorithm.
            If Not String.Equals(IO.Path.GetPathRoot(BaseDirectory), IO.Path.GetPathRoot(Path),
                    StringComparison.OrdinalIgnoreCase) Then
                Return RemoveEndingSeparator(Path)
            End If

            ' Use the algorithm from URI.MakeRelative.
            Dim Index As Integer = 0
            Dim CommonSeparatorPosition As Integer = -1
            ' Loop until the end of a path, or different characters at an index.
            While (Index < BaseDirectory.Length) And (Index < Path.Length)
                If Char.ToUpperInvariant(BaseDirectory.Chars(Index)) <> Char.ToUpperInvariant(Path.Chars(Index)) Then
                    Exit While
                Else
                    ' Update CommonSeparatorPosition if both paths have a separator at an index.
                    If BaseDirectory.Chars(Index) = IO.Path.DirectorySeparatorChar Then
                        CommonSeparatorPosition = Index
                    End If
                End If
                Index += 1
            End While

            ' Index at the end of 2 paths, they are the same, return '.'
            If Index = BaseDirectory.Length And Index = Path.Length Then
                Return "."
            End If

            ' Otherwise, build the result.
            Dim RelativePath As New StringBuilder

            ' Calculate how many directories to go up to common directory from base path.
            While Index < BaseDirectory.Length
                If BaseDirectory.Chars(Index) = IO.Path.DirectorySeparatorChar Then
                    RelativePath.Append(".." & IO.Path.DirectorySeparatorChar)
                End If
                Index += 1
            End While

            ' Append the target path from common directory forward. 
            If CommonSeparatorPosition < Path.Length Then
                RelativePath.Append(Path.Substring(CommonSeparatorPosition + 1))
            End If

            Return RemoveEndingSeparator(RelativePath.ToString)
        End Function

        ''' <summary>
        ''' Get a full, long-format path ending with a separator for GetRelativePath.
        ''' </summary>
        Private Function NormalizePath(Path As String) As String
            ' Get the full path.
            Path = IO.Path.GetFullPath(Path)

            ' Now we have the long-format path, remove all ending separators, then add one for GetRelativePath to work correctly.
            Return Path.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar) & IO.Path.DirectorySeparatorChar
        End Function

        ''' <summary>
        ''' Check whether the give path is a root path or not.
        ''' </summary>
        ''' <remarks>
        ''' Compare the given path with the result of IO.Path.GetPathRoot(Path).
        ''' IO.Path.GetPathRoot() will trim the ending separator, so trim the input as well.
        ''' </remarks>
        Private Function IsRootPath(Path As String) As Boolean
            If IO.Path.IsPathRooted(Path) Then
                Dim TrimmedPath As String = Path.TrimEnd(IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar)
                Return String.Equals(TrimmedPath, IO.Path.GetPathRoot(Path), StringComparison.OrdinalIgnoreCase)
            End If
            Return False
        End Function

        ''' <summary>
        ''' Remove the ending separators from a path if it's not a root path.
        ''' </summary>
        Private Function RemoveEndingSeparator(Path As String) As String
            If Not IsRootPath(Path) Then
                Return Path.TrimEnd(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar)
            Else
                Return Path
            End If
        End Function

        ''' <summary>
        ''' Check whether the screen reader is running
        ''' </summary>
        Friend Function IsScreenReaderRunning() As Boolean
            Dim pvParam As IntPtr = Marshal.AllocCoTaskMem(4)
            Try
                If NativeMethods.SystemParametersInfo(Win32Constant.SPI_GETSCREENREADER, 0, pvParam, 0) <> 0 Then
                    Dim result As Integer = Marshal.ReadInt32(pvParam)
                    Return result <> 0
                End If
            Finally
                Marshal.FreeCoTaskMem(pvParam)
            End Try
            Return False
        End Function

        '''<summary>
        ''' We use this function to map one color in the image to another color
        '''</summary>
        ''' <returns>a new image
        ''' </returns>
        Friend Function MapBitmapColor(unmappedBitmap As Image, originalColor As Color, newColor As Color) As Image
            Dim mappedBitmap As Bitmap

            Try
                mappedBitmap = New Bitmap(unmappedBitmap)

                Using g As Graphics = Graphics.FromImage(mappedBitmap)
                    Dim size As Size = unmappedBitmap.Size
                    Dim r As Rectangle = New Rectangle(New Point(0, 0), size)
                    Dim colorMaps As ColorMap() = New ColorMap(0) {}

                    colorMaps(0) = New ColorMap With {
                        .OldColor = originalColor,
                        .NewColor = newColor
                    }

                    Dim imageAttributes As Drawing.Imaging.ImageAttributes = New Drawing.Imaging.ImageAttributes()
                    imageAttributes.SetRemapTable(colorMaps, ColorAdjustType.Bitmap)

                    g.DrawImage(unmappedBitmap, r, 0, 0, size.Width, size.Height, GraphicsUnit.Pixel, imageAttributes)
                End Using
            Catch e As Exception When ReportWithoutCrash(e, NameOf(MapBitmapColor), NameOf(Utils))
                ' fall-back is to use the unmapped bitmap
                Return unmappedBitmap
            End Try

            Return mappedBitmap
        End Function

        ''' <summary>
        ''' Sets the checked state of the checkbox to a determinate checked or unchecked value
        ''' </summary>
        ''' <param name="CheckBox">The checkbox whose CheckedState should be set.</param>
        ''' <param name="Value">True to set the state to checked, False for unchecked.</param>
        ''' <remarks>
        ''' Just setting Checked to true or false may not get the expected results if the
        '''   checkbox state was previous indeterminate because the checkbox may still be
        '''   indeterminate.
        ''' </remarks>
        Public Sub SetCheckboxDeterminateState(CheckBox As CheckBox, Value As Boolean)
            CheckBox.CheckState = IIf(Value, CheckState.Checked, CheckState.Unchecked)
        End Sub

        ''' <summary>
        ''' Get the service provider associated with a hierarchy...
        ''' </summary>
        ''' <param name="pHier"></param>
        Public Function ServiceProviderFromHierarchy(pHier As IVsHierarchy) As ServiceProvider
            If pHier IsNot Nothing Then
                Dim OLEServiceProvider As OLE.Interop.IServiceProvider = Nothing
                VSErrorHandler.ThrowOnFailure(pHier.GetSite(OLEServiceProvider))
                Return New ServiceProvider(OLEServiceProvider)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Sets error code and error message through IVsUIShell interface
        ''' </summary>
        ''' <param name="hr">error code</param>
        ''' <param name="errorMessage">error message</param>
        Public Sub SetErrorInfo(sp As ServiceProvider, hr As Integer, errorMessage As String)
            Dim vsUIShell As IVsUIShell = Nothing

            If sp IsNot Nothing Then
                vsUIShell = CType(sp.GetService(GetType(IVsUIShell)), IVsUIShell)
            End If

            If vsUIShell Is Nothing AndAlso Not VBPackage.Instance IsNot Nothing Then
                vsUIShell = CType(VBPackage.Instance.GetService(GetType(IVsUIShell)), IVsUIShell)
            End If

            If vsUIShell IsNot Nothing Then
                vsUIShell.SetErrorInfo(hr, errorMessage, 0, Nothing, Nothing)
            Else
                Debug.Fail("Could not get IVsUIShell from service provider. Can't set specific error message.")
            End If
        End Sub

        ''' <summary>
        ''' Sets focus to the first (or last) control inside of a parent HWND.
        ''' </summary>
        ''' <param name="HwndParent">The container HWND.</param>
        ''' <param name="First">If True, sets focus to the first control, otherwise the last.</param>
        Public Function FocusFirstOrLastTabItem(HwndParent As IntPtr, First As Boolean) As Boolean
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
            Dim FirstTabStop As IntPtr = NativeMethods.GetNextDlgTabItem(HwndParent, IntPtr.Zero, False)
            If FirstTabStop.Equals(IntPtr.Zero) Then
                Return False
            End If

            Dim NextTabStop As IntPtr
            If First Then
                NextTabStop = FirstTabStop
            Else
                NextTabStop = NativeMethods.GetNextDlgTabItem(HwndParent, FirstTabStop, True)
            End If

            If NextTabStop.Equals(IntPtr.Zero) Then
                Return False
            End If

            NativeMethods.SetFocus(NextTabStop)
            Return True
        End Function

        ''' <summary>
        ''' Validates whether it is a high-surrogate character
        ''' </summary>
        ''' <param name="ch">a character to check</param>
        ''' <returns>True if it is a high-surrogate character</returns>
        Public Function IsHighSurrogate(ch As Char) As Boolean
            Return AscW(ch) >= &HD800 AndAlso AscW(ch) <= &HDBFF
        End Function

        ''' <summary>
        ''' Validates whether it is a low-surrogate character
        ''' </summary>
        ''' <param name="ch">a character to check</param>
        ''' <returns>True if it is a low-surrogate character</returns>
        Public Function IsLowSurrogate(ch As Char) As Boolean
            Return AscW(ch) >= &HDC00 AndAlso AscW(ch) <= &HDFFF
        End Function

        ''' <summary>
        ''' Get the namespace for the generated file...
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="IncludeRootNamespace"></param>
        ''' <param name="SupportCustomToolNamespace"></param>
        Friend Function GeneratedCodeNamespace(Hierarchy As IVsHierarchy, ItemId As UInteger, IncludeRootNamespace As Boolean, SupportCustomToolNamespace As Boolean) As String
            ' Try to get the root namespace property (if VB)
            Dim RootNamespace As String = ""
            If IsVbProject(Hierarchy) Then
                Dim Project As EnvDTE.Project = DTEUtils.EnvDTEProject(Hierarchy)
                If Project IsNot Nothing Then
                    Dim RootNamespaceProperty As EnvDTE.Property = Nothing
                    Try
                        RootNamespaceProperty = Project.Properties().Item("RootNamespace")
                    Catch ex As ArgumentException
                        ' ignore if the project does not support it
                    End Try
                    If RootNamespaceProperty IsNot Nothing Then
                        RootNamespace = TryCast(RootNamespaceProperty.Value, String)
                    End If
                End If
            End If

            Debug.Assert(RootNamespace = "" OrElse IsVbProject(Hierarchy), "Only VB projects should have a root namespace property!")

            Dim CustomToolNamespace As String = ""
            Dim objDefaultNamespace As Object = Nothing
            Dim DefaultNamespace As String = ""
            Try
                If ItemId = VSConstants.VSITEMID_ROOT Then
                    ' Support VSITEMID_ROOT, get the default namespace of the project
                    VSErrorHandler.ThrowOnFailure(Hierarchy.GetProperty(ItemId, __VSHPROPID.VSHPROPID_DefaultNamespace, objDefaultNamespace))
                    Dim value = TryCast(objDefaultNamespace, String)
                    If value IsNot Nothing Then
                        DefaultNamespace = DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(value)
                    End If
                Else
                    Dim item As EnvDTE.ProjectItem = DTEUtils.ProjectItemFromItemId(Hierarchy, ItemId)
                    Debug.Assert(item IsNot Nothing, "Failed to get EnvDTE.ProjectItem from given hierarchy/itemid")
                    If item IsNot Nothing AndAlso item.Properties IsNot Nothing Then
                        Dim prop As EnvDTE.Property = Nothing
                        If SupportCustomToolNamespace Then
                            Try
                                prop = item.Properties.Item("CustomToolNamespace")
                            Catch ex As ArgumentException
                                'ignore if the project doesn't support it
                            End Try
                        End If

                        If prop IsNot Nothing AndAlso CStr(prop.Value) <> "" Then
                            CustomToolNamespace = DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(CStr(prop.Value))
                        Else
                            VSErrorHandler.ThrowOnFailure(Hierarchy.GetProperty(ItemId, __VSHPROPID.VSHPROPID_DefaultNamespace, objDefaultNamespace))
                            Dim value = TryCast(objDefaultNamespace, String)
                            If value IsNot Nothing Then
                                DefaultNamespace = DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(value)
                            End If
                        End If
                    End If
                End If
            Catch ex As ArgumentException
                ' Venus throws when trying to access the CustomToolNamespace property...
            Catch ex As Exception When ReportWithoutCrash(ex, "Failed to get item.Properties('CustomToolNamespace')", NameOf(Utils))
            End Try

            ' If we have a custom tool namespace, then we will return this unless we also have a root namespace (VB only)
            ' and we are to include the root namespace in the returned value...
            If CustomToolNamespace <> "" Then
                If RootNamespace <> "" AndAlso IncludeRootNamespace Then
                    Return String.Concat(RootNamespace, ".", CustomToolNamespace)
                Else
                    Return CustomToolNamespace
                End If
            End If

            ' No custom tool namespace set - we need to get the default namespace
            Debug.Assert(CustomToolNamespace = "", "We should have used the CustomToolNamespace if set!")

            ' If we shouldn't include the root namespace, then we will strip it off from the default namespace...
            If Not IncludeRootNamespace Then
                ' If we are not supposed to include the root namespace in the returned namespace,
                ' we better check to see if we can find one, and if so, remove it from the start of the
                ' generated namespace....
                Try
                    If RootNamespace <> "" AndAlso DefaultNamespace.StartsWith(RootNamespace, StringComparison.Ordinal) Then
                        If DefaultNamespace.Length > RootNamespace.Length Then
                            ' If the generated namespace name is longer than the rootnamespace, then it 
                            ' should be <rootnamespace>.<rest.of.the.namespace>
                            ' In this case we gotta remove the rootnamespace and the "." between the 
                            ' root namespace and the rest...
                            DefaultNamespace = DefaultNamespace.Substring(RootNamespace.Length() + 1)
                        Else
                            ' If the length of the root namespace is equal to the length of the generated namespace
                            ' they've got to be equal (remember, we already checked that the generated namespace begins with
                            ' the root namespace, right!)
                            DefaultNamespace = ""
                        End If
                    End If
                Catch ex As ArgumentException
                Catch ex As Exception When ReportWithoutCrash(ex, "Exception when trying to get the root namespace", NameOf(Utils))
                End Try
            End If
            Try
                Return DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(DefaultNamespace)
            Catch ex As ArgumentException
                Return String.Empty
            End Try
        End Function

        ''' <summary>
        ''' This function check whether WCF Reference is valid.
        '''  Note: because of multi-target, a project doesn't support WCF reference might contains some reference already
        '''     added before the targetPlatform is changed from 3.0 to 2.0.
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        Private Function IsWCFReferenceValidInProject(Hierarchy As IVsHierarchy) As Boolean
            Requires.NotNull(Hierarchy, NameOf(Hierarchy))

            If TryCast(Hierarchy, WCFReference.Interop.IVsWCFMetadataStorageProvider) IsNot Nothing Then
                Dim objIsServiceReferenceSupported As Object = Nothing
                Try
                    VSErrorHandler.ThrowOnFailure(Hierarchy.GetProperty(VSITEMID.ROOT, CInt(__VSHPROPID3.VSHPROPID_ServiceReferenceSupported), objIsServiceReferenceSupported))
                    If objIsServiceReferenceSupported IsNot Nothing AndAlso TypeOf objIsServiceReferenceSupported Is Boolean Then
                        Return CType(objIsServiceReferenceSupported, Boolean)
                    End If
                Catch ex As NotImplementedException
                    Return False
                Catch ex As NotSupportedException
                End Try
                Return False
            End If
            Return False
        End Function

        Friend Function IsServiceReferenceValidInProject(Hierarchy As IVsHierarchy) As Boolean
            Return IsWCFReferenceValidInProject(Hierarchy)
        End Function

        ''' <summary>
        ''' This function check whether Web Reference is supported by default.
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        Friend Function IsWebReferenceSupportedByDefaultInProject(Hierarchy As IVsHierarchy) As Boolean
            Requires.NotNull(Hierarchy, NameOf(Hierarchy))

            Dim objIsReferenceSupported As Object = Nothing
            Try
                Dim hr = Hierarchy.GetProperty(VSITEMID.ROOT, CInt(__VSHPROPID3.VSHPROPID_WebReferenceSupported), objIsReferenceSupported)
                If hr <> VSConstants.DISP_E_MEMBERNOTFOUND AndAlso
                    hr <> VSConstants.E_NOTIMPL Then

                    VSErrorHandler.ThrowOnFailure(hr)
                    If objIsReferenceSupported IsNot Nothing AndAlso TypeOf objIsReferenceSupported Is Boolean Then
                        Return CType(objIsReferenceSupported, Boolean)
                    End If
                End If
            Catch ex As NotImplementedException
                Return True
            Catch ex As NotSupportedException
            End Try
            Return True
        End Function

        ''' <summary>
        ''' Is this a VB project?
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        Friend Function IsVbProject(Hierarchy As IVsHierarchy) As Boolean
            Requires.NotNull(Hierarchy, NameOf(Hierarchy))

            Dim langService As Guid = Guid.Empty
            Try
                VSErrorHandler.ThrowOnFailure(Hierarchy.GetGuidProperty(VSITEMID.ROOT, CInt(__VSHPROPID.VSHPROPID_PreferredLanguageSID), langService))
                If Not langService = Guid.Empty Then
                    Return langService.Equals(New Guid("{E34ACDC0-BAAE-11D0-88BF-00A0C9110049}"))
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(IsVbProject), NameOf(Utils))
            End Try
            Return False
        End Function

        ''' <summary>
        ''' Get Framework version number
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        Friend Function GetProjectTargetFrameworkVersion(Hierarchy As IVsHierarchy) As UInteger
            ' Get ".Net version" using TargetFrameworkMoniker property
            Dim objVersionNumber As Object = Nothing
            VSErrorHandler.ThrowOnFailure(Hierarchy.GetProperty(VSITEMID.ROOT, CInt(__VSHPROPID4.VSHPROPID_TargetFrameworkMoniker), objVersionNumber))
            If objVersionNumber IsNot Nothing AndAlso TypeOf objVersionNumber Is String Then
                ' Use "Framework class" to extract Version Number 
                Dim objFrameworkName As New FrameworkName(TryCast(objVersionNumber, String))
                If objFrameworkName IsNot Nothing Then
                    Dim versionNumber As Integer = objFrameworkName.Version.Major
                    versionNumber <<= 16
                    versionNumber += objFrameworkName.Version.Minor
                    Return CUInt(versionNumber)
                Else
                    Throw New ArgumentException("Invalid FrameworkName")
                End If
            Else
                Throw New ArgumentException("Invalid Project")
            End If
        End Function

        ''' <summary>
        ''' Is the current project targeting the Client subset?
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <returns>True is the current Framework Profile is Client</returns>
        Friend Function IsClientFrameworkSubset(Hierarchy As IVsHierarchy) As Boolean
            Debug.Assert(Hierarchy IsNot Nothing, "Hierarchy is required")
            Dim service As MultiTargetService = New MultiTargetService(Hierarchy, VSConstants.VSITEMID_ROOT, False)
            ' AuthenticationService is present only in server frameworks. We want to test for presence of this type 
            ' before enabling server-specific functionality
            Return Not service.IsSupportedType(GetType(Web.ApplicationServices.AuthenticationService))

        End Function

#Region "Wrapper that allows indirect calls into the static helpers in order to help unit test our code"

        Private s_instance As New Helper

        Friend ReadOnly Property Instance As Helper
            Get
                Return s_instance
            End Get
        End Property

        Friend Sub SetFakeHelper(fakeHelper As Helper)
            s_instance = fakeHelper
        End Sub

        ''' <summary>
        ''' The helper class is intended to contain all methods that we want to be able to "mock"
        ''' from our unit tests. By changing the m_instance field, we can take provide fake implementations
        ''' of functions that otherwise would require significant mocking...
        ''' </summary>
        Friend Class Helper

            Public Overridable Function ServiceProviderFromHierarchy(pHier As IVsHierarchy) As IServiceProvider ' Microsoft.VisualStudio.Shell.ServiceProvider
                Return Utils.ServiceProviderFromHierarchy(pHier)
            End Function

        End Class

#End Region

        ''' <summary>
        ''' Normalizes line endings.  For instance, it will expand \r and \n to
        '''   \r\n and reverse \n\r to \r\n
        ''' </summary>
        ''' <param name="text"></param>
        Public Function NormalizeLineEndings(text As String) As String
            If text = "" Then
                Return text
            End If

            Dim sb As New StringBuilder(text.Length)

            For i As Integer = 0 To text.Length - 1
                Select Case AscW(text.Chars(i))
                    Case 13 '\r
                        If i < text.Length - 1 AndAlso text.Chars(i + 1) = vbLf Then
                            'This one is okay, skip it
                            sb.Append(vbCrLf)
                            i += 1 'Skip next character
                            Continue For
                        Else
                            'This is an unmatched '\r', need to expand it
                            sb.Append(vbCrLf)
                            Continue For
                        End If

                    Case 10 '\n
                        If i < text.Length - 1 AndAlso text.Chars(i + 1) = vbCr Then
                            'This is backwards (\n\r), need to reverse it
                            sb.Append(vbCrLf)
                            i += 1 'Skip next character
                            Continue For
                        Else
                            'This is an unmatched '\n', need to expand it
                            sb.Append(vbCrLf)
                            Continue For
                        End If

                    Case Else
                        sb.Append(text.Chars(i))
                End Select
            Next

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Determines whether the project associated with the given hierarchy is targeting .NET 4.5 or above
        ''' </summary>
        ''' <param name="hierarchy">Hierarchy object.</param>
        ''' <returns>Value indicating whether the project associated with the given hierarchy is targeting .NET 4.5 or above.</returns>
        Friend Function IsTargetingDotNetFramework45OrAbove(hierarchy As IVsHierarchy) As Boolean

            Dim frameworkName As FrameworkName = Nothing
            If TryGetTargetFrameworkMoniker(hierarchy, frameworkName) Then
                ' Verify that we are targeting .NET
                If Not String.Equals(frameworkName.Identifier, ".NETFramework", StringComparison.OrdinalIgnoreCase) Then
                    Return False
                End If

                ' Verify that the version of the target framework is >= 4.5
                Return frameworkName.Version.Major > 4 OrElse
                   (frameworkName.Version.Major = 4 AndAlso frameworkName.Version.Minor >= 5)
            End If

            Return False

        End Function

        ''' <summary>
        ''' Determines whether the project associated with the given hierarchy is targeting .NET Framework
        ''' </summary>
        ''' <param name="hierarchy">Hierarchy object.</param>
        ''' <returns>Value indicating whether the project associated with the given hierarchy is targeting .NET Framework.</returns>
        Friend Function IsTargetingDotNetFramework(hierarchy As IVsHierarchy) As Boolean

            Dim frameworkName As FrameworkName = Nothing
            If TryGetTargetFrameworkMoniker(hierarchy, frameworkName) Then
                ' Verify that we are targeting .NET
                Return String.Equals(frameworkName.Identifier, ".NETFramework", StringComparison.OrdinalIgnoreCase)

            End If

            Return False

        End Function

        ''' <summary>
        ''' Determines whether the project associated with the given hierarchy is targeting .NET Core
        ''' </summary>
        ''' <param name="hierarchy">Hierarchy object.</param>
        ''' <returns>Value indicating whether the project associated with the given hierarchy is targeting .NET Core.</returns>
        Friend Function IsTargetingDotNetCore(hierarchy As IVsHierarchy) As Boolean

            Dim frameworkName As FrameworkName = Nothing
            If TryGetTargetFrameworkMoniker(hierarchy, frameworkName) Then
                Return IsTargetingDotNetCore(frameworkName.Identifier)
            Else
                Return False
            End If

        End Function

        ''' <summary>
        ''' Determines whether the specified framework is .NET Core
        ''' </summary>
        ''' <param name="frameworkIdentifier">Framework identifier.</param>
        ''' <returns>Value indicating whether the specified framework is .Net Core</returns>
        Friend Function IsTargetingDotNetCore(frameworkIdentifier As String) As Boolean

            Return String.Equals(frameworkIdentifier, ".NETCoreApp", StringComparison.OrdinalIgnoreCase)

        End Function

        ''' <summary>
        ''' Gets the TargetFrameworkMoniker property value for the project associated with the given hierarchy
        ''' </summary>
        ''' <param name="hierarchy">Hierarchy object.</param>
        ''' <returns>Value indicating whether the property was retrieved successfully</returns>
        Private Function TryGetTargetFrameworkMoniker(hierarchy As IVsHierarchy, ByRef frameworkName As FrameworkName) As Boolean

            Dim propertyValue As Object = Nothing

            If VSErrorHandler.Failed(hierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID4.VSHPROPID_TargetFrameworkMoniker, propertyValue)) Then
                Return False
            End If

            If propertyValue Is Nothing Then
                Return False
            End If

            If TypeOf propertyValue IsNot String Then
                Return False
            End If

            frameworkName = New FrameworkName(CStr(propertyValue))
            Return True
        End Function

        ''' <summary>
        ''' Determines whether the given hierarchy is an appcontainer project
        ''' </summary>
        ''' <param name="hierarchy"></param>
        Friend Function IsAppContainerProject(hierarchy As IVsHierarchy) As Boolean

            Dim propertyValue As Object = Nothing

            If VSErrorHandler.Failed(hierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID5.VSHPROPID_AppContainer, propertyValue)) Then
                Return False
            End If

            If propertyValue Is Nothing Then
                Return False
            End If

            If TypeOf propertyValue IsNot Boolean Then
                Return False
            End If

            Return CBool(propertyValue)

        End Function

        ''' <summary>
        ''' Returns true iff the given reference is one that the compiler has added automatically and
        ''' therefore cannot be removed, and also should not be displayed.  For VB, this is currently
        ''' mscorlib.dll and Microsoft.VisualBasic.dll.
        ''' </summary>
        ''' <param name="Reference"></param>
        Friend Function IsImplicitlyAddedReference(Reference As VSLangProj.Reference) As Boolean
            If Reference Is Nothing Then
                Debug.Fail("Reference shouldn't be Nothing")
                Return False
            End If

            Try
                Dim Reference3 As VSLangProj80.Reference3 = TryCast(Reference, VSLangProj80.Reference3)
                If Reference3 IsNot Nothing AndAlso Reference3.AutoReferenced Then
                    Return True
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, "Reference3.AutoReferenced threw an exception", NameOf(Utils))
            End Try

            Return False
        End Function

        Friend Function GetSecurityZoneOfFile(path As String, serviceProvider As ServiceProvider) As Security.SecurityZone
            Requires.NotNull(path, NameOf(path))

            If Not IO.Path.IsPathRooted(path) Then
                Throw CreateArgumentException(NameOf(path))
            End If

            ' Some additional verification is done by Path.GetFullPath...
            Dim absPath As String = IO.Path.GetFullPath(path)

            Dim internetSecurityManager As IInternetSecurityManager = Nothing

            ' We've got to get a fresh instance of the InternetSecurityManager, since it seems that the instance we
            ' can get from our ServiceProvider can't Map URLs to zones...
            Dim localReg As ILocalRegistry2 = TryCast(serviceProvider.GetService(GetType(ILocalRegistry)), ILocalRegistry2)
            If localReg IsNot Nothing Then
                Dim ObjectPtr As IntPtr = IntPtr.Zero
                Try
                    Static CLSID_InternetSecurityManager As New Guid("7b8a2d94-0ac9-11d1-896c-00c04fb6bfc4")
                    VSErrorHandler.ThrowOnFailure(localReg.CreateInstance(CLSID_InternetSecurityManager, Nothing, NativeMethods.IID_IUnknown, Win32Constant.CLSCTX_INPROC_SERVER, ObjectPtr))
                    internetSecurityManager = TryCast(Marshal.GetObjectForIUnknown(ObjectPtr), IInternetSecurityManager)
                Catch Ex As Exception When ReportWithoutCrash(Ex, "Failed to create Interop.IInternetSecurityManager", NameOf(Utils) & "." & NameOf(GetSecurityZoneOfFile))
                Finally
                    If ObjectPtr <> IntPtr.Zero Then
                        Marshal.Release(ObjectPtr)
                    End If
                End Try
            End If

            If internetSecurityManager Is Nothing Then
                Debug.Fail("Failed to create an InternetSecurityManager")
                Throw New ApplicationException
            End If

            Dim zone As Integer
            Dim hr As Integer = internetSecurityManager.MapUrlToZone(absPath, zone, 0)

            If VSErrorHandler.Failed(hr) Then
                ' If we can't map the absolute path to a zone, we silently fail...
                Return Security.SecurityZone.NoZone
            End If

            Return CType(zone, Security.SecurityZone)
        End Function

#Region "Telemetry"
        Public Class TelemetryLogger

            Private Const ProjectSystemEventNamePrefix As String = "vs/projectsystem/"
            Private Const EditorsEventNamePrefix As String = ProjectSystemEventNamePrefix + "editors/"

            Private Const ProjectSystemPropertyNamePrefix As String = "vs.projectsystem."
            Private Const EditorsPropertyNamePrefix As String = ProjectSystemPropertyNamePrefix + "editors."

            Private Const InputXmlFormEventName As String = EditorsEventNamePrefix + "inputxmlform"
            Public Enum InputXmlFormEvent
                FormOpened
                FromFileButtonClicked
                FromWebButtonClicked
                AsTextButtonClicked
            End Enum

            Public Shared Sub LogInputXmlFormEvent(eventValue As InputXmlFormEvent)
                Dim userTask = New UserTaskEvent(InputXmlFormEventName, TelemetryResult.Success)
                userTask.Properties(EditorsPropertyNamePrefix + "inputxmlform") = eventValue
                TelemetryService.DefaultSession.PostEvent(userTask)
            End Sub

            Public Shared Sub LogInputXmlFormException(ex As Exception)
                TelemetryService.DefaultSession.PostFault(InputXmlFormEventName, "Exception encountered during Xml Schema Inference", ex)
            End Sub

            Private Const AdvBuildSettingsPropPageEventName As String = ProjectSystemEventNamePrefix + "appdesigner/advbuildsettingsproppage"
            Public Enum AdvBuildSettingsPropPageEvent
                FormOpened = 0
                LangVersionMoreInfoLinkClicked = 1
            End Enum

            Public Shared Sub LogAdvBuildSettingsPropPageEvent(eventValue As AdvBuildSettingsPropPageEvent)
                Dim userTask = New UserTaskEvent(AdvBuildSettingsPropPageEventName, TelemetryResult.Success)
                userTask.Properties(ProjectSystemPropertyNamePrefix + "appdesigner.advbuildsettingsproppage") = eventValue
                TelemetryService.DefaultSession.PostEvent(userTask)
            End Sub

        End Class
#End Region

        Public Class ObjectSerializer

            ' KnownType information is used by DataContractSerializer for serialization of types that it may not know of currently.
            ' Size is used in Bitmap and has issues being recognized in DataContractSerializer for the unit tests of this class.
            Private Shared ReadOnly s_knownTypes As Type() = {GetType(Size)}

            Public Shared Sub Serialize(stream As Stream, value As Object)
                Requires.NotNull(stream, NameOf(stream))
                Requires.NotNull(value, NameOf(value))
                Using writer As New BinaryWriter(stream, Encoding.UTF8, leaveOpen:=True)
                    Dim valueType = value.GetType()
                    writer.Write(valueType.AssemblyQualifiedName)
                    writer.Flush()
                    Call New DataContractSerializer(valueType, s_knownTypes).WriteObject(stream, value)
                End Using
            End Sub

            Public Shared Function Deserialize(stream As Stream) As Object
                Requires.NotNull(stream, NameOf(stream))
                If stream.Length = 0 Then
                    Throw New SerializationException("The stream contains no content.")
                End If
                Using reader As New BinaryReader(stream, Encoding.UTF8, leaveOpen:=True)
                    Dim valueType = Type.GetType(reader.ReadString())
                    Return New DataContractSerializer(valueType, s_knownTypes).ReadObject(stream)
                End Using
            End Function

        End Class

    End Module
End Namespace
