' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.IO

Imports EnvDTE
Imports EnvDTE.Constants

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' Utilities related to DTE projects and project items
    ''' </summary>
    Friend NotInheritable Class DTEUtils

#Region "Constants from DTE.idl"

        'A Guid version of vsProjectItemKindPhysicalFolder, which as a projectitem kind indicates that the
        '  projectitem is a physical folder on disk (as opposed to a virtual folder, etc.)
        Private Shared ReadOnly s_guid_vsProjectItemKindPhysicalFolder As New Guid(vsProjectItemKindPhysicalFolder)

#End Region

        Public Const PROJECTPROPERTY_CUSTOMTOOL As String = "CustomTool"
        Public Const PROJECTPROPERTY_CUSTOMTOOLNAMESPACE As String = "CustomToolNamespace"

        Private Const PROJECTPROPERTY_MSBUILD_ITEMTYPE As String = "ItemType"
        Private Const PROJECTPROPERTY_BUILDACTION As String = "BuildAction"

        ''' <summary>
        ''' This is a shared class - disallow instantiation.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Given a collection of ProjectItem ("ProjectItems"), queries it for the ProjectItem
        '''   of a given key.  If not found, returns Nothing.
        ''' </summary>
        ''' <param name="ProjectItems">The collection of ProjectItem to check</param>
        ''' <param name="Name">The key to check for.</param>
        ''' <returns>The ProjectItem for the given key, if found, else Nothing.  Throws exceptions only in unexpected cases.</returns>
        Public Shared Function QueryProjectItems(ProjectItems As ProjectItems, Name As String) As ProjectItem
            Try
                Return ProjectItems.Item(Name)
            Catch ex As ArgumentException
                'This is the expected exception if the key could not be found.
            Catch ex As Exception When ReportWithoutCrash(ex, "Unexpected exception searching for an item in ProjectItems", NameOf(DTEUtils))
                'Any other error - shouldn't be the case, but it might depend on the project implementation
            End Try

            Return Nothing
        End Function

        ''' <summary>
        ''' Retrieves the directory name on disk for a ProjectItems collection.
        ''' </summary>
        ''' <param name="ProjectItems">The ProjectItems collection to check.  Must refer to a physical folder on disk.</param>
        ''' <returns>The directory name of the collection on disk.</returns>
        Public Shared Function GetFolderNameFromProjectItems(ProjectItems As ProjectItems) As String
            If s_guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(ProjectItems.Kind)) Then
                If TypeOf ProjectItems.Parent Is Project Then
                    Return GetProjectDirectory(DirectCast(ProjectItems.Parent, Project))
                ElseIf TypeOf ProjectItems.Parent Is ProjectItem Then
                    Return GetFileNameFromFolderProjectItem(DirectCast(ProjectItems.Parent, ProjectItem))
                Else
                    Debug.Fail("Unexpected Parent type for ProjectItems")
                    Return Nothing
                End If
            Else
                Debug.Fail("Shouldn't call GetFileNameFromProjectItems for a ProjectItems collection that is not a physical disk folder.")
                Return ""
            End If
        End Function

        ''' <summary>
        ''' Retrieves the file name on disk for a ProjectItem.
        ''' </summary>
        ''' <param name="ProjectItem">The project item to check.</param>
        ''' <returns>The filename and path of the project item.</returns>
        Private Shared Function GetFileNameFromFolderProjectItem(ProjectItem As ProjectItem) As String
            If s_guid_vsProjectItemKindPhysicalFolder.Equals(New Guid(ProjectItem.Kind)) Then
                'The FileNames property represents the actual full path of the directory if the folder
                '  is an actual physical folder on disk.
                Debug.Assert(ProjectItem.FileCount = 1, "Didn't expect multiple filenames for a folder ProjectItem")
                Return ProjectItem.FileNames(1) 'this collection is 1-indexed
            Else
                Debug.Fail("Trying to get filename of a non-physical folder in the project")
                Return ""
            End If
        End Function

        ''' <summary>
        ''' Given a project, returns the project's directory on disk.
        ''' </summary>
        ''' <param name="Project">The project to query.</param>
        Private Shared Function GetProjectDirectory(Project As Project) As String
            'Some special cases.  In particular, note that the Miscellaneous Files project
            '  has a FullName value of the empty string.
            If Project Is Nothing OrElse Project.FullName Is Nothing OrElse Project.FullName = "" OrElse IsMiscellaneousProject(Project) Then
                Debug.Fail("Shouldn't be calling this with a null Project or with the Miscellaneous Files Project")
                Return ""
            End If

            Dim ProjectDirectory As String
            Try
                ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(Project.FullName))
            Catch ex As ArgumentException
                'In some scenarios Project.FullName does not give us an actual location on the local file
                '  system (e.g. when working with ASP.NET projects created on a URL instead of the local file
                '  system).  ASP.NET projects have a FullPath property which gives us what we want.  Let's try
                '  that before giving up.
                ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(CStr(Project.Properties.Item("FullPath").Value)))
            End Try

            Debug.Assert(Directory.Exists(ProjectDirectory), "Project's FullName property is not its path on disk?")
            Return ProjectDirectory
        End Function

        ''' <summary>
        ''' Given a project, determine if it is the Miscellaneous Files project
        ''' </summary>
        ''' <param name="Project"></param>
        Private Shared Function IsMiscellaneousProject(Project As Project) As Boolean
            If vsMiscFilesProjectUniqueName.Equals(Project.UniqueName, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If

            If Project.FullName = "" Then
                Debug.Fail("This project is not the miscellaneous files project, but its FullName is empty!")
                Return True 'defensive
            End If

            Return False
        End Function

        ''' <summary>
        ''' Get the current EnvDTE.Project instance for the project containing the .settings
        ''' file
        ''' </summary>
        Friend Shared Function EnvDTEProject(VsHierarchy As IVsHierarchy) As Project
            Dim ProjectObj As Object = Nothing
            VSErrorHandler.ThrowOnFailure(VsHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ExtObject, ProjectObj))
            Return CType(ProjectObj, Project)
        End Function

        ''' <summary>
        ''' Get EnvDTE.ProjectItem from hierarchy and itemid
        ''' </summary>
        ''' <param name="VsHierarchy"></param>
        ''' <param name="ItemId"></param>
        Public Shared Function ProjectItemFromItemId(VsHierarchy As IVsHierarchy, ItemId As UInteger) As ProjectItem
            Dim ExtensibilityObject As Object = Nothing
            VSErrorHandler.ThrowOnFailure(VsHierarchy.GetProperty(CUInt(ItemId), CInt(__VSHPROPID.VSHPROPID_ExtObject), ExtensibilityObject))
            Debug.Assert(ExtensibilityObject IsNot Nothing AndAlso TypeOf ExtensibilityObject Is ProjectItem)
            Return DirectCast(ExtensibilityObject, ProjectItem)
        End Function

        ''' <summary>
        ''' Get the file name from a project item.
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <remarks>If the item contains of multiple files, the first one is returned</remarks>
        Public Shared Function FileNameFromProjectItem(ProjectItem As ProjectItem) As String
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
        ''' Retrieves the given project item's property, if it exists, else Nothing
        ''' </summary>
        ''' <param name="PropertyName">The name of the property to retrieve.</param>
        Public Shared Function GetProjectItemProperty(ProjectItem As ProjectItem, PropertyName As String) As [Property]
            If ProjectItem.Properties Is Nothing Then
                Return Nothing
            End If

            For Each Prop As [Property] In ProjectItem.Properties
                If Prop.Name.Equals(PropertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return Prop
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Retrieves the given project's property, if it exists, else Nothing
        ''' </summary>
        ''' <param name="PropertyName">The name of the property to retrieve.</param>
        Public Shared Function GetProjectProperty(Project As Project, PropertyName As String) As [Property]
            If Project.Properties Is Nothing Then
                Return Nothing
            End If

            For Each Prop As [Property] In Project.Properties
                If Prop.Name.Equals(PropertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return Prop
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Tries to set the Build Action property of the given project item to the given build action (enumeration).  
        '''   If this project system doesn't have that property, this call is a NOP.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        Public Shared Sub SetBuildAction(Item As ProjectItem, BuildAction As VSLangProj.prjBuildAction)
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, PROJECTPROPERTY_BUILDACTION)
            If BuildActionProperty IsNot Nothing Then
                BuildActionProperty.Value = BuildAction
            End If
        End Sub

        ''' <summary>
        ''' Tries to get the Build Action property of the given project item to the given build action (enumeration).  
        '''   If this project system doesn't have that property, returns prjBuildActionNone.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        Public Shared Function GetBuildAction(Item As ProjectItem) As VSLangProj.prjBuildAction
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, PROJECTPROPERTY_BUILDACTION)
            If BuildActionProperty IsNot Nothing Then
                Return CType(BuildActionProperty.Value, VSLangProj.prjBuildAction)
            End If

            Return VSLangProj.prjBuildAction.prjBuildActionNone
        End Function

        ''' <summary>
        ''' Tries to set the Build Action property of the given project item to the given build action (string).  
        '''   If this project system doesn't have that property, this call is a NOP.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks>
        ''' This version of the function uses newer functionality in Visual Studio, and is necessary for more
        '''   recent build actions, such as the WPF build actions, that weren't available in the original enumeration.
        ''' </remarks>
        Public Shared Sub SetBuildActionAsString(item As ProjectItem, buildAction As String)

            Dim BuildActionProperty As [Property] = GetProjectItemProperty(item, PROJECTPROPERTY_MSBUILD_ITEMTYPE)
            If BuildActionProperty IsNot Nothing Then
                BuildActionProperty.Value = buildAction
            End If
        End Sub

        ''' <summary>
        ''' Tries to get the Build Action property of the given project item to the given build action (enumeration).  
        '''   If this project system doesn't have that property, returns "".
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        Public Shared Function GetBuildActionAsString(Item As ProjectItem) As String
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, PROJECTPROPERTY_MSBUILD_ITEMTYPE)
            If BuildActionProperty IsNot Nothing Then
                Return CType(BuildActionProperty.Value, String)
            End If

            Return String.Empty
        End Function

        ''' ;FindProjectItem
        ''' <summary>
        ''' Given ProjectItems and a file name, find and return a ProjectItem in this ProjectItems 
        ''' with the same file name. If none is found, return NULL.
        ''' </summary>
        Public Shared Function FindProjectItem(projectItems As ProjectItems, fileName As String) As ProjectItem
            For Each projectItem As ProjectItem In projectItems
                If projectItem.Kind.Equals(
                    vsProjectItemKindPhysicalFile, StringComparison.OrdinalIgnoreCase) AndAlso
                    projectItem.FileCount > 0 Then

                    Dim itemFileName As String = Path.GetFileName(projectItem.FileNames(1))
                    If String.Equals(fileName, itemFileName, StringComparison.OrdinalIgnoreCase) Then
                        Return projectItem
                    End If
                End If
            Next

            Return Nothing
        End Function

        ''' ;ItemIdOfProjectItem
        ''' <summary>
        ''' From a hierarchy and projectitem, return the item id
        ''' </summary>
        Public Shared Function ItemIdOfProjectItem(Hierarchy As IVsHierarchy, ProjectItem As ProjectItem) As UInteger
            Dim FoundItemId As UInteger
            VSErrorHandler.ThrowOnFailure(Hierarchy.ParseCanonicalName(ProjectItem.FileNames(1), FoundItemId))
            Return FoundItemId
        End Function

        Public Shared Sub ApplyTreeViewThemeStyles(handle As IntPtr)

            Dim ShellUIService As IVsUIShell5 = TryCast(Shell.Package.GetGlobalService(GetType(SVsUIShell)), IVsUIShell5)

            If ShellUIService IsNot Nothing Then
                ShellUIService.ThemeWindow(handle)
            Else
                ' set window long
                Dim newStyle As Integer = Interop.NativeMethods.GetWindowLong(handle, Interop.NativeMethods.GWL_STYLE).ToInt32
                newStyle = newStyle Or Interop.NativeMethods.TVS_TRACKSELECT
                newStyle = newStyle And Not Interop.NativeMethods.TVS_HASLINES
                Interop.NativeMethods.SetWindowLong(handle, Interop.NativeMethods.GWL_STYLE, New IntPtr(newStyle))

                ' set window theme
                Interop.NativeMethods.SetWindowTheme(handle, "Explorer", Nothing)

                ' set extended style
                newStyle = Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.TVM_GETEXTENDEDSTYLE, IntPtr.Zero, IntPtr.Zero).ToInt32
                newStyle = newStyle Or Interop.NativeMethods.TVS_EX_DOUBLEBUFFER
                newStyle = newStyle Or Interop.NativeMethods.TVS_EX_FADEINOUTEXPANDOS
                Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.TVM_SETEXTENDEDSTYLE, IntPtr.Zero, New IntPtr(newStyle))
            End If
        End Sub

        Public Shared Sub ApplyListViewThemeStyles(handle As IntPtr)
            ' set window theme
            Interop.NativeMethods.SetWindowTheme(handle, "Explorer", Nothing)

            ' set extended style
            Dim newStyle As Integer = Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.LVM_GETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, IntPtr.Zero).ToInt32
            newStyle = newStyle Or Interop.NativeMethods.LVS_EX_DOUBLEBUFFER
            Interop.NativeMethods.SendMessage(handle, Interop.NativeMethods.LVM_SETEXTENDEDLISTVIEWSTYLE, IntPtr.Zero, New IntPtr(newStyle))
        End Sub

    End Class

End Namespace

