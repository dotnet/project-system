' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports EnvDTE

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    ''' <summary>
    ''' Utilities related to DTE projects and project items
    ''' </summary>
    Friend NotInheritable Class DTEUtils

        Private Const PROJECTPROPERTY_BUILDACTION As String = "BuildAction"

        ''' <summary>
        ''' This is a shared class - disallow instantiation.
        ''' </summary>
        Private Sub New()
        End Sub

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
        ''' Given a DTE project, returns the active IVsCfg configuration for it
        ''' </summary>
        ''' <param name="Project">The DTE project</param>
        ''' <param name="VsCfgProvider">The IVsCfgProvider2 interface instance to look up the active configuration from</param>
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
        Public Shared Function GetActiveDTEConfiguration(Project As Project) As Configuration
            Try
                Return Project.ConfigurationManager.ActiveConfiguration
            Catch ex As ArgumentException
                'If there are no configurations defined in the project, this call can fail.  In that case, just return
                '  the first config (there should be a single Debug configuration automatically defined and available).
                Return Project.ConfigurationManager.Item(1) '1-indexed
            Catch ex As Exception When ReportWithoutCrash(ex, "Unexpected exception trying to get the active configuration", NameOf(DTEUtils))
                Return Project.ConfigurationManager.Item(1) '1-indexed
            End Try
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

    End Class

End Namespace

