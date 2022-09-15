' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.ComponentModel
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices

Imports EnvDTE

Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner.Common

Namespace Microsoft.VisualStudio.Editors.MyApplication

    ''' <summary>
    ''' Generator for strongly MyApplication class
    ''' </summary>
    <Guid("4d35b437-4197-4241-8d24-8ac3ab6f0e0c")>
    <ProvideObject(GetType(MyApplicationCodeGenerator))>
    <CodeGeneratorRegistration(GetType(MyApplicationCodeGenerator), "Generator for MyApplication class", VBPackage.LegacyVBPackageGuid, GeneratesDesignTimeSource:=True)>
    Public NotInheritable Class MyApplicationCodeGenerator
        Implements IVsSingleFileGenerator, IObjectWithSite, System.IServiceProvider, IVsRefactorNotify

        Private _site As Object
        Private _codeDomProvider As CodeDomProvider
        Private _serviceProvider As ServiceProvider

        ' Constants for properties to be generated.
        Private Const MyNamespaceName As String = "My"
        Private Const MainFormFieldName As String = "MainForm"
        Private Const SingleInstanceFieldName As String = "IsSingleInstance"
        Private Const ShutdownModeFieldName As String = "ShutDownStyle"
        Private Const EnableVisualStylesFieldName As String = "EnableVisualStyles"
        Private Const SaveMySettingsOnExitFieldName As String = "SaveMySettingsOnExit"
        Private Const SplashScreenFieldName As String = "SplashScreen"
        Private Const HighDpiModeFieldName As String = "HighDpiMode"

        Private Const HighDpiMode_DpiUnaware = "DpiUnaware"
        Private Const HighDpiMode_SystemAware = "SystemAware"
        Private Const HighDpiMode_PerMonitor = "PerMonitor"
        Private Const HighDpiMode_PerMonitorV2 = "PerMonitorV2"
        Private Const HighDpiMode_DpiUnawareGdiScaled = "DpiUnawareGdiScaled"

        Friend Const SingleFileGeneratorName As String = "MyApplicationCodeGenerator"

        ''' <summary>
        ''' Returns a type for System.Diagnostics.DebuggerStepThrough so that we can create
        ''' attribute-declarations for the CodeDom to spit them out.
        ''' </summary>
        Private Shared ReadOnly Property DebuggerStepThroughAttribute As Type
            Get
                Return GetType(DebuggerStepThroughAttribute)
            End Get
        End Property

#Region "IVsSingleFileGenerator implementation"
        ''' <summary>
        ''' Get the default extension for the generated class.
        ''' </summary>
        ''' <param name="pbstrDefaultExtension"></param>
        Private Function DefaultExtension(ByRef pbstrDefaultExtension As String) As Integer Implements IVsSingleFileGenerator.DefaultExtension
            If CodeDomProvider IsNot Nothing Then
                ' For some reason some the code providers seem to be inconsistent in the way that they 
                ' return the extension - some have a leading "." and some do not...
                Const DESIGNER_PREFIX As String = ".Designer"
                If CodeDomProvider.FileExtension.StartsWith(".") Then
                    pbstrDefaultExtension = DESIGNER_PREFIX & CodeDomProvider.FileExtension
                Else
                    pbstrDefaultExtension = DESIGNER_PREFIX & "." & CodeDomProvider.FileExtension
                End If
            Else
                Debug.Fail("We failed to get a CodeDom provider - defaulting file extension to 'vb'")
                pbstrDefaultExtension = "vb"
            End If
        End Function

        ''' <summary>
        ''' Generate a MyApplication for the contents of the input path
        ''' </summary>
        ''' <param name="wszInputFilePath"></param>
        ''' <param name="bstrInputFileContents"></param>
        ''' <param name="wszDefaultNamespace"></param>
        ''' <param name="rgbOutputFileContents"></param>
        ''' <param name="pcbOutput"></param>
        ''' <param name="pGenerateProgress"></param>
        Private Function Generate(wszInputFilePath As String, bstrInputFileContents As String, wszDefaultNamespace As String, rgbOutputFileContents() As IntPtr, ByRef pcbOutput As UInteger, pGenerateProgress As IVsGeneratorProgress) As Integer Implements IVsSingleFileGenerator.Generate
            Dim BufPtr As IntPtr
            Try

                ' get the DesignTimeSettings from the file content
                '
                Dim data As MyApplicationData = DeserializeMyApplicationData(bstrInputFileContents)

                ' then get the CodeCompileUnit for this .settings file
                '
                Dim CompileUnit As CodeCompileUnit = Create(data, GetRootNamespace(), pGenerateProgress)

                ' Let's start writing to a stream...
                Dim OutputStream As New MemoryStream
                Dim OutputWriter As New StreamWriter(OutputStream, System.Text.Encoding.UTF8)
                CodeDomProvider.GenerateCodeFromCompileUnit(CompileUnit, OutputWriter, New CodeGeneratorOptions())
                OutputWriter.Flush()

                Dim BufLen As Integer = CInt(OutputStream.Length)

                BufPtr = Marshal.AllocCoTaskMem(BufLen)
                Marshal.Copy(OutputStream.ToArray(), 0, BufPtr, BufLen)
                rgbOutputFileContents(0) = BufPtr
                pcbOutput = CUInt(BufLen)

                OutputWriter.Close()
                OutputStream.Close()

                If pGenerateProgress IsNot Nothing Then
                    ' We are done!
                    VSErrorHandler.ThrowOnFailure(pGenerateProgress.Progress(100, 100))
                End If
                BufPtr = IntPtr.Zero
                Return NativeMethods.S_OK
            Catch e As Exception
                If Not BufPtr.Equals(IntPtr.Zero) Then
                    Marshal.FreeCoTaskMem(BufPtr)
                End If

                If pGenerateProgress IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(pGenerateProgress.GeneratorError(0, 1, My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SingleFileGenerator_FailedToGenerateFile_1Arg, e.Message), 0, 0))
                End If
            End Try
            Return NativeMethods.E_FAIL
        End Function

        ''' <summary>
        ''' Creates the CodeCompileUnit for the given MyApplicationData using the given file-path to determine
        ''' the class name.
        ''' </summary>
        ''' <param name="MyApplication">DesignTimeSettings class to generate a CodeCompileUnit from</param>
        ''' <param name="ProjectRootNamespace">The root namespace of the project.  If Nothing, the form will be non-fully qualified.</param>
        ''' <returns>CodeCompileUnit of the given DesignTimeSettings object</returns>
        Private Function Create(MyApplication As MyApplicationData, ProjectRootNamespace As String, pGenerateProgress As IVsGeneratorProgress) As CodeCompileUnit
            Dim CompileUnit As New CodeCompileUnit

            'Set Option Strict On
            CompileUnit.UserData.Add("AllowLateBound", False)

            ' make sure the compile-unit references System to get the base-class definition
            '
            CompileUnit.ReferencedAssemblies.Add("System")

            ' Create a new namespace to put our class in
            '
            Dim MyNamespace As New CodeDom.CodeNamespace(MyNamespaceName)

            'MySubMain will be set to indicate a WindowsApplication sans MY, or non-WindowsApplication type
            If MyApplication.MySubMain AndAlso MyApplicationProperties.IsMySubMainSupported(DirectCast(GetService(GetType(IVsHierarchy)), IVsHierarchy)) Then
                CompileUnit.Namespaces.Add(MyNamespace)

                ' Create the MyApplication partial class
                Dim GeneratedType As New CodeTypeDeclaration("MyApplication")

                'Add comments 
                Dim Comments() As String = {
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_MyAppCommentLine1,
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_MyAppCommentLine2,
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_MyAppCommentLine3,
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_MyAppCommentLine4,
                    My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_MyAppCommentLine5
                }
                For i As Integer = 0 To Comments.Length - 1
                    GeneratedType.Comments.Add(New CodeCommentStatement(Comments(i)))
                Next

                'Set the class visibility
                GeneratedType.TypeAttributes = TypeAttributes.Class Or TypeAttributes.NotPublic

                'We don't write out an Inherits line - the partial class in the
                '  My stuff controls the base class used.

                ' Set this as a partial class
                GeneratedType.IsPartial = True

                'Add constructor, with a DebuggerStepThrough attribute
                '
                '  GENERATED CODE:
                '    <Global.System.Diagnostics.DebuggerStepThrough()> _
                '    Public Sub New()
                '
                Dim Constructor As CodeConstructor = New CodeConstructor With {
                    .Attributes = MemberAttributes.Public
                }
                AddAttribute(Constructor, DebuggerStepThroughAttribute, True)

                'Add AuthenticationMode.Windows as an argument to the MyBase.New() call
                '
                '  GENERATED CODE:
                '    MyBase.New(ApplicationServices.AuthenticationMode.xxx)
                '
                Dim AuthenticationModeEnumType As Type = GetType(ApplicationServices.AuthenticationMode)
                Dim AuthenticationModeValueName As String = [Enum].GetName(AuthenticationModeEnumType, MyApplication.AuthenticationMode)
                Dim AuthenticationModeParameter As CodeExpression = New CodeFieldReferenceExpression(
                    New CodeTypeReferenceExpression(New CodeTypeReference(AuthenticationModeEnumType, CodeTypeReferenceOptions.GlobalReference)), AuthenticationModeValueName)
                Constructor.BaseConstructorArgs.Add(AuthenticationModeParameter)

                'Set member values
                '
                '  GENERATED CODE:
                '    Me.IsSingleInstance = <True/False>
                '    Me.EnableVisualStyles = <True/False>
                '    Me.SaveMySettingsOnExit = <True/False>                
                '
                AddFieldPrimitiveAssignment(Constructor, SingleInstanceFieldName, MyApplication.SingleInstance)
                AddFieldPrimitiveAssignment(Constructor, EnableVisualStylesFieldName, MyApplication.EnableVisualStyles)
                AddFieldPrimitiveAssignment(Constructor, SaveMySettingsOnExitFieldName, MyApplication.SaveMySettingsOnExit)

                '    Me.ShutDownStyle = ApplicationServices.ShutdownMode.xxx
                Dim EnumType As Type
                EnumType = GetType(ApplicationServices.ShutdownMode)
                If MyApplication.ShutdownMode = ApplicationServices.ShutdownMode.AfterAllFormsClose Then
                    AddFieldAssignment(Constructor, ShutdownModeFieldName, EnumType, "AfterAllFormsClose")
                ElseIf MyApplication.ShutdownMode = ApplicationServices.ShutdownMode.AfterMainFormCloses Then
                    AddFieldAssignment(Constructor, ShutdownModeFieldName, EnumType, "AfterMainFormCloses")
                Else
                    Debug.Fail("Unexpected MyApplication.ShutdownMode")
                End If
                
                If IsTargetingDotNetCore(DirectCast(GetService(GetType(IVsHierarchy)), IVsHierarchy))
                    '    Me.HighDpiMode = HighDpiMode.xxx
                    Dim HighDpiValue As String
                    Select Case MyApplication.HighDpiMode
                        Case 0
                            HighDpiValue = HighDpiMode_DpiUnaware
                        Case 1
                            HighDpiValue = HighDpiMode_SystemAware
                        Case 2
                            HighDpiValue = HighDpiMode_PerMonitor
                        Case 3
                            HighDpiValue = HighDpiMode_PerMonitorV2
                        Case 4
                            HighDpiValue = HighDpiMode_DpiUnawareGdiScaled
                        Case Else
                            HighDpiValue = String.Empty
                    End Select
                    AddFieldAssignment(Constructor, HighDpiModeFieldName, HighDpiModeFieldName, HighDpiValue)
                End If
                    
                GeneratedType.Members.Add(Constructor)

                If MyApplication.MainFormNoRootNS <> String.Empty Then
                    'Create OnCreateMainForm override
                    '
                    '  GENERATED CODE:
                    '    <Global.System.Diagnostics.DebuggerStepThrough()> _
                    '    Protected Overrides Sub OnCreateMainForm()
                    '      Me.MainForm = Global.WindowsApplication1.Form1
                    '    End Function
                    '

                    ' Validate the identifier that we are going to spit 

                    ' Since there may still be a namespace prepended here, we have to 
                    ' check each of the namespace parts to make sure that we have a valid 
                    ' identifier name...
                    Dim invalidIdentifier As Boolean = False
                    For Each NamespacePart As String In MyApplication.MainFormNoRootNS.Split("."c)
                        If Not CodeDomProvider.IsValidIdentifier(CodeDomProvider.CreateEscapedIdentifier(NamespacePart)) Then
                            invalidIdentifier = True
                            Exit For
                        End If
                    Next

                    If invalidIdentifier Then
                        Dim errorMsg As String = My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_InvalidIdentifierStartupForm_1Arg, MyApplication.MainFormNoRootNS)
                        If pGenerateProgress IsNot Nothing Then
                            VSErrorHandler.ThrowOnFailure(pGenerateProgress.GeneratorError(0,
                                                                                           1,
                                                                                           My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SingleFileGenerator_FailedToGenerateFile_1Arg, errorMsg),
                                                                                           0,
                                                                                           0))
                        Else
                            Throw New ArgumentException(errorMsg)
                        End If
                    Else
                        Dim OnCreateMainForm As New CodeMemberMethod With {
                            .Attributes = MemberAttributes.Override Or MemberAttributes.Family,
                            .ReturnType = Nothing,
                            .Name = "OnCreateMainForm"
                        }
                        AddAttribute(OnCreateMainForm, DebuggerStepThroughAttribute, True)
                        AddDefaultFormAssignment(OnCreateMainForm, MainFormFieldName, ProjectRootNamespace, MyApplication.MainFormNoRootNS)
                        GeneratedType.Members.Add(OnCreateMainForm)
                    End If
                End If

                If MyApplication.SplashScreenNoRootNS <> String.Empty Then
                    'Create OnCreateSplashScreen override
                    '
                    '  GENERATED CODE:
                    '    <Global.System.Diagnostics.DebuggerStepThrough()> _
                    '    Protected Overrides Sub OnCreateSplashScreen()
                    '      Me.SplashScreen = Global.WindowsApplication1.Form2
                    '    End Function
                    '

                    If Not CodeDomProvider.IsValidIdentifier(MyApplication.SplashScreenNoRootNS) Then
                        Dim errorMsg As String = My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_InvalidIdentifierSplashScreenForm_1Arg, MyApplication.SplashScreenNoRootNS)
                        If pGenerateProgress IsNot Nothing Then
                            VSErrorHandler.ThrowOnFailure(pGenerateProgress.GeneratorError(0,
                                                                                           1,
                                                                                           My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SingleFileGenerator_FailedToGenerateFile_1Arg, errorMsg),
                                                                                           0,
                                                                                           0))
                        Else
                            Throw New ArgumentException(errorMsg)
                        End If
                    Else
                        Dim OnCreateSplashScreen As New CodeMemberMethod With {
                            .Attributes = MemberAttributes.Override Or MemberAttributes.Family,
                            .ReturnType = Nothing,
                            .Name = "OnCreateSplashScreen"
                        }
                        AddAttribute(OnCreateSplashScreen, DebuggerStepThroughAttribute, True)
                        AddDefaultFormAssignment(OnCreateSplashScreen, SplashScreenFieldName, ProjectRootNamespace, MyApplication.SplashScreenNoRootNS)
                        GeneratedType.Members.Add(OnCreateSplashScreen)
                    End If
                End If
                ' Add our class to the namespace...
                MyNamespace.Types.Add(GeneratedType)

            End If

            Return CompileUnit
        End Function

        ''' <summary>
        ''' Given a CodeTypeMember (constructor, method, type, etc.), adds a given attribute to it.
        ''' </summary>
        ''' <param name="Member">The type member to add the attribute to</param>
        ''' <param name="AttributeType">The type of the attribute, e.g. GetType(System.Diagnostics.DebuggerStepThroughAttribute)</param>
        ''' <param name="PrependGlobal">If true, then "Global." is prepended to the attribute name</param>
        Private Shared Sub AddAttribute(Member As CodeTypeMember, AttributeType As Type, PrependGlobal As Boolean)

            If Member.CustomAttributes Is Nothing Then
                Member.CustomAttributes = New CodeAttributeDeclarationCollection()
            End If

            Dim AttributeReference As CodeTypeReference
            If PrependGlobal Then
                AttributeReference = New CodeTypeReference(AttributeType, CodeTypeReferenceOptions.GlobalReference)
            Else
                AttributeReference = New CodeTypeReference(AttributeType)
            End If

            Member.CustomAttributes.Add(New CodeAttributeDeclaration(AttributeReference))
        End Sub

        ' Adds a statement to 'Method' in the form of ' FieldName = Expression ' 
        Private Shared Sub AddFieldAssignment(Method As CodeMemberMethod, FieldName As String, Expression As CodeExpression)
            Dim Statement As CodeAssignStatement
            Statement = New CodeAssignStatement With {
                .Left = New CodeFieldReferenceExpression(New CodeThisReferenceExpression(), FieldName),
                .Right = Expression
            }
            Method.Statements.Add(Statement)
        End Sub

        ' Adds a statement to 'Method' in the form of ' FieldName = Value ' 
        Private Shared Sub AddFieldPrimitiveAssignment(Method As CodeMemberMethod, FieldName As String, Value As Object)
            Dim Statement As CodeAssignStatement
            Statement = New CodeAssignStatement With {
                .Left = New CodeFieldReferenceExpression(New CodeThisReferenceExpression(), FieldName),
                .Right = New CodePrimitiveExpression(Value)
            }
            Method.Statements.Add(Statement)
        End Sub

        ' Adds a statement to 'Method' in the form of ' FieldName = EnumType.EnumField ' 
        Private Shared Sub AddFieldAssignment(Method As CodeMemberMethod, FieldName As String, EnumType As Type, EnumFieldName As String)
            Dim Statement As CodeAssignStatement
            Statement = New CodeAssignStatement With {
                .Left = New CodeFieldReferenceExpression(New CodeThisReferenceExpression(), FieldName)
            }

            Dim TypeRef As CodeTypeReference
            TypeRef = New CodeTypeReference(EnumType) With {
                .Options = CodeTypeReferenceOptions.GlobalReference
            }

            Dim value1 As New CodeFieldReferenceExpression(New CodeTypeReferenceExpression(TypeRef), EnumFieldName)
            Statement.Right = value1
            Method.Statements.Add(Statement)
        End Sub

        ' Overload of AddFieldAssignment to accept a String instead of a Type for EnumType '
        ' Background: For the case of HighDpiMode, the actual Type is only available in the Designer OOP server-side.
        ' So, to use types, we would need to generate the whole code also server-side.
        ' We don't think it's necessary though, and consider it is too much of an effort,
        ' so we decided to use a String for the type name client side.
        Private Shared Sub AddFieldAssignment(Method As CodeMemberMethod, FieldName As String, EnumType As String, EnumFieldName As String)
            Dim Statement As CodeAssignStatement
            Statement = New CodeAssignStatement With {
                .Left = New CodeFieldReferenceExpression(New CodeThisReferenceExpression(), FieldName)
            }

            Dim TypeRef As CodeTypeReference
            TypeRef = New CodeTypeReference(EnumType)

            Dim value1 As New CodeFieldReferenceExpression(New CodeTypeReferenceExpression(TypeRef), EnumFieldName)
            Statement.Right = value1
            Method.Statements.Add(Statement)
        End Sub

        ''' <summary>
        ''' Adds a statement to 'Method' in the form of "Me.xxx = FormX"
        ''' </summary>
        ''' <param name="Method">The Method to add the assignment to</param>
        ''' <param name="FieldName">The name of the field for the left-hand side of the assignment</param>
        ''' <param name="RootNamespace">The root namespace, if any (including empty string if none), or else Nothing if reference should be declared without using the root namespace.</param>
        ''' <param name="FormNameWithoutRootNamespace">The name of the form, without the root namespace.</param>
        Private Shared Sub AddDefaultFormAssignment(Method As CodeMemberMethod, FieldName As String, RootNamespace As String, FormNameWithoutRootNamespace As String)
            '
            '  GENERATED CODE:
            '      Me.MainForm = Global.WindowsApplication1.FormXXX
            '
            Debug.Assert(FieldName IsNot Nothing)
            If FormNameWithoutRootNamespace Is Nothing Then
                FormNameWithoutRootNamespace = String.Empty
            End If

            If RootNamespace Is Nothing Then
                AddFieldAssignment(Method, FieldName, New CodeTypeReferenceExpression(New CodeTypeReference(FormNameWithoutRootNamespace)))
            Else
                AddFieldAssignment(Method, FieldName, New CodeTypeReferenceExpression(New CodeTypeReference(Common.CombineNamespaces(RootNamespace, FormNameWithoutRootNamespace), CodeTypeReferenceOptions.GlobalReference)))
            End If
        End Sub

        ''' <summary>
        ''' Deserialize contents of XML input string into a DesignTimeSettings object
        ''' </summary>
        Private Function DeserializeMyApplicationData(InputString As String) As MyApplicationData
            Dim Hierarchy As IVsHierarchy = DirectCast(GetService(GetType(IVsHierarchy)), IVsHierarchy)
            Debug.Assert(Hierarchy IsNot Nothing, "Failed to get a Hierarchy item for item to generate code from")
            Dim data As MyApplicationData = Nothing
            If InputString <> String.Empty Then
                ' We actually have some contents to deserialize.... 
                Dim MyApplicationReader As New StringReader(InputString)
                data = MyApplicationSerializer.Deserialize(MyApplicationReader)
            End If
            If data Is Nothing Then
                data = New MyApplicationData()
            End If
            Return data
        End Function

        ''' <summary>
        ''' Demand-create a CodeDomProvider corresponding to my projects current language
        ''' </summary>
        ''' <value>A CodeDomProvider</value>
        Private Property CodeDomProvider As CodeDomProvider
            Get
                If _codeDomProvider Is Nothing Then
                    Dim VSMDCodeDomProvider As IVSMDCodeDomProvider = CType(GetService(GetType(IVSMDCodeDomProvider)), IVSMDCodeDomProvider)
                    If VSMDCodeDomProvider IsNot Nothing Then
                        _codeDomProvider = CType(VSMDCodeDomProvider.CodeDomProvider, CodeDomProvider)
                    End If
                    Debug.Assert(_codeDomProvider IsNot Nothing, "Get CodeDomProvider Interface failed.  GetService(QueryService(CodeDomProvider) returned Null.")
                End If
                Return _codeDomProvider
            End Get
            Set
                If Value Is Nothing Then
                    Throw New ArgumentNullException()
                End If
                _codeDomProvider = Value
            End Set
        End Property

        ''' <summary>
        ''' Gets the root namespace of the VB project
        ''' </summary>
        ''' <returns>The root namespace if found (empty string if root namespace is empty), else Nothing if the call
        '''   failed.
        ''' </returns>
        Private Function GetRootNamespace() As String
            Try
                Dim punkVsBrowseObject As IntPtr
                Dim vsBrowseObjectGuid As Guid = GetType(IVsBrowseObject).GUID

                GetSite(vsBrowseObjectGuid, punkVsBrowseObject)
                If Not punkVsBrowseObject.Equals(IntPtr.Zero) Then
                    Dim VsBrowseObject As IVsBrowseObject = TryCast(Marshal.GetObjectForIUnknown(punkVsBrowseObject), IVsBrowseObject)
                    Debug.Assert(VsBrowseObject IsNot Nothing, "Generator invoked by Site that is not IVsBrowseObject?")

                    Marshal.Release(punkVsBrowseObject)

                    If VsBrowseObject IsNot Nothing Then
                        Dim VsHierarchy As IVsHierarchy = Nothing
                        Dim VsItemId As UInteger

                        VsBrowseObject.GetProjectItem(VsHierarchy, VsItemId)

                        Debug.Assert(VsHierarchy IsNot Nothing, "GetProjectItem should have thrown or returned a valid IVsHierarchy")
                        Debug.Assert(VsItemId <> 0, "GetProjectItem should have thrown or returned a valid VSITEMID")

                        If VsHierarchy IsNot Nothing Then
                            Dim RootNSObject As Object = Nothing

                            VsHierarchy.GetProperty(VsItemId, CInt(__VSHPROPID.VSHPROPID_DefaultNamespace), RootNSObject)
                            Dim nsObject = TryCast(RootNSObject, String)
                            If nsObject IsNot Nothing Then
                                Return nsObject
                            End If
                        End If
                    End If
                End If
            Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(GetRootNamespace), NameOf(MyApplicationCodeGenerator))
            End Try

            Debug.Fail("Unable to get the project's root namespace from MyApplicationCodeGenerator")
            Return Nothing
        End Function

#End Region

#Region "IObjectWithSite implementation"
        Private Sub GetSite(ByRef riid As Guid, ByRef ppvSite As IntPtr) Implements IObjectWithSite.GetSite
            If _site Is Nothing Then
                ' Throw E_FAIL
                Throw New Win32Exception(NativeMethods.E_FAIL)
            End If

            Dim pUnknownPointer As IntPtr = Marshal.GetIUnknownForObject(_site)
            Try
                Marshal.QueryInterface(pUnknownPointer, riid, ppvSite)

                If ppvSite = IntPtr.Zero Then
                    ' throw E_NOINTERFACE
                    Throw New Win32Exception(NativeMethods.E_NOINTERFACE)
                End If
            Finally
                If pUnknownPointer <> IntPtr.Zero Then
                    Marshal.Release(pUnknownPointer)
                End If
            End Try
        End Sub

        Private Sub SetSite(pUnkSite As Object) Implements IObjectWithSite.SetSite
            _site = pUnkSite
        End Sub
#End Region

#Region "IVsRefactorNotify Implementation"
        ' ******************* Implement IVsRefactorNotify *****************

        ''' <summary>
        ''' Called when a symbol is about to be renamed
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="cRQNames">count of RQNames passed in. This count can be greater than 1 when an overloaded symbol is being renamed</param>
        ''' <param name="rglpszRQName">RQName-syntax string that identifies the symbol(s) renamed</param>
        ''' <param name="lpszNewName">name that the symbol identified by rglpszRQName is being changed to</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeGlobalSymbolRenamed(phier As IVsHierarchy, itemId As UInteger, cRQNames As UInteger, rglpszRQName() As String, lpszNewName As String, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeGlobalSymbolRenamed
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Dim changesRequired As Boolean = False

            Dim designerPrjItem As ProjectItem = GetDesignerProjectItem(phier, itemId)
            If designerPrjItem IsNot Nothing Then
                Dim applicationData As MyApplicationData = Nothing
                Using dd As New Design.Serialization.DocData(Common.ServiceProviderFromHierarchy(phier), designerPrjItem.FileNames(1))
                    applicationData = GetApplicationData(dd)
                End Using
                If applicationData IsNot Nothing Then
                    Dim oldSymbolName As String = GetSymbolNameNoRootNamespace(rglpszRQName(0), designerPrjItem.ContainingProject)
                    If oldSymbolName IsNot Nothing Then
                        ' if the old class name matches the MainForm name or the SplashScreen name modify it.
                        Dim comparisonType As StringComparison = StringComparison.OrdinalIgnoreCase
                        If designerPrjItem.ContainingProject.CodeModel.IsCaseSensitive Then
                            comparisonType = StringComparison.Ordinal
                        End If

                        If String.Equals(oldSymbolName, applicationData.MainFormNoRootNS, comparisonType) _
                            Or String.Equals(oldSymbolName, applicationData.SplashScreenNoRootNS, comparisonType) Then
                            changesRequired = True
                        End If
                    End If
                End If
            End If

            If changesRequired Then
                'we'll have to modify the designer file; here we need to tell the project system to check it out
                Dim iFound As Integer = 0
                Dim schemaItemId As UInteger = 0
                Dim pdwPriority As VSDOCUMENTPRIORITY() = New VSDOCUMENTPRIORITY(0) {VSDOCUMENTPRIORITY.DP_Standard}
                Dim project As IVsProject = DirectCast(phier, IVsProject)
                Dim fileFullPath As String = designerPrjItem.Properties.Item("FullPath").Value.ToString()

                VSErrorHandler.ThrowOnFailure(project.IsDocumentInProject(fileFullPath, iFound, pdwPriority, schemaItemId))
                If schemaItemId <> 0 Then
                    prgAdditionalCheckoutVSITEMIDS = Array.CreateInstance(GetType(UInteger), 1)
                    prgAdditionalCheckoutVSITEMIDS.SetValue(schemaItemId, prgAdditionalCheckoutVSITEMIDS.GetLowerBound(0))
                End If
            End If

            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Called when a method is about to have its params reordered
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="cRQNames">count of RQNames passed in. This count can be greater than 1 when an overloaded symbol is being renamed</param>
        ''' <param name="rglpszRQName">RQName-syntax string that identifies the symbol(s) renamed</param>
        ''' <param name="lpszNewName">name that the symbol identified by rglpszRQName is being changed to</param>
        ''' <returns>error code</returns>
        Private Function OnGlobalSymbolRenamed(phier As IVsHierarchy, itemId As UInteger, cRQNames As UInteger, rglpszRQName() As String, lpszNewName As String) As Integer Implements IVsRefactorNotify.OnGlobalSymbolRenamed
            Debug.Assert(cRQNames = 1, String.Format("Why Do we get {0} symbols to rename?", cRQNames))

            Dim designerPrjItem As ProjectItem = GetDesignerProjectItem(phier, itemId)
            If designerPrjItem IsNot Nothing Then
                Try
                    Using dd As New Design.Serialization.DocData(Common.ServiceProviderFromHierarchy(phier), designerPrjItem.FileNames(1))
                        Dim data As MyApplicationData = GetApplicationData(dd)
                        If data IsNot Nothing Then
                            Dim oldSymbolName As String = GetSymbolNameNoRootNamespace(rglpszRQName(0), designerPrjItem.ContainingProject)
                            If oldSymbolName IsNot Nothing Then
                                Dim namespaceNoClass As String = String.Empty
                                Dim i As Integer = oldSymbolName.LastIndexOf(".")
                                If i >= 0 Then
                                    namespaceNoClass = oldSymbolName.Substring(0, i + 1)
                                End If

                                Dim madeChanges As Boolean = False
                                ' if the old class name matches the MainForm name or the SplashScreen name modify it.
                                Dim comparisonType As StringComparison = StringComparison.OrdinalIgnoreCase
                                If designerPrjItem.ContainingProject.CodeModel.IsCaseSensitive Then
                                    comparisonType = StringComparison.Ordinal
                                End If

                                If String.Equals(oldSymbolName, data.MainFormNoRootNS, comparisonType) Then
                                    data.MainFormNoRootNS = namespaceNoClass + lpszNewName
                                    madeChanges = True
                                ElseIf String.Equals(oldSymbolName, data.SplashScreenNoRootNS, comparisonType) Then
                                    data.SplashScreenNoRootNS = namespaceNoClass + lpszNewName
                                    madeChanges = True
                                End If

                                If madeChanges Then
                                    Using myappWriter As New Design.Serialization.DocDataTextWriter(dd)
                                        MyApplicationSerializer.Serialize(data, myappWriter)
                                        myappWriter.Close()
                                    End Using
                                End If

                            End If
                        End If
                    End Using
                Catch ex As Exception When Common.ReportWithoutCrash(ex, "Failed to save changes to myapp file", NameOf(MyApplicationCodeGenerator))
                End Try
            End If

            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Called when a method is about to have params added
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method on which params are being added</param>
        ''' <param name="cParams">number of parameters in rgszRQTypeNames, rgszParamNames and rgszDefaultValues</param>
        ''' <param name="rgszParamIndexes">the indexes of the new parameters</param>
        ''' <param name="rgszRQTypeNames">RQName-syntax strings that identify the types of the new parameters</param>
        ''' <param name="rgszParamNames">the names of the parameters</param>
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeAddParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParams As UInteger, rgszParamIndexes() As UInteger, rgszRQTypeNames() As String, rgszParamNames() As String, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeAddParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called after a method has had params added
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method on which params are being added</param>
        ''' <param name="cParams">number of parameters in rgszRQTypeNames, rgszParamNames and rgszDefaultValues</param>
        ''' <param name="rgszParamIndexes">the indexes of the new parameters</param>
        ''' <param name="rgszRQTypeNames">RQName-syntax strings that identify the types of the new parameters</param>
        ''' <param name="rgszParamNames">the names of the parameters</param>
        ''' <returns>error code</returns>
        Private Function OnAddParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParams As UInteger, rgszParamIndexes() As UInteger, rgszRQTypeNames() As String, rgszParamNames() As String) As Integer Implements IVsRefactorNotify.OnAddParams
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called when a method is about to have its params reordered
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being reordered</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where the index in this array is the index to which the param is moving</param>
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeReorderParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeReorderParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called after a method has had its params reordered
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being reordered</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where the index in this array is the index to which the param is moving</param>
        ''' <returns>error code</returns>
        Private Function OnReorderParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger) As Integer Implements IVsRefactorNotify.OnReorderParams
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called when a method is about to have some params removed
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being removed</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where each value indicates the index of the parameter being removed</param>
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeRemoveParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeRemoveParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called when a method is about to have some params removed
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being removed</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where each value indicates the index of the parameter being removed</param>
        ''' <returns>error code</returns>
        Private Function OnRemoveParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger) As Integer Implements IVsRefactorNotify.OnRemoveParams
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Returns the ProjectItem corresponding to the hierarchy and itemId
        ''' </summary>
        ''' <param name="phier">the IVsHierarchy to get the ProjectItem from</param>
        ''' <param name="itemId">the ItemId corresponding to the ProjectItem</param>
        ''' <returns>the ProjectItem found</returns>
        Private Shared Function GetDesignerProjectItem(phier As IVsHierarchy, itemId As UInteger) As ProjectItem
            ' retrieve the ProjectItem corresponding to the itemId; it's the generated code file
            Dim o As Object = Nothing
            VSErrorHandler.ThrowOnFailure(phier.GetProperty(itemId, __VSHPROPID.VSHPROPID_ExtObject, o))
            Debug.Assert(TypeOf o Is ProjectItem, "returned object is not a ProjectItem?")
            Dim projItem As ProjectItem = TryCast(o, ProjectItem)

            If projItem IsNot Nothing AndAlso projItem.Collection IsNot Nothing Then
                ' get the parent project item, it's the one we want to modify
                Return TryCast(projItem.Collection.Parent, ProjectItem)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the ApplicationData from the 'myApp' project item
        ''' </summary>
        ''' <param name="dd">the DocData to load the ApplicationData from</param>
        ''' <returns>the ApplicationData loaded from the ProjectItem</returns>
        Private Shared Function GetApplicationData(dd As Shell.Design.Serialization.DocData) As MyApplicationData
            Dim data As MyApplicationData = Nothing
            If dd IsNot Nothing Then
                ' read the content of this ProjectItem into a MyApplicationData object
                Using myApplicationReader As New Design.Serialization.DocDataTextReader(dd)
                    data = MyApplicationSerializer.Deserialize(myApplicationReader)
                End Using
            End If

            Return data
        End Function

        ''' <summary>
        ''' Parses the aggregate name to find the class name
        ''' </summary>
        ''' <param name="rqName">the aggregate name to parse</param>
        ''' <returns>the class name including sub-namespaces, but without the root namespace</returns>
        Private Shared Function GetSymbolNameNoRootNamespace(rqName As String, currentProject As Project) As String
            ' extract the class name we want to change from the aggregate name provided by the language service
            Dim symbolName As String = RenamingHelper.ParseRQName(rqName)

            ' if the symbol name starts with the current project's root namespace, remove it
            Dim namespaceProperty As [Property] = currentProject.Properties.Item("RootNamespace")
            If namespaceProperty IsNot Nothing Then
                Dim rootNamespace As String = DirectCast(namespaceProperty.Value, String)
                If symbolName IsNot Nothing AndAlso symbolName.StartsWith(rootNamespace) Then
                    symbolName = symbolName.Substring(rootNamespace.Length + 1)
                End If
            End If

            Return symbolName
        End Function

#End Region

#Region "IServiceProvider"

        ''' <summary>
        ''' I'm capable of providing services
        ''' </summary>
        ''' <param name="serviceType">The type of service requested</param>
        ''' <returns>An instance of the service, or nothing if service not found</returns>
        Private Function GetService(serviceType As Type) As Object Implements System.IServiceProvider.GetService
            If ServiceProvider IsNot Nothing Then
                Return ServiceProvider.GetService(serviceType)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Demand-create service provider from my site
        ''' </summary>
        Private ReadOnly Property ServiceProvider As ServiceProvider
            Get
                If _serviceProvider Is Nothing Then
                    Dim OleSp As IServiceProvider = CType(_site, IServiceProvider)
                    _serviceProvider = New ServiceProvider(OleSp)
                End If
                Return _serviceProvider
            End Get
        End Property

#End Region

    End Class
End Namespace
