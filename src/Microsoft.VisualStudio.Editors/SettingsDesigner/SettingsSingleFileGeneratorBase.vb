' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.ComponentModel
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Designer.Interfaces
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner.Common

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    Public Class SettingsSingleFileGeneratorBase
        Implements IVsSingleFileGenerator, IObjectWithSite, System.IServiceProvider, IVsRefactorNotify

        Private _site As Object
        Private _codeDomProvider As CodeDomProvider
        Private _serviceProvider As ServiceProvider

        Private Const AddedHandlerFieldName As String = "addedHandler"
        Private Const AddedHandlerLockObjectFieldName As String = "addedHandlerLockObject"
        Private Const AutoSaveSubName As String = "AutoSaveSettings"
        Friend Const DefaultInstanceFieldName As String = "defaultInstance"
        Friend Const DefaultInstancePropertyName As String = "Default"

        Friend Const MyNamespaceName As String = "My"
        Private Const MySettingsModuleName As String = "MySettingsProperty"
        Private Const MySettingsPropertyName As String = "Settings"

        Private Const MyTypeWinFormsDefineConstant_If As String = "#If _MyType = ""WindowsForms"" Then"
        Private Const MyTypeWinFormsDefineConstant_EndIf As String = "#End If"

        Private Const HideAutoSaveRegionBegin As String = "#Region ""{0}"""
        Private Const HideAutoSaveRegionEnd As String = "#End Region"

        Private Const DocCommentSummaryStart As String = "<summary>"
        Private Const DocCommentSummaryEnd As String = "</summary>"

        Friend Const DesignerGeneratedFileSuffix As String = ".Designer"

        ''' <summary>
        ''' If set to true, tells the shell that symbolic renames are OK. 
        ''' </summary>
        ''' <remarks>
        ''' Normally, we can't handle symbolic renames since we don't update the contents of the .settings
        ''' file (which means that we overwrite the changes the next time the file is generated. 
        ''' In the special case where the designer invokes the symbolic rename, we should allow it.
        ''' 
        ''' Since all the file generation should happen on the main thread, it is OK to have this member shared...
        ''' </remarks>
        Friend Shared AllowSymbolRename As Boolean

        ''' <summary>
        ''' Returns the default visibility of this properties
        ''' </summary>
        ''' <remarks>MemberAttributes indicating what visibility to make the generated properties.</remarks>
        Friend Shared ReadOnly Property SettingsPropertyVisibility As MemberAttributes
            Get
                Return MemberAttributes.Public Or MemberAttributes.Final
            End Get
        End Property

        ''' <summary>
        ''' Returns the default visibility of this properties
        ''' </summary>
        ''' <value>MemberAttributes indicating what visibility to make the generated properties.</value>
        Friend Overridable ReadOnly Property SettingsClassVisibility As TypeAttributes
            Get
                Return TypeAttributes.Sealed Or TypeAttributes.NestedAssembly
            End Get
        End Property

        ''' <summary>
        ''' Allow derived classes to modify the generated settings class and/or compile unit
        ''' </summary>
        ''' <param name="compileUnit">The full compile unit that we are to generate code from</param>
        ''' <param name="generatedClass">The generated settings class</param>
        Protected Overridable Sub OnCompileUnitCreated(compileUnit As CodeCompileUnit, generatedClass As CodeTypeDeclaration)
            ' By default, we don't want to make any modifications...
        End Sub

#Region "IVsSingleFileGenerator implementation"
        ''' <summary>
        ''' Get the default extension for the generated class.
        ''' </summary>
        ''' <param name="pbstrDefaultExtension"></param>
        Private Function DefaultExtension(ByRef pbstrDefaultExtension As String) As Integer Implements IVsSingleFileGenerator.DefaultExtension
            If CodeDomProvider IsNot Nothing Then
                ' For some reason some the code providers seem to be inconsistent in the way that they 
                ' return the extension - some have a leading "." and some do not...
                If CodeDomProvider.FileExtension.StartsWith(".") Then
                    pbstrDefaultExtension = DesignerGeneratedFileSuffix & CodeDomProvider.FileExtension
                Else
                    pbstrDefaultExtension = DesignerGeneratedFileSuffix & "." & CodeDomProvider.FileExtension
                End If
            Else
                Debug.Fail("We failed to get a CodeDom provider - defaulting file extension to 'Designer.vb'")
                pbstrDefaultExtension = DesignerGeneratedFileSuffix & ".vb"
            End If
        End Function

        ''' <summary>
        ''' Generate a strongly typed wrapper for the contents of the setting path
        ''' </summary>
        ''' <param name="wszInputFilePath"></param>
        ''' <param name="bstrInputFileContents"></param>
        ''' <param name="wszDefaultNamespace"></param>
        ''' <param name="rgbOutputFileContents"></param>
        ''' <param name="pcbOutput"></param>
        ''' <param name="pGenerateProgress"></param>
        Private Function Generate(wszInputFilePath As String, bstrInputFileContents As String, wszDefaultNamespace As String, rgbOutputFileContents() As IntPtr, ByRef pcbOutput As UInteger, pGenerateProgress As IVsGeneratorProgress) As Integer Implements IVsSingleFileGenerator.Generate
            Dim BufPtr As IntPtr = IntPtr.Zero
            Try
                ' get the DesignTimeSettings from the file content
                '
                Dim Settings As DesignTimeSettings = DeserializeSettings(bstrInputFileContents, pGenerateProgress)

                ' Add appropriate references to the project
                '
                AddRequiredReferences(pGenerateProgress)

                ' We have special handling for VB
                '
                Dim isVB As Boolean = CodeDomProvider.FileExtension.Equals("vb", StringComparison.Ordinal)

                ' And even more special handling for the default VB file...
                '
                Dim shouldGenerateMyStuff As Boolean = isVB AndAlso IsDefaultSettingsFile(wszInputFilePath)

                Dim typeAttrs As TypeAttributes

                typeAttrs = SettingsClassVisibility

                ' for VB, we need to generate some code that is fully-qualified, but our generator is always invoked
                '   without the project's root namespace due to VB convention. If this is VB, then we need to look
                '   up the project's root namespace and pass that in to Create in order to be able to generate the
                '   appropriate code.
                '
                Dim projectRootNamespace As String = String.Empty
                If isVB Then
                    projectRootNamespace = GetProjectRootNamespace()
                End If
                
                ' then get the CodeCompileUnit for this .settings file
                '
                Dim generatedClass As CodeTypeDeclaration = Nothing
                Dim CompileUnit As CodeCompileUnit = Create(isVB,
                                                            DirectCast(GetService(GetType(IVsHierarchy)), IVsHierarchy),
                                                            Settings,
                                                            wszDefaultNamespace,
                                                            wszInputFilePath,
                                                            False,
                                                            typeAttrs,
                                                            shouldGenerateMyStuff,
                                                            generatedClass)

                ' For VB, we need to add Option Strict ON, Option Explicit ON plus check whether or not we
                '   should add the My module
                '
                If isVB Then

                    CompileUnit.UserData("AllowLateBound") = False
                    CompileUnit.UserData("RequireVariableDeclaration") = True

                    ' If this is the "default" settings file, we add the "My" module as well...
                    '
                    If shouldGenerateMyStuff Then
                        AddMyModule(CompileUnit, projectRootNamespace, DesignUtil.GenerateValidLanguageIndependentNamespace(wszDefaultNamespace), isVB)
                    End If
                End If

                OnCompileUnitCreated(CompileUnit, generatedClass)

                Try
                    CodeGenerator.ValidateIdentifiers(CompileUnit)
                Catch argEx As ArgumentException
                    ' We have an invalid identifier here...
                    If pGenerateProgress IsNot Nothing Then
                        VSErrorHandler.ThrowOnFailure(pGenerateProgress.GeneratorError(0, 1, My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SingleFileGenerator_FailedToGenerateFile_1Arg, argEx.Message), 0, 0))
                        Return NativeMethods.E_FAIL
                    Else
                        Throw
                    End If
                End Try

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
                If pGenerateProgress IsNot Nothing Then
                    VSErrorHandler.ThrowOnFailure(pGenerateProgress.GeneratorError(0, 1, My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SingleFileGenerator_FailedToGenerateFile_1Arg, e.Message), 0, 0))
                End If
            Finally
                If Not BufPtr.Equals(IntPtr.Zero) Then
                    Marshal.FreeCoTaskMem(BufPtr)
                End If
            End Try
            Return NativeMethods.E_FAIL
        End Function

        ''' <summary>
        ''' Creates the CodeCompileUnit for the given DesignTimeSettings using the given file-path to determine the class name.
        ''' </summary>
        ''' <param name="Hierarchy">Hierarchy that contains the settings file</param>
        ''' <param name="Settings">DesignTimeSettings class to generate a CodeCompileUnit from</param>
        ''' <param name="DefaultNamespace">namespace to generate code within</param>
        ''' <param name="FilePath">path to the file this Settings object is (used to create the class name)</param>
        ''' <param name="IsDesignTime">flag to tell whether we are generating for design-time consumers like SettingsGlobalObjectProvider users or not.</param>
        ''' <param name="GeneratedClassVisibility"></param>
        ''' <param name="GenerateVBMyAutoSave"></param>
        ''' <returns>CodeCompileUnit of the given DesignTimeSettings object</returns>
        Friend Shared Function Create(IsVb as Boolean,
                                      Hierarchy As IVsHierarchy,
                                      Settings As DesignTimeSettings,
                                      DefaultNamespace As String,
                                      FilePath As String,
                                      IsDesignTime As Boolean,
                                      GeneratedClassVisibility As TypeAttributes,
                                      Optional GenerateVBMyAutoSave As Boolean = False,
                                      <Out> Optional ByRef generatedType As CodeTypeDeclaration = Nothing) As CodeCompileUnit

            Dim CompileUnit As New CodeCompileUnit

            ' make sure the compile-unit references System to get the base-class definition
            '
            CompileUnit.ReferencedAssemblies.Add("System")

            ' Create a new namespace to put our class in
            '
            Dim ns as CodeNamespace
            
            If IsVb Then
                ns = New CodeNamespace(MyNamespaceName)
            Else
                ns = New CodeNamespace(DesignerFramework.DesignUtil.GenerateValidLanguageIndependentNamespace(DefaultNamespace))
            End If
            
            CompileUnit.Namespaces.Add(ns)

            ' Create the strongly typed settings class
            ' VsWhidbey 234144, Make sure this is a valid class name
            '
            ' pick up the default visibility
            '
            generatedType = New CodeTypeDeclaration(SettingsDesigner.GeneratedClassName(Hierarchy, VSITEMID.NIL, Settings, FilePath)) With {
                .TypeAttributes = GeneratedClassVisibility
            }

            ' Set the base class
            '
            generatedType.BaseTypes.Add(CreateGlobalCodeTypeReference(SettingsBaseClass))

            ' This is the "main" partial class - there may be others that expand this class
            ' that contain user code...
            '
            generatedType.IsPartial = True

            ' add the CompilerGeneratedAttribute in order to support deploying VB apps in Yukon (where
            '   our shared/static fields make the code not "safe" according to Yukon's measure of what
            '   it means to deploy a safe assembly. VSWhidbey 320692.
            '
            Dim CompilerGeneratedAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Runtime.CompilerServices.CompilerGeneratedAttribute)))
            generatedType.CustomAttributes.Add(CompilerGeneratedAttribute)

            ' Tell FXCop that we are compiler generated stuff...
            Static toolName As String = GetType(SettingsSingleFileGenerator).FullName
            Static toolVersion As String = GetType(SettingsSingleFileGenerator).Assembly.GetName().Version.ToString()
            Dim GeneratedCodeAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(GeneratedCodeAttribute)),
                                                                       New CodeAttributeArgument() {New CodeAttributeArgument(New CodePrimitiveExpression(toolName)),
                                                                                                     New CodeAttributeArgument(New CodePrimitiveExpression(toolVersion))})
            generatedType.CustomAttributes.Add(GeneratedCodeAttribute)

            ' add the shared getter that fetches the default instance
            '
            AddDefaultInstance(generatedType, GenerateVBMyAutoSave)

            ' and then add each setting as a property
            '

            ' We don't really care about the current language since we only want to translate the virtual type names
            ' into .NET FX type names...
            For Each Instance As DesignTimeSettingInstance In Settings
                generatedType.Members.Add(CodeDomPropertyFromSettingInstance(Instance, IsDesignTime))
            Next

            ' Add our class to the namespace...
            '
            ns.Types.Add(generatedType)

            Return CompileUnit

        End Function

        ''' <summary>
        ''' Deserialize contents of XML input string into a DesignTimeSettings object
        ''' </summary>
        ''' <param name="InputString"></param>
        ''' <param name="GenerateProgress"></param>
        Private Shared Function DeserializeSettings(InputString As String, GenerateProgress As IVsGeneratorProgress) As DesignTimeSettings
            Dim Settings As New DesignTimeSettings()
            If InputString <> "" Then
                ' We actually have some contents to deserialize.... 
                Dim SettingsReader As New StringReader(InputString)
                Try
                    SettingsSerializer.Deserialize(Settings, SettingsReader, True)
                Catch ex As Xml.XmlException
                    If GenerateProgress IsNot Nothing Then
                        GenerateProgress.GeneratorError(0, 1, ex.Message, &HFFFFFFFFUL, &HFFFFFFFFUL)
                    Else
                        Throw
                    End If
                End Try
            End If
            Return Settings
        End Function

        ''' <summary>
        ''' Generate a CodeDomProperty to be the shared accessor
        ''' </summary>
        Private Shared Sub AddDefaultInstance(GeneratedType As CodeTypeDeclaration, Optional GenerateVBMyAutoSave As Boolean = False)

            ' type-reference that both the default-instance field and the property will be
            '
            Dim SettingsClassTypeReference As New CodeTypeReference(GeneratedType.Name)

            ' Emit default instance field.
            '
            '     Private Shared defaultInstance As Settings = CType(Global.System.Configuration.ApplicationSettingsBase.Synchronized(New Settings),Settings)
            '
            Dim Field As New CodeMemberField(SettingsClassTypeReference, DefaultInstanceFieldName)
            Dim NewInstanceExpression As New CodeObjectCreateExpression(SettingsClassTypeReference)
            Dim SynchronizedExpression As New CodeMethodInvokeExpression(New CodeTypeReferenceExpression(New CodeTypeReference(SettingsBaseClass, CodeTypeReferenceOptions.GlobalReference)),
                                                                         "Synchronized",
                                                                         New CodeExpression() {NewInstanceExpression})
            Dim InitExpression As New CodeCastExpression(GeneratedType.Name, SynchronizedExpression)

            Field.Attributes = MemberAttributes.Private Or MemberAttributes.Static
            Field.InitExpression = InitExpression

            GeneratedType.Members.Add(Field)

            ' Emit the property that returns the default-instance field
            '
            '   Public Shared ReadOnly Property [Default]() As Settings
            '
            Dim CodeProperty As New CodeMemberProperty With {
                .Attributes = MemberAttributes.Public Or MemberAttributes.Static,
                .Name = DefaultInstancePropertyName,
                .Type = SettingsClassTypeReference,
                .HasGet = True,
                .HasSet = False
            }

            ' We should hook up the My.Application.Shutdown event if told to auto save the 
            ' settings (only applicable for the main settings file & only applicable for VB)
            '
            If GenerateVBMyAutoSave Then
                ' if we need to generate the My.Settings module + AutoSave functionality, we should mark the class itself
                '   as advanced so it doesn't clutter IntelliSense because users will access this class via My.Settings, not
                '   Settings.Default
                '
                '   <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _
                '   Class <settings-name-goes-here>
                '
                Dim browsableStateTypeReference As CodeTypeReference = CreateGlobalCodeTypeReference(GetType(EditorBrowsableState))
                Dim browsableAttributeTypeReference As CodeTypeReference = CreateGlobalCodeTypeReference(GetType(EditorBrowsableAttribute))

                Dim browsableAdvancedFieldReference As New CodeFieldReferenceExpression(New CodeTypeReferenceExpression(browsableStateTypeReference), "Advanced")
                Dim parameters() As CodeAttributeArgument = {New CodeAttributeArgument(browsableAdvancedFieldReference)}

                Dim browsableAdvancedAttribute As New CodeAttributeDeclaration(browsableAttributeTypeReference, parameters)

                GeneratedType.CustomAttributes.Add(browsableAdvancedAttribute)

                ' Add the AddHandler call that hooks My.Application.Shutdown inside the default-instance getter
                '
                Dim AutoSaveSnippet As New CodeSnippetExpression With {
                    .Value =
                    Environment.NewLine &
                    MyTypeWinFormsDefineConstant_If & Environment.NewLine &
                    "               If Not " & AddedHandlerFieldName & " Then" & Environment.NewLine &
                    "                    SyncLock " & AddedHandlerLockObjectFieldName & Environment.NewLine &
                    "                        If Not " & AddedHandlerFieldName & " Then" & Environment.NewLine &
                    "                            AddHandler My.Application.Shutdown, AddressOf " & AutoSaveSubName & Environment.NewLine &
                    "                            " & AddedHandlerFieldName & " = True" & Environment.NewLine &
                    "                        End If" & Environment.NewLine &
                    "                    End SyncLock" & Environment.NewLine &
                    "                End If" & Environment.NewLine &
                    MyTypeWinFormsDefineConstant_EndIf
                }

                CodeProperty.GetStatements.Add(AutoSaveSnippet)
            End If

            ' Emit return line
            '
            '   Return defaultInstance
            '
            Dim ValueReference As New CodeFieldReferenceExpression With {
                .FieldName = DefaultInstanceFieldName
            }
            CodeProperty.GetStatements.Add(New CodeMethodReturnStatement(ValueReference))

            ' And last, add the property to the class we're generating
            '
            GeneratedType.Members.Add(CodeProperty)
        End Sub

        ''' <summary>
        ''' Given a setting instance, generate a CodeDomProperty
        ''' </summary>
        Private Shared Function CodeDomPropertyFromSettingInstance(Instance As DesignTimeSettingInstance, IsDesignTime As Boolean) As CodeMemberProperty
            Dim CodeProperty As New CodeMemberProperty With {
                .Attributes = SettingsPropertyVisibility,
                .Name = Instance.Name
            }
            Dim fxTypeName As String = SettingTypeNameResolutionService.PersistedSettingTypeNameToFxTypeName(Instance.SettingTypeName)

            CodeProperty.Type = New CodeTypeReference(fxTypeName) With {
                .Options = CodeTypeReferenceOptions.GlobalReference
            }

            CodeProperty.HasGet = True
            CodeProperty.GetStatements.AddRange(GenerateGetterStatements(Instance, CodeProperty.Type))

            ' At runtime, we currently only generate setters for User scoped settings.
            ' At designtime, however, consumers of the global settings class may have to set application
            ' scoped settings (i.e. connection strings, settings bound to properties on user controls and so on)
            If IsDesignTime OrElse Instance.Scope <> DesignTimeSettingInstance.SettingScope.Application Then
                CodeProperty.HasSet = True
                CodeProperty.SetStatements.AddRange(GenerateSetterStatements(Instance))
            End If

            ' Make sure we have a CustomAttributes collection!
            CodeProperty.CustomAttributes = New CodeAttributeDeclarationCollection
            ' Add scope attribute
            Dim ScopeAttribute As CodeAttributeDeclaration
            If Instance.Scope = DesignTimeSettingInstance.SettingScope.User Then
                ScopeAttribute = New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.UserScopedSettingAttribute)))
            Else
                ScopeAttribute = New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.ApplicationScopedSettingAttribute)))
            End If
            CodeProperty.CustomAttributes.Add(ScopeAttribute)

            If Instance.Provider <> "" Then
                Dim attr As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.SettingsProviderAttribute)))
                attr.Arguments.Add(New CodeAttributeArgument(New CodeTypeOfExpression(Instance.Provider)))
                CodeProperty.CustomAttributes.Add(attr)
            End If

            If Instance.Description <> "" Then
                Dim attr As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.SettingsDescriptionAttribute)))
                attr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpression(Instance.Description)))
                CodeProperty.CustomAttributes.Add(attr)

                CodeProperty.Comments.Add(New CodeCommentStatement(DocCommentSummaryStart, True))
                CodeProperty.Comments.Add(New CodeCommentStatement(Security.SecurityElement.Escape(Instance.Description), True))
                CodeProperty.Comments.Add(New CodeCommentStatement(DocCommentSummaryEnd, True))
            End If

            ' Add DebuggerNonUserCode attribute
            CodeProperty.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(DebuggerNonUserCodeAttribute))))

            If String.Equals(Instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                ' Add connection string attribute if this is a connection string...
                Dim SpecialSettingRefExp As New CodeTypeReferenceExpression(CreateGlobalCodeTypeReference(GetType(Configuration.SpecialSetting)))
                Dim FieldExp As New CodeFieldReferenceExpression(SpecialSettingRefExp, Configuration.SpecialSetting.ConnectionString.ToString())
                Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(FieldExp)}
                Dim ConnectionStringAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.SpecialSettingAttribute)), Parameters)
                CodeProperty.CustomAttributes.Add(ConnectionStringAttribute)
            ElseIf String.Equals(Instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                ' Add web reference attribute if this is a web reference...
                Dim SpecialSettingRefExp As New CodeTypeReferenceExpression(CreateGlobalCodeTypeReference(GetType(Configuration.SpecialSetting)))
                Dim FieldExp As New CodeFieldReferenceExpression(SpecialSettingRefExp, Configuration.SpecialSetting.WebServiceUrl.ToString())
                Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(FieldExp)}
                Dim WebReferenceAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.SpecialSettingAttribute)), Parameters)
                CodeProperty.CustomAttributes.Add(WebReferenceAttribute)
            End If

            If Instance.GenerateDefaultValueInCode AndAlso (Instance.SerializedValue <> "" OrElse String.Equals(Instance.SettingTypeName, GetType(String).FullName, StringComparison.Ordinal)) Then
                ' Only add default value attributes for settings that actually have a value (Special-casing strings - 
                ' treat an empty serialized value as an empty string...)
                Debug.Assert(Instance.SerializedValue IsNot Nothing, "Why do we have a NULL serialized value!?")
                AddDefaultValueAttribute(CodeProperty, Instance.SerializedValue)
            End If

            ' Add SettingsManageabilityAttribute if this setting is roaming (but only if this is a USER scoped setting...
            If Instance.Roaming AndAlso Instance.Scope = DesignTimeSettingInstance.SettingScope.User Then
                AddManageabilityAttribute(CodeProperty, Configuration.SettingsManageability.Roaming)
            End If

            Return CodeProperty
        End Function

        Private Shared Sub AddDefaultValueAttribute(CodeProperty As CodeMemberProperty, Value As String)
            ' Add default value attribute
            Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(New CodePrimitiveExpression(Value))}
            Dim DefaultValueAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.DefaultSettingValueAttribute)), Parameters)
            CodeProperty.CustomAttributes.Add(DefaultValueAttribute)
        End Sub

        Private Shared Sub AddManageabilityAttribute(CodeProperty As CodeMemberProperty, Value As Configuration.SettingsManageability)
            Dim SettingsManageability As New CodeTypeReferenceExpression(CreateGlobalCodeTypeReference(GetType(Configuration.SettingsManageability)))
            Dim FieldExp As New CodeFieldReferenceExpression(SettingsManageability, Value.ToString)
            Dim Parameters() As CodeAttributeArgument = {New CodeAttributeArgument(FieldExp)}
            Dim SettingsManageabilityAttribute As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Configuration.SettingsManageabilityAttribute)), Parameters)
            CodeProperty.CustomAttributes.Add(SettingsManageabilityAttribute)
        End Sub

        ''' <summary>
        ''' Get the type of the class that our strongly typed wrapper class is supposed to inherit from
        ''' </summary>
        Friend Shared ReadOnly Property SettingsBaseClass As Type
            Get
                Return GetType(Configuration.ApplicationSettingsBase)
            End Get
        End Property

        ''' <summary>
        ''' Generate CodeDomStatements to get a setting from our base class
        ''' </summary>
        ''' <param name="Instance"></param>
        Private Shared Function GenerateGetterStatements(Instance As DesignTimeSettingInstance, SettingType As CodeTypeReference) As CodeStatementCollection
            Dim Statements As New CodeStatementCollection
            Dim Parameters() As CodeExpression = {New CodePrimitiveExpression(Instance.Name)}
            Dim IndexerStatement As New CodeIndexerExpression(New CodeThisReferenceExpression(), Parameters)
            ' Make sure we case this value to the correct type

            Dim TypeConversionStatement As New CodeCastExpression(SettingType, IndexerStatement)
            Dim ReturnStatement As New CodeMethodReturnStatement(TypeConversionStatement)

            Statements.Add(ReturnStatement)

            Return Statements
        End Function

        ''' <summary>
        ''' Generate statements to set a settings value
        ''' </summary>
        ''' <param name="Instance"></param>
        Private Shared Function GenerateSetterStatements(Instance As DesignTimeSettingInstance) As CodeStatementCollection
            Dim Statements As New CodeStatementCollection
            Dim Parameters() As CodeExpression = {New CodePrimitiveExpression(Instance.Name)}
            Dim IndexerStatement As New CodeIndexerExpression(New CodeThisReferenceExpression(), Parameters)
            ' Make sure we case this value to the correct type
            Dim AssignmentStatement As New CodeAssignStatement(IndexerStatement, New CodePropertySetValueReferenceExpression)

            Statements.Add(AssignmentStatement)
            Return Statements
        End Function

        ''' <summary>
        ''' Creates a string representation of the full-type name give the project's root-namespace, the default namespace
        ''' into which we are generating, and the name of the class
        ''' </summary>
        ''' <param name="defaultNamespace">namespace into which we are generating (may be String.Empty)</param>
        ''' <param name="typeName">the type of the settings-class we are generating</param>
        Private Shared Function GetFullTypeName(projectRootNamespace As String, defaultNamespace As String, typeName As String, isVb as Boolean) As String

            Dim fullTypeName As String = String.Empty

            If projectRootNamespace <> "" Then
                fullTypeName = projectRootNamespace & "."
            End If
            
            If defaultNamespace <> "" Then
                fullTypeName &= defaultNamespace & "."
            End If

            If isVb And Not fullTypeName.EndsWith("." + MyNamespaceName + ".") Then
                fullTypeName &= MyNamespaceName + "."
            End If
            
            Debug.Assert(typeName <> "", "we shouldn't have an empty type-name when generating a Settings class")
            fullTypeName &= typeName

            Return fullTypeName

        End Function

        ''' <summary>
        ''' Add required references to the project - currently only adding a reference to the settings base class assembly
        ''' </summary>
        ''' <param name="GenerateProgress"></param>
        Protected Overridable Sub AddRequiredReferences(GenerateProgress As IVsGeneratorProgress)
            Dim CurrentProjectItem As EnvDTE.ProjectItem = CType(GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
            If CurrentProjectItem Is Nothing Then
                Debug.Fail("Failed to get EnvDTE.ProjectItem service")
                Return
            End If

            Dim CurrentProject As VSLangProj.VSProject = CType(CurrentProjectItem.ContainingProject.Object, VSLangProj.VSProject)
            If CurrentProject Is Nothing Then
                Debug.Fail("Failed to get containing project")
                Return
            End If

            CurrentProject.References.Add(SettingsBaseClass.Assembly.GetName().Name)
        End Sub

        ''' <summary>
        ''' Adds the Module that lives in the My namespace and proffers up a Settings property which implements
        ''' My.Settings for easy access to typed-settings.
        ''' </summary>
        ''' <param name="Unit"></param>
        Private Shared Sub AddMyModule(Unit As CodeCompileUnit, projectRootNamespace As String, defaultNamespace As String, isVb as Boolean)

            Debug.Assert(Unit IsNot Nothing AndAlso Unit.Namespaces.Count = 1 AndAlso Unit.Namespaces(0).Types.Count = 1, "Expected a compile unit with a single namespace containing a single type!")

            Dim GeneratedType As CodeTypeDeclaration = Unit.Namespaces(0).Types(0)

            ' Create a field to capture whether or not we've already done the AddHandler. We can't use a
            '   CodeMemberField to output this b/c that doesn't have a way of doing #If/#End If around
            '   the field declaration. Since adding My goo is VB-specific, we don't really need to bother too
            '   much about being language-agnostic.
            '
            ' #If _MyType = "WindowsForms" Then
            '    Private Shared addedHandler As Boolean
            ' #End If
            '
            Dim AutoSaveCode As New CodeSnippetTypeMember With {
                .Text =
                String.Format(HideAutoSaveRegionBegin, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_SFG_AutoSaveRegionText) & Environment.NewLine &
                MyTypeWinFormsDefineConstant_If & Environment.NewLine &
                "    Private Shared " & AddedHandlerFieldName & " As Boolean" & Environment.NewLine &
                Environment.NewLine &
                "    Private Shared " & AddedHandlerLockObjectFieldName & " As New Object" & Environment.NewLine &
                Environment.NewLine &
                "    <Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _" & Environment.NewLine &
                "    Private Shared Sub " & AutoSaveSubName & "(sender As Global.System.Object, e As Global.System.EventArgs)" & Environment.NewLine &
                "        If My.Application.SaveMySettingsOnExit Then" & Environment.NewLine &
                "            " & MyNamespaceName & "." & MySettingsPropertyName & ".Save()" & Environment.NewLine &
                "        End If" & Environment.NewLine &
                "    End Sub" & Environment.NewLine &
                MyTypeWinFormsDefineConstant_EndIf & Environment.NewLine &
                HideAutoSaveRegionEnd
            }

            GeneratedType.Members.Add(AutoSaveCode)

            ' Create a namespace named My
            '
            Dim MyNamespace As New CodeNamespace(MyNamespaceName)

            ' Create a property named Settings
            '
            Dim SettingProperty As New CodeMemberProperty With {
                .Name = MySettingsPropertyName,
                .HasGet = True,
                .HasSet = False
            }

            Dim fullTypeReference As CodeTypeReference = New CodeTypeReference(GetFullTypeName(projectRootNamespace, defaultNamespace, GeneratedType.Name, isVb)) With {
                .Options = CodeTypeReferenceOptions.GlobalReference
            }
            SettingProperty.Type = fullTypeReference
            SettingProperty.Attributes = MemberAttributes.Assembly Or MemberAttributes.Final

            'TODO: Once CodeDom supports putting attributes on the individual Getter and Setter, we should
            '   mark this property as DebuggerNonUserCode. In Whidbey, CodeDom doesn't offer a way to put
            '   attributes on the getter and setter. It only offers attributes on the property itself which
            '   doesn't work for the debugger.
            '
            'SettingProperty.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Diagnostics.DebuggerNonUserCodeAttribute))))

            ' Also add the help keyword attribute
            '
            Dim helpKeywordAttr As New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(Design.HelpKeywordAttribute)))
            helpKeywordAttr.Arguments.Add(New CodeAttributeArgument(New CodePrimitiveExpression(HelpIDs.MySettingsHelpKeyword)))
            SettingProperty.CustomAttributes.Add(helpKeywordAttr)

            Dim MethodInvokeExpr As New CodeMethodInvokeExpression(New CodeTypeReferenceExpression(SettingProperty.Type), DefaultInstancePropertyName, Array.Empty(Of CodeExpression))
            SettingProperty.GetStatements.Add(New CodeMethodReturnStatement(MethodInvokeExpr))

            ' Create a Module
            '
            '   <Global.Microsoft.VisualBasic.HideModuleNameAttribute(),  _
            '    Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), _
            '    Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
            '   Module MySettingsProperty
            '        
            Dim ModuleDecl As New CodeTypeDeclaration(MySettingsModuleName)
            ModuleDecl.UserData("Module") = True
            ModuleDecl.TypeAttributes = TypeAttributes.Sealed Or TypeAttributes.NestedAssembly
            ModuleDecl.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(HideModuleNameAttribute))))
            ModuleDecl.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(DebuggerNonUserCodeAttribute))))
            ' add the CompilerGeneratedAttribute in order to support deploying VB apps in Yukon (where
            '   our shared/static fields make the code not "safe" according to Yukon's measure of what
            '   it means to deploy a safe assembly. VSWhidbey 320692.
            ModuleDecl.CustomAttributes.Add(New CodeAttributeDeclaration(CreateGlobalCodeTypeReference(GetType(System.Runtime.CompilerServices.CompilerGeneratedAttribute))))

            ModuleDecl.Members.Add(SettingProperty)

            ' add the Module to the My namespace
            '
            MyNamespace.Types.Add(ModuleDecl)

            ' Add the My namespace to the CodeCompileUnit
            '
            Unit.Namespaces.Add(MyNamespace)
        End Sub
