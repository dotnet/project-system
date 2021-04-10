' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing.Design

Imports Microsoft.VisualStudio.Data.Core
Imports Microsoft.VisualStudio.Data.Services
Imports Microsoft.VisualStudio.Data.Services.SupportEntities
Imports Microsoft.VisualStudio.DataTools.Interop
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VSDesigner.Data.Local
Imports Microsoft.VSDesigner.VSDesignerPackage

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Simple UI type editor that launches a IVsDataConnectionDialog dialog to let 
    ''' the user create/edit connection strings
    ''' </summary>
    Friend NotInheritable Class ConnectionStringUITypeEditor
        Inherits UITypeEditor

        ''' <summary>
        ''' This is a modal dialog...
        ''' </summary>
        ''' <param name="context">The context parameter is ignored...</param>
        Public Overrides Function GetEditStyle(context As ComponentModel.ITypeDescriptorContext) As UITypeEditorEditStyle
            Return UITypeEditorEditStyle.Modal
        End Function

        ''' <summary>
        ''' Edit the actual value
        ''' </summary>
        ''' <param name="context">The context parameter is ignored...</param>
        ''' <param name="ServiceProvider">
        ''' The following services are expected to be available from the service provider:
        '''   IVsDataConnectionDialogFactory
        '''   IVsDataProviderManager
        '''   IDTAdoDotNetProviderMapper
        '''   IUIService (will work without it)
        ''' </param>
        ''' <param name="oValue"></param>
        ''' <remarks>Does not use the IWindowsFormsEditorService service to show it's dialog...</remarks>
        Public Overrides Function EditValue(context As ComponentModel.ITypeDescriptorContext, ServiceProvider As IServiceProvider, oValue As Object) As Object
            Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                Dim dataConnectionDialogFactory As IVsDataConnectionDialogFactory = DirectCast(ServiceProvider.GetService(GetType(IVsDataConnectionDialogFactory)), IVsDataConnectionDialogFactory)

                If dataConnectionDialogFactory Is Nothing Then
                    Debug.Fail("Couldn't get the IVsDataConnectionDialogFactory service from our provider...")
                    Return oValue
                End If

                Dim providerManager As IVsDataProviderManager = DirectCast(ServiceProvider.GetService(GetType(IVsDataProviderManager)), IVsDataProviderManager)
                If providerManager Is Nothing Then
                    Debug.Fail("Couldn't get the IVsProviderManager service from our provider")
                    Return oValue
                End If

                Dim providerMapper As IDTAdoDotNetProviderMapper = DirectCast(ServiceProvider.GetService(GetType(IDTAdoDotNetProviderMapper)), IDTAdoDotNetProviderMapper)
                If providerMapper Is Nothing Then
                    Debug.Fail("Couldn't get the IDTAdoDotNetProviderMapper service from our provider")
                    Return oValue
                End If

                Dim dataConnectionDialog As IVsDataConnectionDialog = dataConnectionDialogFactory.CreateConnectionDialog()

                If dataConnectionDialog Is Nothing Then
                    Debug.Fail("Failed to get a IVsDataConnectionDialog from the IVsDataConnectionDialogFactory!?")
                    Return oValue
                End If

                ' If this is a local data file, we've gotta let the connectionstringconverter service
                ' do it's magic to convert the path. Trying to convert a non-local data connection string
                ' should be a no-op, so we should be safe if we just try to get hold of the required services
                ' and give it a try...
                Dim dteProj As EnvDTE.Project = Nothing
                Dim connectionStringConverter As IConnectionStringConverterService =
                DirectCast(ServiceProvider.GetService(GetType(IConnectionStringConverterService)), IConnectionStringConverterService)
                If connectionStringConverter IsNot Nothing Then
                    ' The SettingsDesignerLoader should have added the project item as a service in case someone needs to 
                    ' get hold of it....
                    Dim dteProjItem As EnvDTE.ProjectItem = Nothing
                    dteProjItem = DirectCast(ServiceProvider.GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)
                    If dteProjItem IsNot Nothing Then
                        dteProj = dteProjItem.ContainingProject
                    Else
                        Debug.Fail("We failed to get the EnvDTE.ProjectItem service from our service provider. The settings designer loader should have added it...")
                    End If
                End If

                dataConnectionDialog.AddSources(AddressOf New DataConnectionDialogFilterer(dteProj).IsCombinationSupported)
                dataConnectionDialog.LoadSourceSelection()
                dataConnectionDialog.LoadProviderSelections()

                Dim value As SerializableConnectionString
                Try
                    value = TryCast(oValue, SerializableConnectionString)

                    ' We keep track of if there was sensitive data in the string when we were called - if not, we've gotta prompt the user
                    ' about the potential security implications if they add sensitive data
                    Dim containedSensitiveData As Boolean = False

                    ' If we have a value coming in, we should feed the dialog with this value
                    If value IsNot Nothing Then
                        ' The normalized connection string contains the connection string after the connection string converter is done
                        ' munching it.
                        Dim normalizedConnectionString As String = Nothing
                        Try
                            If connectionStringConverter IsNot Nothing AndAlso dteProj IsNot Nothing AndAlso value.ProviderName <> "" Then
                                normalizedConnectionString = connectionStringConverter.ToDesignTime(dteProj, value.ConnectionString, value.ProviderName)
                            End If
                        Catch ex As ArgumentException
                        Catch ex As ConnectionStringConverterServiceException
                            ' Well, the user may very well type garbage into the connection string text box
                        Finally
                            ' If we couldn't find the service, or if something else went wrong, we fall back 
                            ' to the connection string as it is showing in the designer...
                            If normalizedConnectionString Is Nothing Then
                                normalizedConnectionString = value.ConnectionString
                            End If
                        End Try

                        Dim providerGuid As Guid = Guid.Empty
                        If value.ProviderName <> "" Then
                            ' Get the provider GUID (if any)
                            providerGuid = GetGuidFromInvariantProviderName(providerMapper, value.ProviderName, normalizedConnectionString, False)
                        End If

                        ' If we have a provider, we can feed the dialog with the initial values. 
                        If Not providerGuid.Equals(Guid.Empty) Then
                            dataConnectionDialog.LoadExistingConfiguration(providerGuid, normalizedConnectionString, False)

                            Dim oldConnectionStringProperties As IVsDataConnectionProperties = GetConnectionStringProperties(providerManager, providerGuid, normalizedConnectionString)
                            If oldConnectionStringProperties IsNot Nothing AndAlso ContainsSensitiveData(oldConnectionStringProperties) Then
                                ' If we already had sensitive data in the string coming in to this function, we don't have to prompt again...
                                containedSensitiveData = True
                            End If
                        ElseIf normalizedConnectionString <> "" Then
                            dataConnectionDialog.SafeConnectionString = normalizedConnectionString
                        End If
                    End If

                    If dataConnectionDialog.ShowDialog() AndAlso dataConnectionDialog.DisplayConnectionString <> "" Then
                        ' If the user press OK and we have a connection string, lets return this new value!
                        dataConnectionDialog.SaveProviderSelections()
                        If dataConnectionDialog.SaveSelection Then
                            dataConnectionDialog.SaveSourceSelection()
                        End If

                        Dim newValue As New SerializableConnectionString With {
                        .ProviderName = GetInvariantProviderNameFromGuid(providerMapper, dataConnectionDialog.SelectedProvider),
                        .ConnectionString = GetConnectionString(ServiceProvider, dataConnectionDialog, Not containedSensitiveData)
                    }
                        If dteProj IsNot Nothing AndAlso connectionStringConverter IsNot Nothing Then
                            ' Go back to the runtime representation of the string...
                            newValue.ConnectionString = connectionStringConverter.ToRunTime(dteProj, newValue.ConnectionString, newValue.ProviderName)
                        End If

                        Return newValue
                    Else
                        ' Well, we better return the old value...
                        Return oValue
                    End If
                Finally
                    If dataConnectionDialog IsNot Nothing Then
                        dataConnectionDialog.Dispose()
                    End If
                End Try
                Return oValue
            End Using
        End Function

        ''' <summary>
        ''' Determine if a given connection string contains sensitive information.
        ''' </summary>
        ''' <param name="ProviderManager"></param>
        ''' <param name="DataProvider"></param>
        ''' <param name="ConnectionString"></param>
        Private Shared Function ContainsSensitiveData(ProviderManager As IVsDataProviderManager, DataProvider As Guid, ConnectionString As String) As Boolean
            If ConnectionString = "" Then
                Return False
            End If

            Try
                Dim DataConnectionProperties As IVsDataConnectionProperties = GetConnectionStringProperties(ProviderManager, DataProvider, ConnectionString)
                Return ContainsSensitiveData(DataConnectionProperties)
            Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(ContainsSensitiveData), NameOf(ConnectionStringUITypeEditor))
            End Try
            ' The secure & safe assumption is that it does contain sensitive data
            Return True
        End Function

        ''' <summary>
        ''' Determine if a given connection string contains sensitive information.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <param name="DataProvider"></param>
        ''' <param name="ConnectionString"></param>
        Private Shared Function ContainsSensitiveData(ServiceProvider As IServiceProvider, DataProvider As Guid, ConnectionString As String) As Boolean
            Dim providerManager As IVsDataProviderManager = DirectCast(ServiceProvider.GetService(GetType(IVsDataProviderManager)), IVsDataProviderManager)
            If providerManager IsNot Nothing Then
                Return ContainsSensitiveData(providerManager, DataProvider, ConnectionString)
            Else
                Debug.Fail("Failed to get IVsDataProviderManager from provided ServiceProvider")
            End If
            ' The secure & safe assumption is that it does contain sensitive data
            Return True
        End Function

        ''' <summary>
        ''' Depending on if the user wants to store the password or not, we get the connection string 
        ''' in slightly different ways...
        ''' </summary>
        ''' <param name="Dialog">The IVsDataConnectionDialog instance to get the connection string from</param>
        ''' <returns>
        ''' Unencrypted ConnectionString with or without the user-entered password depending on if
        ''' there exists sensitive information in the string and whether the user chooses to persist it
        '''</returns>
        Private Shared Function GetConnectionString(ServiceProvider As IServiceProvider, Dialog As IVsDataConnectionDialog, PromptIfContainsSensitiveData As Boolean) As String
            Requires.NotNull(Dialog, NameOf(Dialog))
            Requires.NotNull(ServiceProvider, NameOf(ServiceProvider))

            Dim SafeConnectionString As String = Dialog.SafeConnectionString

            If SafeConnectionString Is Nothing Then
                Debug.Fail("Failed to get SafeConnectionString from IVsDataConnectionDialog (got a NULL value:()")
                Return ""
            End If

            Dim RawConnectionString As String = DataProtection.DecryptString(Dialog.EncryptedConnectionString)
            If ContainsSensitiveData(ServiceProvider, Dialog.SelectedProvider, RawConnectionString) Then
                If Not PromptIfContainsSensitiveData OrElse
                       DesignerFramework.DesignerMessageBox.Show(ServiceProvider,
                                                             My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_IncludeSensitiveInfoInConnectionStringWarning,
                                                             DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider),
                                                             System.Windows.Forms.MessageBoxButtons.YesNo,
                                                             System.Windows.Forms.MessageBoxIcon.Warning,
                                                             System.Windows.Forms.MessageBoxDefaultButton.Button2) = System.Windows.Forms.DialogResult.Yes _
                Then
                    Return RawConnectionString
                End If
            End If

            Return SafeConnectionString
        End Function

        ''' <summary>
        ''' Get connection string properties for a specific provider/connection string
        ''' </summary>
        ''' <param name="ProviderManager"></param>
        ''' <param name="ProviderGUID"></param>
        ''' <param name="ConnectionString"></param>
        Private Shared Function GetConnectionStringProperties(ProviderManager As IVsDataProviderManager, ProviderGUID As Guid, ConnectionString As String) As IVsDataConnectionProperties
            Dim provider As IVsDataProvider = Nothing
            If ProviderManager.Providers.ContainsKey(ProviderGUID) Then
                provider = ProviderManager.Providers(ProviderGUID)
            End If
            Dim DataConnectionProperties As IVsDataConnectionProperties
            DataConnectionProperties = TryCast(provider.CreateObject(GetType(IVsDataConnectionProperties)), IVsDataConnectionProperties)
            If DataConnectionProperties IsNot Nothing Then
                DataConnectionProperties.Parse(ConnectionString)
            End If
            Return DataConnectionProperties
        End Function

        ''' <summary>
        ''' Does the given connection string contain sensitive data?
        ''' </summary>
        ''' <param name="ConnectionProperties"></param>
        Private Shared Function ContainsSensitiveData(ConnectionProperties As IVsDataConnectionProperties) As Boolean
            If ConnectionProperties Is Nothing Then
                Debug.Fail("We can't tell if it contains sensitive data if we didn't get a bag of properties!")
                Throw New ArgumentNullException()
            End If

            ' If the safe string's length is less than the full string's length, then it must strip something sensitive out...
            Return ConnectionProperties.ToSafeString().Trim.Length < ConnectionProperties.ToString().Trim.Length()
        End Function

#Region "Mapping provider GUIDs <-> display names"

        Private Shared Function GetInvariantProviderNameFromGuid(ProviderMapper As IDTAdoDotNetProviderMapper, providerGuid As Guid) As String
            If ProviderMapper Is Nothing Then
                Debug.Fail("Failed to get a IDTAdoDotNetProviderMapper")
                Return providerGuid.ToString()
            End If

            Dim invariantName As String = ProviderMapper.MapGuidToInvariantName(providerGuid)
            If invariantName Is Nothing Or invariantName = "" Then
                Debug.Fail(String.Format("{0} is not an ADO.NET provider", providerGuid))
                Return providerGuid.ToString()
            End If

            Return invariantName
        End Function

        Private Shared Function GetGuidFromInvariantProviderName(ProviderMapper As IDTAdoDotNetProviderMapper, providerName As String, ConnectionString As String, EncryptedString As Boolean) As Guid
            If ProviderMapper Is Nothing Then
                Debug.Fail("Failed to get a IDTAdoDotNetProviderMapper")
                Return Guid.Empty
            End If

            Dim providerGuid As Guid = ProviderMapper.MapInvariantNameToGuid(providerName, ConnectionString, EncryptedString)
            If providerGuid.Equals(Guid.Empty) Then
                Debug.Fail(String.Format("Couldn't find GUID for provider {0}", providerName))
                Try
                    ' Let's see if the provided name is a valid Guid?
                    Return New Guid(providerName)
                Catch ex As FormatException
                    ' Nope...
                End Try
                Return Guid.Empty
            End If

            Return providerGuid
        End Function
#End Region

        Private Class DataConnectionDialogFilterer
            Private ReadOnly _targetProject As EnvDTE.Project

            Public Sub New(project As EnvDTE.Project)
                _targetProject = project
            End Sub

            Public Function IsCombinationSupported(source As Guid, provider As Guid) As Boolean
                Return VSDesigner.Data.DataProviderProjectControl.IsProjectSupported(provider, _targetProject)
            End Function
        End Class

    End Class
End Namespace
