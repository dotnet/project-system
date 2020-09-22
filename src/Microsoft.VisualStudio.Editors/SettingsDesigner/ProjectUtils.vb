' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.CodeDom
Imports System.CodeDom.Compiler
Imports System.IO
Imports System.Reflection

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner.ProjectUtils
    Friend Module ProjectUtils

        ''' <summary>
        ''' Get the file name from a project item. 
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <remarks>If the item contains of multiple files, the first one is returned</remarks>
        Friend Function FileName(ProjectItem As EnvDTE.ProjectItem) As String
            If ProjectItem Is Nothing Then
                Debug.Fail("Can't get file name for NULL project item!")
                Throw New ArgumentNullException()
            End If

            If ProjectItem.FileCount <= 0 Then
                Debug.Fail("No file associated with ProjectItem (filecount <= 0)")
                Return Nothing
            End If

            ' The ProjectItem.FileNames collection is 1 based...
            Return ProjectItem.FileNames(1)
        End Function

        ''' <summary>
        ''' From a hierarchy and projectitem, return the item id
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ProjectItem"></param>
        Friend Function ItemId(Hierarchy As IVsHierarchy, ProjectItem As EnvDTE.ProjectItem) As UInteger
            Dim FoundItemId As UInteger
            VSErrorHandler.ThrowOnFailure(Hierarchy.ParseCanonicalName(FileName(ProjectItem), FoundItemId))
            Return FoundItemId
        End Function

        ''' <summary>
        ''' Is the file pointed to by FullPath included in the project?
        ''' </summary>
        ''' <param name="project"></param>
        ''' <param name="FullFilePath"></param>
        Friend Function IsFileInProject(project As IVsProject, FullFilePath As String) As Boolean
            Dim found As Integer
            Dim prio(0) As VSDOCUMENTPRIORITY
            prio(0) = VSDOCUMENTPRIORITY.DP_Standard
            Dim itemId As UInteger

            VSErrorHandler.ThrowOnFailure(project.IsDocumentInProject(FullFilePath, found, prio, itemId))
            Return found <> 0
        End Function

        ''' <summary>
        ''' VB projects don't store the root namespace as part of the generated
        ''' namespace in the .settings file.
        ''' </summary>
        Friend Function PersistedNamespaceIncludesRootNamespace(Hierarchy As IVsHierarchy) As Boolean
            If Common.IsVbProject(Hierarchy) Then
                Return False
            Else
                Return True
            End If
        End Function

        ''' <summary>
        ''' From an (optionally empty) namespace and a class name, return the fully qualified classname
        ''' </summary>
        ''' <param name="Namespace"></param>
        ''' <param name="ClassName"></param>
        Friend Function FullyQualifiedClassName([Namespace] As String, ClassName As String) As String
            Dim sectionName As String

            If [Namespace] = "" Then
                sectionName = ClassName
            Else
                sectionName = String.Format(Globalization.CultureInfo.InvariantCulture, "{0}.{1}", [Namespace], ClassName)
            End If

            Return sectionName
        End Function

        ''' <summary>
        ''' Get the namespace for the generated file...
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        Friend Function GeneratedSettingsClassNamespace(Hierarchy As IVsHierarchy, ItemId As UInteger) As String
            Dim IncludeRootNamespace As Boolean = PersistedNamespaceIncludesRootNamespace(Hierarchy)
            Return GeneratedSettingsClassNamespace(Hierarchy, ItemId, IncludeRootNamespace)
        End Function

        ''' <summary>
        ''' Get the namespace for the generated file...
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="IncludeRootNamespace"></param>
        Friend Function GeneratedSettingsClassNamespace(Hierarchy As IVsHierarchy, ItemId As UInteger, IncludeRootNamespace As Boolean) As String
            Return Common.GeneratedCodeNamespace(Hierarchy, ItemId, IncludeRootNamespace, True)
        End Function

        ''' <summary>
        ''' Is the specified ProjectItem the default settings file for the project?
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="Item"></param>
        Friend Function IsDefaultSettingsFile(Hierarchy As IVsHierarchy, Item As EnvDTE.ProjectItem) As Boolean
            If Hierarchy Is Nothing Then
                Debug.Fail("Can't get the special files from a NULL Hierarchy!")
                Throw New ArgumentNullException()
            End If

            If Item Is Nothing Then
                Debug.Fail("Shouldn't pass in NULL as Item to check if it is the default settings file!")
                Return False
            End If

            Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            If SpecialProjectItems Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from IVsHierarchy")
                Return False
            End If

            Try
                Dim DefaultSettingsItemId As UInteger
                Dim DontCarePath As String = Nothing
                VSErrorHandler.ThrowOnFailure(SpecialProjectItems.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), DefaultSettingsItemId, DontCarePath))
                Return DefaultSettingsItemId = ItemId(Hierarchy, Item)
            Catch ex As System.Runtime.InteropServices.COMException
                ' Something went wrong when we tried to get the special file name. This could be because there is a directory
                ' with the same name as the default settings file would have had if it existed.
                ' Anyway, since the project system can't find the default settings file name, this can't be it!
            End Try
            Return False

        End Function

        ''' <summary>
        ''' Open a document that contains a class that expands the generated settings class, creating a new
        ''' document if one doesn't already exist!
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ProjectItem"></param>
        ''' <param name="CodeProvider"></param>
        Friend Sub OpenAndMaybeAddExtendingFile(ClassName As String, SuggestedFileName As String, sp As IServiceProvider, Hierarchy As IVsHierarchy, ProjectItem As EnvDTE.ProjectItem, CodeProvider As CodeDomProvider, View As DesignerFramework.BaseDesignerView)
            Dim SettingClassElement As EnvDTE.CodeElement = FindElement(ProjectItem, False, True, New KnownClassName(ClassName))

            Dim cc2 As EnvDTE80.CodeClass2 = TryCast(SettingClassElement, EnvDTE80.CodeClass2)
            If cc2 Is Nothing Then
                Debug.Fail("Failed to get CodeClass2 to extend!")
                Return
            End If

            ' Find all classes that extend this class
            Dim ExtendingItem As EnvDTE.ProjectItem = Nothing
            Dim MainSettingsItemId As UInteger = ItemId(Hierarchy, cc2.ProjectItem)
            Try
                Dim pcs As EnvDTE.CodeElements = cc2.Parts()
                For ItemNo As Integer = 1 To pcs.Count
                    Dim ExpandingClass As EnvDTE80.CodeClass2 = TryCast(pcs.Item(ItemNo), EnvDTE80.CodeClass2)
                    If ExpandingClass IsNot Nothing Then
                        If ItemId(Hierarchy, ExpandingClass.ProjectItem) <> MainSettingsItemId Then
                            ExtendingItem = ExpandingClass.ProjectItem
                            Exit For
                        End If
                    End If
                Next
            Catch ex As NotImplementedException
                ' BUG VsWhidbey 204348 - PartialClasses property not implemented for VB CodeModel!
            End Try

            ' "Manually" find the classes that extend this class
            If ExtendingItem Is Nothing Then
                Dim ExpandingClass As EnvDTE.CodeElement
                ExpandingClass = FindElement(Common.DTEUtils.EnvDTEProject(Hierarchy), False, True, New ExpandsKnownClass(cc2))
                If ExpandingClass IsNot Nothing Then
                    ExtendingItem = ExpandingClass.ProjectItem
                End If
            End If

            If ExtendingItem Is Nothing Then
                ' Since we didn't find an existing item that extends the specified class, we
                ' better create a new item...

                ' But before adding a new item, we need to make sure that the project file is editable...
                If View IsNot Nothing AndAlso ProjectItem IsNot Nothing AndAlso ProjectItem.ContainingProject IsNot Nothing Then
                    View.EnterProjectCheckoutSection()
                    Try
                        Dim sccmgr As New DesignerFramework.SourceCodeControlManager(sp, Hierarchy)
                        sccmgr.ManageFile(ProjectItem.ContainingProject.FullName)
                        sccmgr.EnsureFilesEditable()

                        If View.ProjectReloadedDuringCheckout Then
                            ' We need to bail ASAP if the project was reloaded during checkout - this will have brought down a new version of
                            ' the project file and potentially a file containing the user part of the settings class....
                            '
                            ' The user will see this as if nothing happened the first time they clicked on ViewCode, and they will hopefully 
                            ' try again...
                            Return
                        End If
                    Finally
                        View.LeaveProjectCheckoutSection()
                    End Try
                End If

                Dim vsproj As IVsProject = TryCast(Hierarchy, IVsProject)
                Dim ParentId As UInteger
                Dim CollectionToAddTo As EnvDTE.ProjectItems
                Dim NewFilePath As String

                If IsDefaultSettingsFile(Hierarchy, ProjectItem) Then
                    ParentId = VSITEMID.ROOT
                    CollectionToAddTo = ProjectItem.ContainingProject.ProjectItems
                    NewFilePath = DirectCast(ProjectItem.ContainingProject.Properties.Item("FullPath").Value, String)
                Else
                    ParentId = ItemId(Hierarchy, ProjectItem)
                    CollectionToAddTo = ProjectItem.Collection
                    NewFilePath = IO.Path.GetDirectoryName(FileName(ProjectItem))
                End If

                If Not (NewFilePath.EndsWith(IO.Path.DirectorySeparatorChar) OrElse NewFilePath.EndsWith(IO.Path.AltDirectorySeparatorChar)) Then
                    NewFilePath &= IO.Path.DirectorySeparatorChar
                End If

                Dim NewItemName As String
                If SuggestedFileName <> "" Then
                    NewItemName = SuggestedFileName & "." & CodeProvider.FileExtension
                Else
                    NewItemName = cc2.Name & "." & CodeProvider.FileExtension
                End If

                If IsFileInProject(vsproj, NewFilePath & NewItemName) Then
                    VSErrorHandler.ThrowOnFailure(vsproj.GenerateUniqueItemName(ParentId, "." & CodeProvider.FileExtension, IO.Path.GetFileNameWithoutExtension(NewItemName), NewItemName))
                End If
                ' CONSIDER: Using different mechanism to figure out if this is VB than checking the file extension...
                Dim supportsDeclarativeEventHandlers As Boolean = CodeProvider.FileExtension.Equals("vb", StringComparison.OrdinalIgnoreCase)
                ExtendingItem = AddNewProjectItemExtendingClass(cc2, NewFilePath & NewItemName, CodeProvider, supportsDeclarativeEventHandlers, CollectionToAddTo)
            End If

            Debug.Assert(ExtendingItem IsNot Nothing, "Couldn't find/create a class that extends the generated settings class")

            If ExtendingItem IsNot Nothing Then
                If ExtendingItem.IsOpen AndAlso ExtendingItem.Document IsNot Nothing Then
                    ExtendingItem.Document.Activate()
                Else
                    Dim Win As EnvDTE.Window = ExtendingItem.Open()
                    If Win IsNot Nothing Then
                        Win.SetFocus()
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        ''' Create a new file, adding a class that extends the generated settings class
        ''' </summary>
        ''' <param name="cc2">CodeClass2 to extend</param>
        ''' <param name="NewFilePath">Fully specified name and path for new file</param>
        ''' <param name="Generator">Code generator to use to generate the code</param>
        ''' <param name="CollectionToAddTo"></param>
        Private Function AddNewProjectItemExtendingClass(cc2 As EnvDTE80.CodeClass2, NewFilePath As String, Generator As CodeDomProvider, supportsDeclarativeEventHandlers As Boolean, Optional CollectionToAddTo As EnvDTE.ProjectItems = Nothing) As EnvDTE.ProjectItem
            If cc2 Is Nothing Then
                Debug.Fail("CodeClass2 isntance to extend can't be NULL!")
                Throw New ArgumentNullException()
            End If

            If NewFilePath Is Nothing Then
                Debug.Fail("NewFilePath can't be nothing!")
                Throw New ArgumentNullException()
            End If

            If Generator Is Nothing Then
                Debug.Fail("Can't create a new file with a NULL CodeDomProvider")
                Throw New ArgumentNullException()
            End If

            Dim AddTo As EnvDTE.ProjectItems
            If CollectionToAddTo IsNot Nothing Then
                ' If we are given a specific ProjectItems collection to add the new
                ' item to, make sure we do so!
                AddTo = CollectionToAddTo
            Else
                ' Otherwise, we'll add the item to the class to expand's containing
                ' project (at the root level)
                AddTo = cc2.ProjectItem.ContainingProject.ProjectItems
            End If

            Debug.Assert(AddTo IsNot Nothing, "Must have a project items collection to add new item to!")

            ' Create new document...
            Using Writer As New StreamWriter(NewFilePath, False, System.Text.Encoding.UTF8)
                Dim ExtendingNamespace As CodeNamespace = Nothing
                If cc2.Namespace IsNot Nothing Then
                    Debug.Assert(cc2.Namespace.FullName IsNot Nothing, "Couldn't get a FullName from the CodeClass2.Namespace!?")
                    If String.Equals(cc2.Language, EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB, StringComparison.OrdinalIgnoreCase) Then
                        Dim rootNamespace As String = ""
                        Try
                            Dim projProp As EnvDTE.Property = cc2.ProjectItem.ContainingProject.Properties.Item("RootNamespace")
                            If projProp IsNot Nothing Then
                                rootNamespace = CStr(projProp.Value)
                            End If
                        Catch ex As Exception When Common.ReportWithoutCrash(ex, "Failed to get root namespace to remove from class name", NameOf(ProjectUtils))
                        End Try
                        ExtendingNamespace = New CodeNamespace(Common.RemoveRootNamespace(cc2.Namespace.FullName, rootNamespace))
                    Else
                        ExtendingNamespace = New CodeNamespace(cc2.Namespace.FullName)
                    End If
                End If

                Dim ExtendingType As New CodeTypeDeclaration(cc2.Name) With {
                    .TypeAttributes = CodeModelToCodeDomTypeAttributes(cc2),
                    .IsPartial = True
                }

                ExtendingType.Comments.Add(New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_COMMON1))
                ExtendingType.Comments.Add(New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_COMMON2))
                ExtendingType.Comments.Add(New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_COMMON3))
                ExtendingType.Comments.Add(New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_COMMON4))
                ExtendingType.Comments.Add(New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_COMMON5))
                If Not supportsDeclarativeEventHandlers Then
                    GenerateExtendingClassInstructions(ExtendingType, Generator)
                End If

                If ExtendingNamespace IsNot Nothing Then
                    ExtendingNamespace.Types.Add(ExtendingType)
                    Generator.GenerateCodeFromNamespace(ExtendingNamespace, Writer, Nothing)
                Else
                    Generator.GenerateCodeFromType(ExtendingType, Writer, Nothing)
                End If
                Writer.Flush()
                Writer.Close()
            End Using

            Return AddTo.AddFromFileCopy(NewFilePath)
        End Function

        Friend Interface IFindFilter
            Function IsMatch(Element As EnvDTE.CodeElement) As Boolean
        End Interface

        ''' <summary>
        ''' Indicates whether to search for a class or a module or either
        ''' </summary>
        Friend Enum ClassOrModule
            ClassOnly
            ModuleOnly
            Either
        End Enum

        ''' <summary>
        ''' Look for a CodeClass with a known name in the project
        ''' </summary>
        Friend Class KnownClassName
            Implements IFindFilter

            Private ReadOnly _className As String
            Private ReadOnly _classOrModule As ClassOrModule

            Friend Sub New(ClassName As String, Optional ClassOrModule As ClassOrModule = ClassOrModule.ClassOnly)
                _className = ClassName
                _classOrModule = ClassOrModule
            End Sub

            Public Function IsMatch(Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                Select Case _classOrModule
                    Case ClassOrModule.ClassOnly
                        If Element.Kind <> EnvDTE.vsCMElement.vsCMElementClass Then
                            Return False
                        End If
                    Case ClassOrModule.Either
                        If Element.Kind <> EnvDTE.vsCMElement.vsCMElementClass AndAlso Element.Kind <> EnvDTE.vsCMElement.vsCMElementModule Then
                            Return False
                        End If
                    Case ClassOrModule.ModuleOnly
                        If Element.Kind <> EnvDTE.vsCMElement.vsCMElementModule Then
                            Return False
                        End If
                    Case Else
                        Debug.Fail("Unexpected case")
                End Select

                Return _className.Equals(Element.FullName, StringComparison.Ordinal)
            End Function
        End Class

        ''' <summary>
        ''' Filter that finds a class expanding a known class specified by a CodeClass2 instance
        ''' </summary>
        Private Class ExpandsKnownClass
            Implements IFindFilter

            ''' <summary>
            ''' The class to expand
            ''' </summary>
            Private ReadOnly _classToExpand As EnvDTE80.CodeClass2

            Friend Sub New(ClassToExpand As EnvDTE80.CodeClass2)
                If ClassToExpand Is Nothing Then
                    Debug.Fail("Can't find a class that expands a NULL class...")
                    Throw New ArgumentNullException()
                End If
                _classToExpand = ClassToExpand
            End Sub

            Public Function IsMatch(Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                If Element.Kind = EnvDTE.vsCMElement.vsCMElementClass AndAlso
                    (Not FileName(_classToExpand.ProjectItem).Equals(FileName(Element.ProjectItem), StringComparison.Ordinal)) AndAlso
                    _classToExpand.FullName.Equals(Element.FullName) _
                Then
                    Dim cc2 As EnvDTE80.CodeClass2 = TryCast(Element, EnvDTE80.CodeClass2)
                    If cc2 IsNot Nothing AndAlso cc2.DataTypeKind = EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindPartial Then
                        Return True
                    End If
                End If
                Return False
            End Function
        End Class

        ''' <summary>
        ''' Find a CodeElement representing a property with a known name in a known class
        ''' </summary>
        Friend Class FindPropertyFilter
            Implements IFindFilter

            Private ReadOnly _containingClass As EnvDTE.CodeElement
            Private ReadOnly _propertyName As String

            Public Sub New(ContainingClass As EnvDTE.CodeElement, PropertyName As String)
                If ContainingClass Is Nothing Then
                    Debug.Fail("Can't find property in unknown class!")
                    Throw New ArgumentNullException()
                End If

                If PropertyName Is Nothing Then
                    Debug.Fail("Can't find property without a property name!")
                    Throw New ArgumentNullException()
                End If

                _containingClass = ContainingClass
                _propertyName = PropertyName
            End Sub

            Public Function IsMatch(Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                If Element.Kind <> EnvDTE.vsCMElement.vsCMElementProperty Then
                    Return False
                End If

                Dim comparisonType As StringComparison
                If Element.ProjectItem IsNot Nothing AndAlso
                    Element.ProjectItem.ContainingProject IsNot Nothing _
                    AndAlso Not Element.ProjectItem.ContainingProject.CodeModel.IsCaseSensitive Then
                    'BEGIN
                    comparisonType = StringComparison.OrdinalIgnoreCase
                Else
                    comparisonType = StringComparison.Ordinal
                End If

                If Not Element.Name.Equals(_propertyName, comparisonType) Then
                    Return False
                End If

                Dim Prop As EnvDTE.CodeProperty = TryCast(Element, EnvDTE.CodeProperty)
                Debug.Assert(Prop IsNot Nothing, "Failed to get EnvDTE.CodeProperty from element with kind = vsCMElementProperty!?")
                If Prop.Parent Is Nothing Then
                    Return False
                End If

                If Prop.Parent.FullName.Equals(_containingClass.FullName, comparisonType) Then
                    Return True
                Else
                    Return False
                End If
            End Function
        End Class

        ''' <summary>
        ''' Find a CodeElement representing a method with a known name in a known class
        ''' </summary>
        Friend Class FindFunctionFilter
            Implements IFindFilter

            Private ReadOnly _containingClass As EnvDTE.CodeElement
            Private ReadOnly _functionName As String

            Public Sub New(ContainingClass As EnvDTE.CodeElement, FunctionName As String)
                If ContainingClass Is Nothing Then
                    Debug.Fail("Can't find property in unknown class!")
                    Throw New ArgumentNullException()
                End If

                If FunctionName Is Nothing Then
                    Debug.Fail("Can't find property without a property name!")
                    Throw New ArgumentNullException()
                End If

                _containingClass = ContainingClass
                _functionName = FunctionName
            End Sub

            ''' <summary>
            ''' Check whether a code element meets our requirement.
            ''' </summary>
            Public Function IsMatch(Element As EnvDTE.CodeElement) As Boolean Implements IFindFilter.IsMatch
                If Element.Kind <> EnvDTE.vsCMElement.vsCMElementFunction Then
                    Return False
                End If

                ' Check name first...
                Dim comparisonType As StringComparison
                If Element.ProjectItem IsNot Nothing AndAlso
                    Element.ProjectItem.ContainingProject IsNot Nothing _
                    AndAlso Not Element.ProjectItem.ContainingProject.CodeModel.IsCaseSensitive Then
                    'BEGIN
                    comparisonType = StringComparison.OrdinalIgnoreCase
                Else
                    comparisonType = StringComparison.Ordinal
                End If

                If Not Element.Name.Equals(_functionName, comparisonType) Then
                    Return False
                End If

                ' check containing class...
                Dim Func As EnvDTE.CodeFunction = TryCast(Element, EnvDTE.CodeFunction)
                Debug.Assert(Func IsNot Nothing, "Failed to get EnvDTE.CodeFunction from element with kind = vsCMElementFunction!?")
                If Func.Parent Is Nothing Then
                    Return False
                End If

                Dim ContainingClass As EnvDTE.CodeClass = TryCast(Func.Parent, EnvDTE.CodeClass)
                If ContainingClass IsNot Nothing AndAlso ContainingClass.FullName.Equals(_containingClass.FullName, comparisonType) Then
                    Return True
                Else
                    Return False
                End If
            End Function
        End Class

        ''' <summary>
        ''' Find the first CodeElement in the project that satisfies the given filter
        ''' </summary>
        ''' <param name="Project">The project to search in</param>
        ''' <param name="ExpandChildElements">If we should loop through the child CodeElements of types</param>
        ''' <param name="ExpandChildItems">If we should recurse to ProjectItem children</param>
        ''' <param name="Filter">The IFilter to satisfy</param>
        ''' <returns>The found element, NULL if no matching element found</returns>
        Friend Function FindElement(Project As EnvDTE.Project, ExpandChildElements As Boolean, ExpandChildItems As Boolean, Filter As IFindFilter) As EnvDTE.CodeElement
            For Each Item As EnvDTE.ProjectItem In Project.ProjectItems
                Dim Result As EnvDTE.CodeElement = FindElement(Item, ExpandChildElements, ExpandChildItems, Filter)
                If Result IsNot Nothing Then
                    Return Result
                End If
            Next
            Return Nothing
        End Function

        ''' <summary>
        ''' Find the first CodeElement int the ProjectItem's FileCodeModel that satisfies the given filter
        ''' </summary>
        ''' <param name="ProjectItem">The project to search in</param>
        ''' <param name="ExpandChildElements">If we should loop through the child CodeElements of types</param>
        ''' <param name="ExpandChildItems">If we should recurse to ProjectItem children</param>
        ''' <param name="Filter">The IFilter to satisfy</param>
        ''' <returns>The found element, NULL if no matching element found</returns>
        Friend Function FindElement(ProjectItem As EnvDTE.ProjectItem, ExpandChildElements As Boolean, ExpandChildItems As Boolean, Filter As IFindFilter) As EnvDTE.CodeElement
            If ProjectItem.FileCodeModel IsNot Nothing Then
                For Each Element As EnvDTE.CodeElement In ProjectItem.FileCodeModel.CodeElements
                    Dim Result As EnvDTE.CodeElement = FindElement(Element, ExpandChildElements, Filter)
                    If Result IsNot Nothing Then
                        Return Result
                    End If
                Next
            End If

            If ExpandChildItems AndAlso ProjectItem.ProjectItems IsNot Nothing Then
                For Each ChildItem As EnvDTE.ProjectItem In ProjectItem.ProjectItems
                    Dim Result As EnvDTE.CodeElement = FindElement(ChildItem, ExpandChildElements, ExpandChildItems, Filter)
                    If Result IsNot Nothing Then
                        Return Result
                    End If
                Next
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Find the first element that satisfies the Filters IsMatch function
        ''' </summary>
        ''' <param name="Element">The element to check</param>
        ''' <param name="ExpandChildren">If we want to recurse through this elements children</param>
        ''' <param name="Filter">The filter to satisfy</param>
        Private Function FindElement(Element As EnvDTE.CodeElement, ExpandChildren As Boolean, Filter As IFindFilter) As EnvDTE.CodeElement
            If Filter.IsMatch(Element) Then
                Return Element
            End If

            ' We only expand code elements if it is a Namespace OR we are explicitly told to do so...
            Dim ShouldExpand As Boolean = ExpandChildren OrElse Element.Kind = EnvDTE.vsCMElement.vsCMElementNamespace
            If ShouldExpand Then
                Dim Children As EnvDTE.CodeElements = Nothing
                If Element.IsCodeType Then
                    If Element.Kind <> EnvDTE.vsCMElement.vsCMElementDelegate Then
                        Children = CType(Element, EnvDTE.CodeType).Members
                    End If
                ElseIf Element.Kind = EnvDTE.vsCMElement.vsCMElementNamespace Then
                    Children = CType(Element, EnvDTE.CodeNamespace).Members
                End If

                ' If we found children, let's iterate through these as well to find 
                ' any potential matches...
                If Children IsNot Nothing Then
                    For Each ChildElement As EnvDTE.CodeElement In Children
                        Dim Result As EnvDTE.CodeElement = FindElement(ChildElement, ExpandChildren, Filter)
                        If Result IsNot Nothing Then
                            Return Result
                        End If
                    Next
                End If
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' If the generated language doesn't support declarative event handlers, we
        ''' add a stub to make sure the user isn't totally lost when they are presented
        ''' with the user code...
        ''' </summary>
        ''' <param name="ct"></param>
        ''' <param name="generator"></param>
        Private Sub GenerateExtendingClassInstructions(ct As CodeTypeDeclaration, generator As CodeDomProvider)
            Const SettingChangingEventName As String = "SettingChanging"
            Const SettingsSavingEventName As String = "SettingsSaving"

            Const SettingChangingEventHandlerName As String = "SettingChangingEventHandler"
            Const SettingsSavingEventHandlerName As String = "SettingsSavingEventHandler"

            ' Add constructor
            Dim constructor As New CodeConstructor With {
                .Attributes = MemberAttributes.Public
            }

            ' Generate a series of statements to add to the constructor
            Dim thisExpr As New CodeThisReferenceExpression()
            Dim statements As New CodeStatementCollection From {
                New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_HOWTO_ATTACHEVTS),
                New CodeAttachEventStatement(thisExpr, SettingChangingEventName, New CodeMethodReferenceExpression(thisExpr, SettingChangingEventHandlerName)),
                New CodeAttachEventStatement(thisExpr, SettingsSavingEventName, New CodeMethodReferenceExpression(thisExpr, SettingsSavingEventHandlerName))
            }

            For Each stmt As CodeStatement In statements
                constructor.Statements.Add(CommentStatement(stmt, generator, True))
            Next

            ' Add stubs for settingschanging/settingssaving event handlers
            Dim senderParam As New CodeParameterDeclarationExpression(GetType(Object), "sender")
            Dim changingStub As New CodeMemberMethod With {
                .Name = SettingChangingEventHandlerName,
                .ReturnType = Nothing
            }
            changingStub.Parameters.Add(senderParam)
            changingStub.Parameters.Add(New CodeParameterDeclarationExpression(GetType(Configuration.SettingChangingEventArgs), "e"))
            changingStub.Statements.Add(New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_HANDLE_CHANGING))

            Dim savingStub As New CodeMemberMethod With {
                .Name = SettingsSavingEventHandlerName,
                .ReturnType = Nothing
            }
            savingStub.Parameters.Add(senderParam)
            savingStub.Parameters.Add(New CodeParameterDeclarationExpression(GetType(ComponentModel.CancelEventArgs), "e"))
            savingStub.Statements.Add(New CodeCommentStatement(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_CODEGENCMT_HANDLE_SAVING))

            ct.Members.Add(constructor)
            ct.Members.Add(changingStub)
            ct.Members.Add(savingStub)
        End Sub

        ''' <summary>
        ''' Create a comment statement from a "normal" code statement
        ''' </summary>
        ''' <param name="statement">The statement to comment out</param>
        ''' <param name="generator"></param>
        ''' <param name="doubleCommentComments">
        ''' If "statement" is already a comment, we can choose to add another level of comments by
        ''' settings this to true
        ''' </param>
        Private Function CommentStatement(statement As CodeStatement, generator As CodeDomProvider, doubleCommentComments As Boolean) As CodeCommentStatement
            ' If this is already a comment and we don't want to double comment it, just return the statement...
            If TypeOf statement Is CodeCommentStatement AndAlso Not doubleCommentComments Then
                Return DirectCast(statement, CodeCommentStatement)
            End If

            Dim sb As New System.Text.StringBuilder
            Dim sw As New StringWriter(sb)

            generator.GenerateCodeFromStatement(statement, sw, New CodeGeneratorOptions())
            sw.Flush()

            Return New CodeCommentStatement(sb.ToString())
        End Function

        ''' <summary>
        ''' Get either the app.config file name or the project file name if the app.config file isn't included in the 
        ''' project. Used to check out the right set of files... 
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <param name="VsHierarchy"></param>
        Friend Function AppConfigOrProjectFileNameForCheckout(ProjectItem As EnvDTE.ProjectItem, VsHierarchy As IVsHierarchy) As String
            ' We also want to check out the app.config and possibly the project file(s)...
            If ProjectItem IsNot Nothing Then
                ' We try to check out the app.config file...
                Dim projSpecialFiles As IVsProjectSpecialFiles = TryCast(VsHierarchy, IVsProjectSpecialFiles)
                If projSpecialFiles IsNot Nothing Then
                    Dim appConfigFileName As String = ""
                    Dim appConfigItemId As UInteger
                    Dim hr As Integer = projSpecialFiles.GetFile(__PSFFILEID.PSFFILEID_AppConfig, CUInt(__PSFFLAGS.PSFF_FullPath), appConfigItemId, appConfigFileName)
                    If VSErrorHandler.Succeeded(hr) AndAlso appConfigFileName <> "" Then
                        If appConfigItemId <> VSITEMID.NIL Then
                            Return appConfigFileName
                        Else
                            ' Not app.config file in the project - we need to check out the project file!
                            If ProjectItem.ContainingProject IsNot Nothing AndAlso ProjectItem.ContainingProject.FullName <> "" Then
                                Return ProjectItem.ContainingProject.FullName
                            End If
                        End If
                    End If
                End If
            End If
            Return String.Empty
        End Function

        ''' <summary>
        ''' Translate the visibility (friend/public) and other type attributes (i.e. sealed) from CodeModel lingo
        ''' to what CodeDom expects
        ''' </summary>
        ''' <param name="cc2"></param>
        Friend Function CodeModelToCodeDomTypeAttributes(cc2 As EnvDTE80.CodeClass2) As TypeAttributes
            Requires.NotNull(cc2, NameOf(cc2))

            Dim returnValue As TypeAttributes

            Select Case cc2.Access
                Case EnvDTE.vsCMAccess.vsCMAccessProject
                    returnValue = TypeAttributes.NestedAssembly
                Case EnvDTE.vsCMAccess.vsCMAccessPublic
                    returnValue = TypeAttributes.Public
                Case Else
                    Debug.Fail("Unexpected access for settings class: " & cc2.Access.ToString())
                    returnValue = TypeAttributes.NestedAssembly
            End Select

            If cc2.InheritanceKind = EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed Then
                returnValue = returnValue Or TypeAttributes.Sealed
            End If

            Return returnValue
        End Function

    End Module
End Namespace