#End Region

        ''' <summary>
        ''' Gets the default-namespace for the project containing the .settings file for which
        ''' we are currently generating. This will include the root-namespace for VB even though
        ''' we would not have been passed in that namespace in the call to Generate.
        ''' </summary>
        Protected Overridable Function GetProjectRootNamespace() As String

            Dim rootNamespace As String = String.Empty

            Try
                Dim punkVsBrowseObject As IntPtr
                Dim vsBrowseObjectGuid As Guid = GetType(IVsBrowseObject).GUID

                ' first, we need to get IVsBrowseObject from our site
                '
                GetSite(vsBrowseObjectGuid, punkVsBrowseObject)

                Try
                    If punkVsBrowseObject <> IntPtr.Zero Then

                        Dim vsBrowseObject As IVsBrowseObject = TryCast(Marshal.GetObjectForIUnknown(punkVsBrowseObject), IVsBrowseObject)
                        Debug.Assert(vsBrowseObject IsNot Nothing, "Generator invoked by Site that is not IVsBrowseObject?")

                        If vsBrowseObject IsNot Nothing Then
                            Dim vsHierarchy As IVsHierarchy = Nothing
                            Dim itemid As UInteger = 0

                            ' use the IVsBrowseObject to get the hierarchy/itemid for the .settings file
                            '   from which are generating
                            '
                            VSErrorHandler.ThrowOnFailure(vsBrowseObject.GetProjectItem(vsHierarchy, itemid))

                            Debug.Assert(vsHierarchy IsNot Nothing, "GetProjectItem should have thrown or returned a valid IVsHierarchy")
                            Debug.Assert(itemid <> VSITEMID.NIL, "GetProjectItem should have thrown or returned a valid VSITEMID")

                            If (vsHierarchy IsNot Nothing) AndAlso (itemid <> VSITEMID.NIL) Then

                                Dim obj As Object = Nothing

                                ' get the default-namespace of the root node which will be the project's root namespace
                                '
                                VSErrorHandler.ThrowOnFailure(vsHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_DefaultNamespace, obj))

                                Dim o = TryCast(obj, String)
                                If o IsNot Nothing Then
                                    ' now we finally have the default-namespace
                                    '
                                    rootNamespace = o
                                Else
                                    Debug.Fail("DefaultNamespace didn't return a string?")
                                End If
                            End If
                        End If
                    End If
                Finally
                    If punkVsBrowseObject <> IntPtr.Zero Then
                        Marshal.Release(punkVsBrowseObject)
                    End If
                End Try
            Catch ex As Exception When Common.ReportWithoutCrash(ex, "Failed to get the DefaultNamespace", NameOf(SettingsSingleFileGenerator))
            End Try

            Return rootNamespace
        End Function

        ''' <summary>
        ''' Is this the "default" settings file
        ''' </summary>
        ''' <param name="FilePath">Fully qualified path of file to check</param>
        Private Function IsDefaultSettingsFile(FilePath As String) As Boolean
            Dim Hierarchy As IVsHierarchy = DirectCast(GetService(GetType(IVsHierarchy)), IVsHierarchy)
            If Hierarchy Is Nothing Then
                Debug.Fail("Failed to get Hierarchy for file to generate code from...")
                Return False
            End If

            Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            If SpecialProjectItems Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from project")
                Return False
            End If

            Dim DefaultSettingsItemId As UInteger
            Dim DefaultSettingsFilePath As String = Nothing

            Dim hr As Integer = SpecialProjectItems.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), DefaultSettingsItemId, DefaultSettingsFilePath)
            If NativeMethods.Succeeded(hr) Then
                If DefaultSettingsItemId <> VSITEMID.NIL Then
                    Dim NormalizedDefaultSettingFilePath As String = Path.GetFullPath(DefaultSettingsFilePath)
                    Dim NormalizedSettingFilePath As String = Path.GetFullPath(FilePath)
                    Return String.Equals(NormalizedDefaultSettingFilePath, NormalizedSettingFilePath, StringComparison.Ordinal)
                End If
            Else
                ' Something went wrong when we tried to get the special file name. This could be because there is a directory
                ' with the same name as the default settings file would have had if it existed.
                ' Anyway, since the project system can't find the default settings file name, this can't be it!
            End If
            Return False
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
        ''' Demand-create service provider from my site
        ''' </summary>
        Private ReadOnly Property ServiceProvider As ServiceProvider
            Get
                If _serviceProvider Is Nothing AndAlso _site IsNot Nothing Then
                    Dim OleSp As IServiceProvider = CType(_site, IServiceProvider)
                    _serviceProvider = New ServiceProvider(OleSp)
                End If
                Return _serviceProvider
            End Get
        End Property

        ''' <summary>
        ''' Create a CodeTypeReference instance with the GlobalReference option set.
        ''' </summary>
        ''' <param name="type"></param>
        Private Shared Function CreateGlobalCodeTypeReference(type As Type) As CodeTypeReference
            Dim ctr As New CodeTypeReference(type) With {
                .Options = CodeTypeReferenceOptions.GlobalReference
            }
            Return ctr
        End Function

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
            ClearCachedServices()
        End Sub

        Private Sub ClearCachedServices()
            _serviceProvider = Nothing
            _codeDomProvider = Nothing
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
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeGlobalSymbolRenamed(phier As IVsHierarchy, itemId As UInteger, cRQNames As UInteger, rglpszRQName() As String, lpszNewName As String, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeGlobalSymbolRenamed
            prgAdditionalCheckoutVSITEMIDS = Nothing

            Dim isRootNamespaceRename As Boolean = RenamingHelper.IsRootNamespaceRename(phier, cRQNames, rglpszRQName, lpszNewName)

            If AllowSymbolRename Or isRootNamespaceRename Then
                If isRootNamespaceRename Then
                    ' We need to tell all settings global object to update the default namespace as well... 
                    ' if we don't do this, they will have the old namespace cached and anyone who asks for a 
                    ' virtual type will get a type with a bogus namespace...
                    Dim sp As ServiceProvider = Common.ServiceProviderFromHierarchy(phier)
                    Dim proj As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(phier)
                    Dim objectService As Design.GlobalObjectService = New Design.GlobalObjectService(sp, proj, GetType(Design.Serialization.CodeDomSerializer))
                    If objectService IsNot Nothing Then
                        Dim objectCollection As Design.GlobalObjectCollection = objectService.GetGlobalObjects(GetType(Configuration.ApplicationSettingsBase))
                        If objectCollection IsNot Nothing Then
                            ' Note: We are currently calling refresh on all settings global objects for each
                            '   refactor notify, which effectively makes this an O(n^2) operation where n is the
                            '   number of .settings files in the project. We are OK with this because:
                            '   a) We don't expect users to have too many .settings files
                            '   b) Once we have retreived the settings global object, it will be cached
                            '      and the retreival is just a hash table lookup.
                            '   c) The Refresh is a cheap operation
                            For Each gob As Design.GlobalObject In objectCollection
                                Dim sgob As SettingsGlobalObjects.SettingsFileGlobalObject = TryCast(gob, SettingsGlobalObjects.SettingsFileGlobalObject)
                                If sgob IsNot Nothing Then
                                    sgob.Refresh()
                                End If
                            Next
                        End If
                    End If
                End If
                Return NativeMethods.S_OK
            Else
                Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_RenameNotSupported)
                ' Always return an error code to disable renaming of generated code
                Return NativeMethods.E_NOTIMPL
            End If
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
            'VSWhidbey #452759: Always return S_OK in OnGlobalSymbolRenamed.
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
#End Region

    End Class
End Namespace
