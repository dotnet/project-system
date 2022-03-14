' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.IO
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Editors.ResourceEditor
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' The root designer for settings
    ''' </summary>
    Friend NotInheritable Class SettingsDesigner
        Inherits DesignerFramework.BaseRootDesigner
        Implements IRootDesigner

        Friend Const SETTINGS_FILE_EXTENSION As String = ".settings"

        Friend Const ApplicationScopeName As String = "Application"
        Friend Const UserScopeName As String = "User"
        Friend Const CultureInvariantDefaultProfileName As String = "(Default)"
        Private Const SpecialClassName As String = "MySettings"

        ' Our view
        Private _settingsDesignerViewProperty As SettingsDesignerView

        ''' <summary>
        ''' Trace switch used by all SettingsDesigner components - should be moved to the common Switches file
        ''' </summary>
        Friend Shared ReadOnly Property TraceSwitch As TraceSwitch
            Get
                Static MyTraceSwitch As New TraceSwitch("SettingsDesigner", "Tracing for settings designer")
                Return MyTraceSwitch
            End Get
        End Property

        ''' <summary>
        ''' Demand-crete our designer view 
        ''' </summary>
        Private ReadOnly Property View As SettingsDesignerView
            Get
                If _settingsDesignerViewProperty Is Nothing Then
                    Debug.WriteLineIf(TraceSwitch.TraceVerbose, "Creating SettingsDesignerView")
                    _settingsDesignerViewProperty = New SettingsDesignerView
                    _settingsDesignerViewProperty.SetDesigner(Me)
                End If
                Return _settingsDesignerViewProperty
            End Get
        End Property

        ''' <summary>
        ''' Have we already created a view?
        ''' </summary>
        Friend ReadOnly Property HasView As Boolean
            Get
                Return _settingsDesignerViewProperty IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Publicly expose our view
        ''' </summary>
        ''' <param name="technology"></param>
        ''' <returns>The view for this root designer</returns>
        Public Function GetView(technology As ViewTechnology) As Object Implements IRootDesigner.GetView
            If technology <> ViewTechnology.Default Then
                Debug.Fail("Unsupported view technology!")
                Throw New NotSupportedException()
            End If

            Return View
        End Function

        ''' <summary>
        ''' Our supported technologies
        ''' </summary>
        Public ReadOnly Property SupportedTechnologies As ViewTechnology() Implements IRootDesigner.SupportedTechnologies
            Get
                Return New ViewTechnology() {ViewTechnology.Default}
            End Get
        End Property

        ''' <summary>
        ''' Get access to all our settings
        ''' </summary>
        Friend ReadOnly Property Settings As DesignTimeSettings
            Get
                Return Component
            End Get
        End Property

        ''' <summary>
        ''' Commit any pending changes
        ''' </summary>
        Public Sub CommitPendingChanges(suppressValidationUI As Boolean, cancelOnValidationFailure As Boolean)
            If _settingsDesignerViewProperty IsNot Nothing Then
                _settingsDesignerViewProperty.CommitPendingChanges(suppressValidationUI, cancelOnValidationFailure)
            End If
        End Sub

#Region "Component overrides and shadows"

        ''' <summary>
        ''' Make component property type safe if we want to access the component through
        ''' a SettingsDesigner instance
        ''' </summary>
        Public Shadows ReadOnly Property Component As DesignTimeSettings
            Get
                Return CType(MyBase.Component, DesignTimeSettings)
            End Get
        End Property
#End Region

        ''' <summary>
        ''' Show context menu
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Public Overloads Sub ShowContextMenu(sender As Object, e As System.Windows.Forms.MouseEventArgs)
            ShowContextMenu(Constants.MenuConstants.SettingsDesignerContextMenuID, e.X, e.Y)
        End Sub

        Protected Overrides Sub Dispose(Disposing As Boolean)
            If Disposing Then
                If _settingsDesignerViewProperty IsNot Nothing Then
                    Debug.WriteLineIf(TraceSwitch.TraceVerbose, "Disposing SettingsDesignerView")
                    _settingsDesignerViewProperty.Dispose()
                    _settingsDesignerViewProperty = Nothing
                End If
            End If
            MyBase.Dispose(Disposing)
        End Sub

