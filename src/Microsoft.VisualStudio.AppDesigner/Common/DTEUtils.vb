' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports EnvDTE
Imports Microsoft.VisualStudio.Shell.Interop


Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    ''' <summary>
    ''' Utilities related to DTE projects and project items
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class DTEUtils


        'The relevant project property names

        Private Const s_PROJECTPROPERTY_MSBUILD_ITEMTYPE As String = "ItemType"
        Private Const s_PROJECTPROPERTY_BUILDACTION As String = "BuildAction"

        ''' <summary>
        ''' This is a shared class - disallow instantation.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub New()
        End Sub


        ''' <summary>
        ''' Given a collection of ProjectItem ("ProjectItems"), queries it for the ProjectItem
        '''   of a given key.  If not found, returns Nothing.
        ''' </summary>
        ''' <param name="ProjectItems">The collection of ProjectItem to check</param>
        ''' <param name="Name">The key to check for.</param>
        ''' <returns>The ProjectItem for the given key, if found, else Nothing.  Throws exceptions only in unexpected cases.</returns>
        ''' <remarks></remarks>
        Public Shared Function QueryProjectItems(ProjectItems As ProjectItems, Name As String) As ProjectItem
            Try
                Return ProjectItems.Item(Name)
            Catch ex As ArgumentException
                'This is the expected exception if the key could not be found.
            Catch ex As Exception When AppDesCommon.ReportWithoutCrash(ex, "Unexpected exception searching for an item in ProjectItems", NameOf(DTEUtils))
                'Any other error - shouldn't be the case, but it might depend on the project implementation
            End Try

            Return Nothing
        End Function



        ''' <summary>
        ''' Get the file name from a project item.
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <returns></returns>
        ''' <remarks>If the item contains of multiple files, the first one is returned</remarks>
        Public Shared Function FileNameFromProjectItem(ProjectItem As ProjectItem) As String
            If ProjectItem Is Nothing Then
                System.Diagnostics.Debug.Fail("Can't get file name for NULL project item!")
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
        ''' <returns></returns>
        ''' <remarks></remarks>
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
        ''' <returns></returns>
        ''' <remarks></remarks>
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
        ''' Given a DTE project, returns the active IVsCfg configuration for it
        ''' </summary>
        ''' <param name="Project">The DTE project</param>
        ''' <param name="VsCfgProvider">The IVsCfgProvider2 interface instance to look up the active configuration from</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetActiveConfiguration(Project As Project, VsCfgProvider As IVsCfgProvider2) As IVsCfg
            Dim VsCfg As IVsCfg = Nothing
            With GetActiveDTEConfiguration(Project)
                VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgOfName(.ConfigurationName, .PlatformName, VsCfg))
            End With
            Return VsCfg
        End Function


        ''' <summary>
        ''' Given a DTE project, returns the active DTE configuration object for it
        ''' </summary>
        ''' <param name="Project">The DTE project</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetActiveDTEConfiguration(Project As Project) As Configuration
            Try
                Return Project.ConfigurationManager.ActiveConfiguration
            Catch ex As ArgumentException
                'If there are no configurations defined in the project, this call can fail.  In that case, just return
                '  the first config (there should be a single Debug configuration automatically defined and available).
                Return Project.ConfigurationManager.Item(1) '1-indexed
            Catch ex As Exception When AppDesCommon.ReportWithoutCrash(ex, "Unexpected exception trying to get the active configuration", NameOf(DTEUtils))
                Return Project.ConfigurationManager.Item(1) '1-indexed
            End Try
        End Function


        ''' <summary>
        ''' Tries to set the Build Action property of the given project item to the given build action (enumation).  
        '''   If this project system doesn't have that property, this call is a NOP.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks></remarks>
        Public Shared Sub SetBuildAction(Item As ProjectItem, BuildAction As VSLangProj.prjBuildAction)
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, s_PROJECTPROPERTY_BUILDACTION)
            If BuildActionProperty IsNot Nothing Then
                BuildActionProperty.Value = BuildAction
            End If
        End Sub

        ''' <summary>
        ''' Tries to get the Build Action property of the given project item to the given build action (enumation).  
        '''   If this project system doesn't have that property, returns prjBuildActionNone.
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks></remarks>
        Public Shared Function GetBuildAction(Item As ProjectItem) As VSLangProj.prjBuildAction
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, s_PROJECTPROPERTY_BUILDACTION)
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

            Dim BuildActionProperty As [Property] = GetProjectItemProperty(item, s_PROJECTPROPERTY_MSBUILD_ITEMTYPE)
            If BuildActionProperty IsNot Nothing Then
                BuildActionProperty.Value = buildAction
            End If
        End Sub

        ''' <summary>
        ''' Tries to get the Build Action property of the given project item to the given build action (enumation).  
        '''   If this project system doesn't have that property, returns "".
        ''' </summary>
        ''' <param name="Item">The ProjectItem on which to set the property</param>
        ''' <remarks></remarks>
        Public Shared Function GetBuildActionAsString(Item As ProjectItem) As String
            Dim BuildActionProperty As [Property] = GetProjectItemProperty(Item, s_PROJECTPROPERTY_MSBUILD_ITEMTYPE)
            If BuildActionProperty IsNot Nothing Then
                Return CType(BuildActionProperty.Value, String)
            End If

            Return String.Empty
        End Function

    End Class

End Namespace