#Region "Helper methods to determine the settings class name"

        '''<summary>
        '''Get the fully qualified settings class name
        '''</summary>
        '''<param name="Hierarchy"></param>
        '''<param name="Item"></param>
        Friend Shared Function FullyQualifiedGeneratedTypedSettingsClassName(Hierarchy As IVsHierarchy, ItemId As UInteger, Settings As DesignTimeSettings, Item As EnvDTE.ProjectItem) As String
            Dim Ns As String
            Ns = ProjectUtils.GeneratedSettingsClassNamespace(Hierarchy, ProjectUtils.ItemId(Hierarchy, Item), True)
            Return ProjectUtils.FullyQualifiedClassName(Ns, GeneratedClassName(Hierarchy, ItemId, Settings, ProjectUtils.FileName(Item)))
        End Function

        ''' <summary>
        ''' Helper method to determine the generated class name...
        ''' 
        ''' If this is a VB project, and it is the default .settings file, and the magic UseMySettingsClassName flag is set in the .settings
        ''' file, we will use the name "MySettings" instead of basing the classname off the filename 
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="itemId"></param>
        ''' <param name="Settings"></param>
        ''' <param name="FullPath"></param>
        Friend Shared Function GeneratedClassName(Hierarchy As IVsHierarchy, itemId As UInteger, Optional Settings As DesignTimeSettings = Nothing, Optional FullPath As String = Nothing) As String
            Try
                If itemId = VSITEMID.NIL AndAlso FullPath = "" Then
                    Debug.Fail("Must supply either an itemid or a full path to determine the class name")
                    Return ""
                End If

                ' If we didn't get a full path, let's compute it from the hierarchy and itemid
                If FullPath = "" AndAlso itemId <> VSITEMID.NIL Then
                    Dim projItem As EnvDTE.ProjectItem = Common.DTEUtils.ProjectItemFromItemId(Hierarchy, itemId)
                    FullPath = Common.DTEUtils.FileNameFromProjectItem(projItem)
                End If

                '
                ' If this is a VB project, and it is the default settings file, and the default settings file has the magic
                ' UsMySettingsName flag set, we special-case the class name...
                '
                ' First, we have to figure out if this is a vb project...
                '
                Dim isVbProject As Boolean = False
                If Hierarchy IsNot Nothing Then
                    isVbProject = Common.IsVbProject(Hierarchy)
                End If

                If isVbProject AndAlso
                    ((itemId <> VSITEMID.NIL AndAlso IsDefaultSettingsFile(Hierarchy, itemId)) _
                    OrElse (FullPath <> "" AndAlso IsDefaultSettingsFile(Hierarchy, FullPath))) _
                Then
                    '
                    ' Now, since this is a VB project, and it is the default settings file, 
                    ' we check the UseSpecialClassName flag
                    ' To do so, we've got to crack the .settings file open if this is not already
                    ' done...
                    '
                    Try
                        If Settings Is Nothing Then
                            ' 
                            ' No settings class provided - let's crack open the .settings file... 
                            '
                            Settings = New DesignTimeSettings()
                            Using Reader As New StreamReader(FullPath)
                                SettingsSerializer.Deserialize(Settings, Reader, True)
                            End Using
                        End If

                        If Settings.UseSpecialClassName Then
                            Return SpecialClassName
                        End If
                    Catch ex As Exception When Common.ReportWithoutCrash(ex, String.Format("Failed to crack open {0} to determine if we were supposed to use the ""Special"" settings class name", FullPath), NameOf(SettingsDesigner))
                    End Try
                End If

                '
                ' Not a special case - let's return the "normal" class name which is based on the file name...
                '
                Return GeneratedClassNameFromPath(FullPath)
            Catch ex As Exception When Common.ReportWithoutCrash(ex, "Failed to determine if we were supposed to use the ""Special"" settings class name", NameOf(SettingsDesigner))
            End Try
            Return ""
        End Function

        ''' <summary>
        ''' The class name is basically the file name minus the file extension...
        ''' </summary>
        ''' <param name="PathName"></param>
        Private Shared Function GeneratedClassNameFromPath(PathName As String) As String
            If PathName Is Nothing Then
                Debug.Fail("Can't get a class name from an empty path!")
                Return ""
            End If
            Return ResourceEditorView.GetGeneratedClassNameFromFileName(IO.Path.GetFileNameWithoutExtension(PathName))
        End Function

        ''' <summary>
        ''' Is this the default settings file?
        ''' </summary>
        ''' <param name="Hierarchy"></param>
        ''' <param name="itemId"></param>
        Friend Shared Function IsDefaultSettingsFile(Hierarchy As IVsHierarchy, itemId As UInteger) As Boolean
            If itemId = VSITEMID.NIL OrElse itemId = VSITEMID.ROOT OrElse itemId = VSITEMID.SELECTION Then
                Return False
            End If

            Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(Hierarchy, IVsProjectSpecialFiles)
            If SpecialProjectItems Is Nothing Then
                Debug.Fail("Failed to get IVsProjectSpecialFiles from project")
                Return False
            End If

            Dim DefaultSettingsItemId As UInteger
            Dim DefaultSettingsFilePath As String = Nothing
            Dim hr As Integer = SpecialProjectItems.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, 0, DefaultSettingsItemId, DefaultSettingsFilePath)

            If VSErrorHandler.Succeeded(hr) AndAlso itemId = DefaultSettingsItemId Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is this the "default" settings file
        ''' </summary>
        ''' <param name="FilePath">Fully qualified path of file to check</param>
        Friend Shared Function IsDefaultSettingsFile(Hierarchy As IVsHierarchy, FilePath As String) As Boolean
            If Hierarchy Is Nothing Then
                Debug.Fail("Passed in a NULL hiearchy - can't figure out if this is the default settings file")
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
                    Dim NormalizedDefaultSettingFilePath As String = IO.Path.GetFullPath(DefaultSettingsFilePath)
                    Dim NormalizedSettingFilePath As String = IO.Path.GetFullPath(FilePath)
                    Return String.Equals(NormalizedDefaultSettingFilePath, NormalizedSettingFilePath, StringComparison.OrdinalIgnoreCase)
                End If
            Else
                ' Something went wrong when we tried to get the special file name. This could be because there is a directory
                ' with the same name as the default settings file would have had if it existed.
                ' Anyway, since the project system can't find the default settings file name, this can't be it!
            End If
            Return False
        End Function

#End Region

#Region "Sync user config files"

        ''' <summary>
        ''' Find all user config files that are associated with this application
        ''' </summary>
        ''' <param name="DIrectories"></param>
        Friend Shared Function FindUserConfigFiles(Directories As List(Of String)) As List(Of String)
            Dim result As New List(Of String)
            For Each directory As String In Directories
                AddUserConfigFiles(directory, result)
            Next
            Return result
        End Function

        ''' <summary>
        ''' Find all directories that we are going to search through to find user.config files
        ''' </summary>
        ''' <param name="hierarchy"></param>
        Friend Shared Function FindUserConfigDirectories(hierarchy As IVsHierarchy) As List(Of String)
            Dim result As New List(Of String)
            Dim ConfigHelper As New Shell.Design.Serialization.ConfigurationHelperService

            ' No hierarchy - can't find any user.config files...
            If hierarchy Is Nothing Then
                Return result
            End If

            Dim hierSp As IServiceProvider = Common.ServiceProviderFromHierarchy(hierarchy)
            Dim project As EnvDTE.Project = Common.DTEUtils.EnvDTEProject(hierarchy)

            If project Is Nothing OrElse project.ConfigurationManager Is Nothing Then
                Return result
            End If

            For Each BuildConfiguration As EnvDTE.Configuration In project.ConfigurationManager
                Try
                    '
                    ' Add all combinations of Roaming/Local User paths...
                    '

                    Dim path As String
                    path = ConfigHelper.GetUserConfigurationPath(hierSp, project, Configuration.ConfigurationUserLevel.PerUserRoaming, underHostingProcess:=False, buildConfiguration:=BuildConfiguration)
                    If path IsNot Nothing Then
                        path = IO.Path.GetDirectoryName(path)
                        ' Make sure we only add the path once...
                        If Not result.Contains(path) Then
                            result.Add(path)
                        End If
                    End If

                    path = ConfigHelper.GetUserConfigurationPath(hierSp, project, Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal, underHostingProcess:=False, buildConfiguration:=BuildConfiguration)
                    If path IsNot Nothing Then
                        path = IO.Path.GetDirectoryName(path)
                        ' Make sure we only add the path once...
                        If Not result.Contains(path) Then
                            result.Add(path)
                        End If
                    End If
                Catch ex As ArgumentException
                    ' Failed to get one or more paths...
                End Try
            Next
            Return result
        End Function

        ''' <summary>
        ''' Find all user config files associated with this application given the conditions applied
        ''' </summary>
        ''' <param name="path"></param>
        ''' <param name="files"></param>
        Friend Shared Sub AddUserConfigFiles(path As String, files As List(Of String))
            Debug.WriteLineIf(Common.Switches.SDSyncUserConfig.TraceInfo, String.Format("SettingsDesigner::DeleteUserConfig, path={0}", path))

            If path = "" Then
                Return
            End If

            ' The path passed in to us is the path to the current active user.config file..
            Dim currentApplicationVersionDirectoryInfo As New IO.DirectoryInfo(path)

            ' The application may have scribbled user.config files in sibling directories to the current version's
            ' directory, so we'll start off from there...
            Dim applicationRootDirectoryInfo As IO.DirectoryInfo = currentApplicationVersionDirectoryInfo.Parent()

            ' If the parent directory doesn't exist, we are fine...
            If Not applicationRootDirectoryInfo.Exists Then
                Return
            End If

            For Each directory As IO.DirectoryInfo In applicationRootDirectoryInfo.EnumerateDirectories()
                For Each file As IO.FileInfo In directory.EnumerateFiles("user.config")
                    files.Add(file.FullName)
                Next
            Next
        End Sub

        ''' <summary>
        ''' Delete all user configs associated with all versions of the current project
        ''' </summary>
        ''' <param name="files">List of files to delete</param>
        ''' <param name="directories">List of directories to delete (if empty)</param>
        Friend Shared Function DeleteFilesAndDirectories(files As List(Of String), directories As List(Of String)) As Boolean
            Dim completeSuccess As Boolean = True
            If files IsNot Nothing Then
                For Each file As String In files
                    Try
                        IO.File.Delete(file)
                    Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(DeleteFilesAndDirectories), NameOf(SettingsDesigner))
                        completeSuccess = False
                    End Try
                Next
            End If

            If directories IsNot Nothing Then
                For Each directory As String In directories
                    Try
                        IO.Directory.Delete(directory, False)
                    Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(DeleteFilesAndDirectories), NameOf(SettingsDesigner))
                        completeSuccess = False
                    End Try
                Next
            End If
            Return completeSuccess
        End Function
#End Region
    End Class

End Namespace
