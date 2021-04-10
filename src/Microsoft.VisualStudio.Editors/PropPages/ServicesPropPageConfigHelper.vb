' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml

Imports Microsoft.VisualStudio.Editors.SettingsDesigner
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Friend NotInheritable Class ServicesPropPageAppConfigHelper
        Private Shared ReadOnly s_clientRoleManagerType As Type = GetType(Web.ClientServices.Providers.ClientRoleProvider)
        Private Shared ReadOnly s_clientFormsMembershipProviderType As Type = GetType(Web.ClientServices.Providers.ClientFormsAuthenticationMembershipProvider)
        Private Shared ReadOnly s_clientWindowsMembershipProviderType As Type = GetType(Web.ClientServices.Providers.ClientWindowsAuthenticationMembershipProvider)

        Private Const SystemWeb As String = "system.web"
        Private Const RoleManager As String = "roleManager"
        Private Const Providers As String = "providers"
        Private Const AppSettings As String = "appSettings"
        Private Const ConnectionString As String = "connectionString"
        Private Const ConnectionStrings As String = "connectionStrings"
        Private Const Membership As String = "membership"
        Private Const Configuration As String = "configuration"
        Private Const ClientSettingsProviderPrefix As String = "ClientSettingsProvider."
        Private Const DefaultProvider As String = "defaultProvider"

        Private Const CacheTimeout As String = "cacheTimeout"
        Private Const CacheTimeoutDefault As String = "86400" '86400 seconds = 1 day

        Private Const Enabled As String = "enabled"
        Private Const EnabledDefault As String = "true"

        Private Const HonorCookieExpiry As String = "honorCookieExpiry"
        Private Const HonorCookieExpiryDefault As String = "false"

        Private Const ServiceUri As String = "serviceUri"
        Private Shared ReadOnly s_serviceUriDefault As String = String.Empty

        Private Const ConnectionStringName As String = "connectionStringName"
        Private Const ConnectionStringNameDefault As String = "DefaultConnection"
        Friend Const ConnectionStringValueDefault As String = "Data Source = |SQL/CE|"

        Private Const CredentialsProvider As String = "credentialsProvider"
        Private Shared ReadOnly s_credentialsProviderDefault As String = String.Empty

        Private Const SavePasswordHashLocally As String = "savePasswordHashLocally"
        Private Const SavePasswordHashLocallyDefault As String = "true"

        Private Const Add As String = "add"
        Private Const Key As String = "key"
        Private Const Name As String = "name"
        Private Const Type As String = "type"
        Private Const Value As String = "value"
        Private Const ChildPrefix As String = "child::"

        Private Const RoleManagerDefaultNameDefault As String = "ClientRoleProvider"
        Private Const MembershipDefaultNameDefault As String = "ClientAuthenticationMembershipProvider"

        Private Sub New()
            'Don't create a public constructor
        End Sub

#Region "Document"
        Friend Shared Function AppConfigXmlDocument(provider As IServiceProvider, projectHierarchy As IVsHierarchy, Optional createIfNotPresent As Boolean = False) As XmlDocument
            Dim contents As String = Nothing
            Using docData As DocData = GetDocData(provider, projectHierarchy, createIfNotPresent, False)
                If docData Is Nothing Then Return Nothing

                Using textReader As New DocDataTextReader(docData)
                    contents = textReader.ReadToEnd()
                End Using
            End Using
            Return XmlDocumentFromText(contents)
        End Function

        Friend Shared Function AppConfigXmlDocument(propertyPageSite As OLE.Interop.IPropertyPageSite, projectHierarchy As IVsHierarchy, Optional createIfNotPresent As Boolean = False) As XmlDocument
            If propertyPageSite IsNot Nothing Then
                Dim provider As IServiceProvider = CType(propertyPageSite, IServiceProvider)
                If provider IsNot Nothing Then
                    Return AppConfigXmlDocument(provider, projectHierarchy, createIfNotPresent)
                End If
            End If

            Return Nothing
        End Function

        Friend Shared Function GetDocData(provider As IServiceProvider, projectHierarchy As IVsHierarchy, Optional createIfNotPresent As Boolean = False, Optional writeable As Boolean = False) As DocData
            Return AppConfigSerializer.GetAppConfigDocData(provider, projectHierarchy, createIfNotPresent, writeable)
        End Function

        Friend Shared Function XmlDocumentFromText(contents As String) As XmlDocument
            Dim doc As XmlDocument = Nothing
            If Not String.IsNullOrEmpty(contents) Then
                doc = New XmlDocument With {
                    .XmlResolver = Nothing
                }
                Try
                    Using reader As XmlReader = XmlReader.Create(New StringReader(contents))
                        doc.Load(reader)
                    End Using
                Catch ex As XmlException
                    Return Nothing
                End Try
            End If
            Return doc
        End Function

        Friend Shared Function TryWriteXml(appConfigDocument As XmlDocument, provider As IServiceProvider, hierarchy As IVsHierarchy) As Boolean
            Dim fileName As String = Nothing
            Dim appConfigItemId As UInteger
            Dim flags As UInteger = CUInt(__PSFFLAGS.PSFF_CreateIfNotExist Or __PSFFLAGS.PSFF_FullPath)

            Dim ProjSpecialFiles As IVsProjectSpecialFiles = TryCast(hierarchy, IVsProjectSpecialFiles)
            ProjSpecialFiles.GetFile(__PSFFILEID.PSFFILEID_AppConfig, flags, appConfigItemId, fileName)

            Dim sb As New StringBuilder
            Using writer As New XmlTextWriter(New StringWriter(sb, CultureInfo.InvariantCulture))
                writer.Formatting = Formatting.Indented
                appConfigDocument.WriteContentTo(writer)
                writer.Flush()
            End Using

            Using docData As DocData = New DocData(provider, fileName)
                Using textWriter As New DocDataTextWriter(docData)
                    Try
                        textWriter.Write(sb.ToString())
                        textWriter.Close()
                        SaveAppConfig(fileName, provider, hierarchy)
                    Catch ex As COMException When IsCheckoutOrCancelError(ex)
                        Return False
                    End Try
                End Using
            End Using

            Return True
        End Function

        Private Shared Function IsCheckoutOrCancelError(cex As COMException) As Boolean
            Return cex IsNot Nothing AndAlso (cex.ErrorCode = &H80041004 OrElse cex.ErrorCode = &H8004000C)
        End Function

        'This code is stolen from SecurityPropertyPage.  If you don't do this, the document doesn't get written unless it
        'happens to be open

        Private Shared Sub SaveAppConfig(fileName As String, provider As IServiceProvider, hierarchy As IVsHierarchy)
            Dim rdt As IVsRunningDocumentTable = TryCast(provider.GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
            Debug.Assert(rdt IsNot Nothing, "What?  No RDT?")
            If rdt Is Nothing Then Throw New InvalidOperationException("No RDT")

            Dim hier As IVsHierarchy = Nothing
            Dim flags As UInteger
            Dim localPunk As IntPtr = IntPtr.Zero
            Dim localFileName As String = Nothing
            Dim itemId As UInteger
            Dim docCookie As UInteger = 0
            Dim readLocks As UInteger = 0
            Dim editLocks As UInteger = 0

            Try
                VSErrorHandler.ThrowOnFailure(hierarchy.ParseCanonicalName(fileName, itemId))
                VSErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument(CType(_VSRDTFLAGS.RDT_NoLock, UInteger), fileName, hier, itemId, localPunk, docCookie))
            Finally
                If localPunk <> IntPtr.Zero Then
                    Marshal.Release(localPunk)
                    localPunk = IntPtr.Zero
                End If
            End Try

            Try
                VSErrorHandler.ThrowOnFailure(rdt.GetDocumentInfo(docCookie, flags, readLocks, editLocks, localFileName, hier, itemId, localPunk))
            Finally
                If localPunk <> IntPtr.Zero Then
                    Marshal.Release(localPunk)
                End If
            End Try

            If editLocks = 1 Then
                ' we're the only person with it open, save the document
                VSErrorHandler.ThrowOnFailure(rdt.SaveDocuments(CUInt(__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty), hier, itemId, docCookie))
            End If
        End Sub
#End Region

#Region "Nodes/NodeList"
        Friend Shared Function GetXmlNode(currentNode As XmlNode, ParamArray path() As String) As XmlNode
            If path Is Nothing OrElse path.Length = 0 Then Return Nothing
            For i As Integer = 0 To path.Length - 1
                If currentNode Is Nothing Then Return Nothing
                currentNode = currentNode.SelectSingleNode(ChildPrefix & path(i))
            Next
            Return currentNode
        End Function

        Private Shared Function GetXmlNodeWithValueFromList(list As XmlNodeList, attributeName As String, searchValue As String) As XmlNode
            If list Is Nothing Then Return Nothing
            For Each node As XmlNode In list
                If GetAttribute(node, attributeName) = searchValue Then Return node
            Next
            Return Nothing
        End Function

        Private Shared Function GetConfigurationNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(doc, Configuration)
        End Function

        Private Shared Function GetConnectionStringsNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetConfigurationNode(doc), ConnectionStrings)
        End Function

        Private Shared Function GetSystemWebNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetConfigurationNode(doc), SystemWeb)
        End Function

        Private Shared Function GetRoleManagerNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetSystemWebNode(doc), RoleManager)
        End Function

        Private Shared Function GetRoleManagerProvidersNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetRoleManagerNode(doc), Providers)
        End Function

        Private Shared Function GetMembershipNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetSystemWebNode(doc), Membership)
        End Function

        Private Shared Function GetMembershipProvidersNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetMembershipNode(doc), Providers)
        End Function

        Private Shared Function GetDefaultClientServicesRoleManagerProviderNode(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As XmlNode
            Dim defaultProviderName As String = GetRoleManagerDefaultProviderName(doc)
            If String.IsNullOrEmpty(defaultProviderName) Then Return Nothing
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetRoleManagerProvidersNode(doc), Add)
            Dim addNode As XmlNode = GetXmlNodeWithValueFromList(addNodeList, Name, defaultProviderName)
            If addNode IsNot Nothing AndAlso IsClientRoleManagerProviderType(GetAttribute(addNode, Type), projectHierarchy) Then
                Return addNode
            End If
            Return Nothing
        End Function

        Private Shared Function GetXmlNodeList(node As XmlNode, listName As String) As XmlNodeList
            If node Is Nothing Then Return Nothing
            Return node.SelectNodes(ChildPrefix & listName)
        End Function

        Private Shared Function GetDefaultClientServicesMembershipProviderNode(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As XmlNode
            Dim defaultProviderName As String = GetMembershipDefaultProviderName(doc)
            If String.IsNullOrEmpty(defaultProviderName) Then Return Nothing
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetMembershipProvidersNode(doc), Add)
            Dim addNode As XmlNode = GetXmlNodeWithValueFromList(addNodeList, Name, defaultProviderName)
            If addNode IsNot Nothing AndAlso IsClientMembershipProviderType(GetAttribute(addNode, Type), projectHierarchy) Then
                Return addNode
            End If
            Return Nothing
        End Function

        Private Shared Function GetAppSettingsNode(doc As XmlDocument) As XmlNode
            Return GetXmlNode(GetConfigurationNode(doc), AppSettings)
        End Function

        Private Shared Function GetAppSettingsServiceUriNode(doc As XmlDocument) As XmlNode
            Dim addList As XmlNodeList = GetXmlNodeList(GetAppSettingsNode(doc), Add)
            Return GetXmlNodeWithValueFromList(addList, Key, AppSettingsName(ServiceUri))
        End Function

        Private Shared Function GetAppSettingsConnectionStringNameNode(doc As XmlDocument) As XmlNode
            Dim addList As XmlNodeList = GetXmlNodeList(GetAppSettingsNode(doc), Add)
            Return GetXmlNodeWithValueFromList(addList, Key, AppSettingsName(ConnectionStringName))
        End Function

        Private Shared Function GetAppSettingsHonorCookieExpiryNode(doc As XmlDocument) As XmlNode
            Dim addList As XmlNodeList = GetXmlNodeList(GetAppSettingsNode(doc), Add)
            Return GetXmlNodeWithValueFromList(addList, Key, AppSettingsName(HonorCookieExpiry))
        End Function

        Private Shared Function IsClientMembershipWindowsProviderNode(node As XmlNode, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return NodeHasTypeAttribute(node, GetSupportedType(s_clientWindowsMembershipProviderType, projectHierarchy))
        End Function

        Private Shared Function NodeHasTypeAttribute(node As XmlNode, typeToCheck As Type) As Boolean
            Dim nodeType As String = GetAttribute(node, Type)
            Return TypesMatch(nodeType, typeToCheck)
        End Function

        Private Shared Sub RemoveProvidersByType(node As XmlNode, typeToRemove As Type)
            Dim providers As XmlNodeList = GetXmlNodeList(node, Add)
            If providers IsNot Nothing Then
                For Each provider As XmlNode In providers
                    Dim providerType As String = GetAttribute(provider, Type)
                    If providerType IsNot Nothing AndAlso (providerType.Equals(typeToRemove.FullName, StringComparison.OrdinalIgnoreCase) OrElse providerType.StartsWith(typeToRemove.FullName + ",", StringComparison.OrdinalIgnoreCase)) Then
                        RemoveNode(node, provider)
                    End If
                Next
            End If
        End Sub
#End Region

#Region "Attributes"
        Friend Shared Function GetDefaultProviderName(node As XmlNode) As String
            If node Is Nothing Then Return Nothing

            Dim defaultProviderAttribute As XmlAttribute = node.Attributes(DefaultProvider)
            If defaultProviderAttribute IsNot Nothing Then
                Return defaultProviderAttribute.Value
            End If
            Return Nothing
        End Function

        Friend Shared Function GetRoleManagerDefaultProviderName(doc As XmlDocument) As String
            Return GetDefaultProviderName(GetRoleManagerNode(doc))
        End Function

        Friend Shared Function GetMembershipDefaultProviderName(doc As XmlDocument) As String
            Return GetDefaultProviderName(GetMembershipNode(doc))
        End Function

        Friend Shared Function GetAttribute(node As XmlNode, attributeName As String) As String
            If node Is Nothing Then Return Nothing
            Dim attribute As XmlAttribute = node.Attributes(attributeName)
            If attribute Is Nothing Then Return Nothing
            Return attribute.Value
        End Function

        Friend Shared Sub SetAttribute(doc As XmlDocument, node As XmlNode, attributeName As String, value As String)
            If node Is Nothing Then Return
            Dim attribute As XmlAttribute = node.Attributes(attributeName)
            If attribute Is Nothing Then
                attribute = CType(CreateNode(doc, XmlNodeType.Attribute, attributeName), XmlAttribute)
                node.Attributes.SetNamedItem(attribute)
            End If

            Debug.Assert(attribute IsNot Nothing, "No attribute")
            If attribute IsNot Nothing Then
                attribute.Value = value
            End If
        End Sub

        Friend Shared Sub SetAttributeIfNonNull(doc As XmlDocument, node As XmlNode, attributeName As String, value As String)
            If value IsNot Nothing Then SetAttribute(doc, node, attributeName, value)
        End Sub

        Private Shared Function SetAttributeValueAndCheckForChange(doc As XmlDocument, node As XmlNode, attributeName As String, value As IConvertible) As Boolean
            If node Is Nothing Then Return False
            Dim initialValue As String = GetAttribute(node, attributeName)
            Dim newValue As String = value.ToString(CultureInfo.InvariantCulture)
            SetAttribute(doc, node, attributeName, newValue)
            Return Not String.Equals(newValue, initialValue)
        End Function

        Friend Shared Sub SetAttributeIfNull(doc As XmlDocument, node As XmlNode, attributeName As String, value As String)
            If GetAttribute(node, attributeName) IsNot Nothing Then Return
            SetAttribute(doc, node, attributeName, value)
        End Sub

        Friend Shared Sub RemoveAttribute(node As XmlNode, attributeName As String)
            If node IsNot Nothing AndAlso Not String.IsNullOrEmpty(attributeName) Then
                Dim attributeToRemove As XmlAttribute = node.Attributes(attributeName)
                If attributeToRemove IsNot Nothing Then
                    node.Attributes.Remove(attributeToRemove)
                End If
            End If
        End Sub

        Friend Shared Sub RemoveNode(parentNode As XmlNode, childNode As XmlNode)
            If parentNode Is Nothing OrElse childNode Is Nothing Then Return
            parentNode.RemoveChild(childNode)
        End Sub
#End Region

#Region "ServicesEnabled"
        Friend Shared Function ApplicationServicesAreEnabled(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing, Optional checkForAllEnabled As Boolean = False) As Boolean
            If checkForAllEnabled Then
                Return HasDefaultClientServicesRoleManagerProvider(doc, projectHierarchy) AndAlso HasAppSettings(doc) AndAlso HasDefaultClientServicesAuthProvider(doc, projectHierarchy)
            Else
                Return HasDefaultClientServicesRoleManagerProvider(doc, projectHierarchy) OrElse HasAppSettings(doc) OrElse HasDefaultClientServicesAuthProvider(doc, projectHierarchy)
            End If
        End Function

        Friend Shared Function HasDefaultClientServicesRoleManagerProvider(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy) IsNot Nothing
        End Function

        Friend Shared Function HasAppSettings(doc As XmlDocument) As Boolean
            Return GetAppSettingsNode(doc) IsNot Nothing
        End Function

        Friend Shared Function HasDefaultClientServicesAuthProvider(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy) IsNot Nothing
        End Function

        Friend Shared Sub EnsureApplicationServicesEnabled(appConfigDocument As XmlDocument, enable As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing)
            EnsureAppSettings(appConfigDocument, enable, projectHierarchy)
            EnsureDefaultMembershipProvider(appConfigDocument, enable, projectHierarchy)
            EnsureDefaultRoleManagerProvider(appConfigDocument, enable, projectHierarchy)
        End Sub

        Private Shared Sub EnsureDefaultRoleManagerProvider(appConfigDocument As XmlDocument, enable As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing)
            If enable Then
                EnsureDefaultRoleManagerNodeExists(appConfigDocument, projectHierarchy)
            Else
                EnsureDefaultRoleManagerNodeDoesntExist(appConfigDocument, projectHierarchy)
            End If
        End Sub

        Private Shared Sub EnsureDefaultRoleManagerNodeExists(appConfigDocument As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing)
            'We should have a block that looks like this when we're done:
            '<roleManager defaultProvider="Default" enabled="true">
            '  <providers>
            '    <add name="Default" type="System.Web.ClientServices.Providers.ClientRoleProvider,System.Web.Extensions, Version=2.0.0.0, Culture=neutral PublicKeyToken=31bf3856ad364e35, processorArchitecture=MSIL"
            '              connectionStringName = "connSE"
            '              serviceUri = "http://localhost/testservices/rolesservice.svc"
            '              cacheTimeout = "60"
            '              honorCookieExpiry = "true"
            '         />
            '  </providers>
            '</roleManager>
            Dim configurationNode As XmlNode = EnsureNode(appConfigDocument, Configuration, appConfigDocument)
            Dim systemWebNode As XmlNode = EnsureNode(appConfigDocument, SystemWeb, configurationNode)
            Dim roleManagerNode As XmlNode = EnsureNode(appConfigDocument, RoleManager, systemWebNode)
            Dim defaultSettingNode As XmlNode

            Dim defaultProviderAttribute As XmlAttribute = roleManagerNode.Attributes(DefaultProvider)

            'If we already have a default provider, make sure it's one of ours
            If defaultProviderAttribute IsNot Nothing Then
                defaultSettingNode = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    'We had a default, and it wasn't one of ours.  Remove the default attribute
                    RemoveAttribute(roleManagerNode, DefaultProvider)
                    defaultProviderAttribute = Nothing
                End If
            End If

            If defaultProviderAttribute Is Nothing Then
                'Remove any existing provider with the same type as clientRoleManagerType
                RemoveProvidersByType(GetRoleManagerProvidersNode(appConfigDocument), s_clientRoleManagerType)
                Dim nameValue As String = GetRoleManagerCreateDefaultProviderName(appConfigDocument)
                SetAttribute(appConfigDocument, roleManagerNode, DefaultProvider, nameValue)
                SetAttribute(appConfigDocument, roleManagerNode, Enabled, EnabledDefault)
                defaultSettingNode = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    Dim providersNode As XmlNode = EnsureNode(appConfigDocument, Providers, roleManagerNode)
                    Dim addNode As XmlNode = CreateNode(appConfigDocument, XmlNodeType.Element, Add)
                    SetAttribute(appConfigDocument, addNode, Name, nameValue)
                    Dim defaultName As String = DefaultConnectionStringName(appConfigDocument, projectHierarchy)
                    SetAttributeIfNonNull(appConfigDocument, addNode, ConnectionStringName, defaultName)
                    SetAttribute(appConfigDocument, addNode, Type, GetSupportedType(s_clientRoleManagerType, projectHierarchy).AssemblyQualifiedName)
                    SetAttribute(appConfigDocument, addNode, ServiceUri, s_serviceUriDefault)
                    SetAttribute(appConfigDocument, addNode, CacheTimeout, CacheTimeoutDefault)
                    SetAttributeIfNonNull(appConfigDocument, addNode, HonorCookieExpiry, DefaultHonorCookieExpiry(appConfigDocument, projectHierarchy))
                    providersNode.AppendChild(addNode)
                End If
            End If
        End Sub

        Private Shared Function GetRoleManagerCreateDefaultProviderName(doc As XmlDocument) As String
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetRoleManagerProvidersNode(doc), Add)
            Return FindUniqueValueInList(addNodeList, RoleManagerDefaultNameDefault, Name)
        End Function

        Private Shared Function FindUniqueValueInList(nodeList As XmlNodeList, defaultName As String, attributeName As String) As String
            Dim count As Integer = 0
            Dim currentName As String = defaultName

            'If we can use the default name, just use that (don't append a number).  If we need to find another name, use
            '<defaultName>1, <defaultName>2, etc. until we find a name that's not being used.
            While GetXmlNodeWithValueFromList(nodeList, attributeName, currentName) IsNot Nothing
                count += 1
                currentName = defaultName & count.ToString(CultureInfo.InvariantCulture)
            End While

            Return currentName
        End Function

        Private Shared Function GetConnectionStringCreateDefaultProviderName(doc As XmlDocument) As String
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetConnectionStringsNode(doc), Add)
            Return FindUniqueValueInList(addNodeList, ConnectionStringNameDefault, Name)
        End Function

        Private Shared Sub EnsureDefaultRoleManagerNodeDoesntExist(appConfigDocument As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing)
            Dim systemWebNode As XmlNode = GetSystemWebNode(appConfigDocument)
            Dim roleManagerNode As XmlNode = GetRoleManagerNode(appConfigDocument)
            Dim roleManagerProvidersNode As XmlNode = GetRoleManagerProvidersNode(appConfigDocument)

            'Remove roleManager's defaultProvider and associated data, including "providers" if it's now
            'empty, and roleManager if it's now empty.
            Dim defaultSettingNode As XmlNode = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
            If defaultSettingNode IsNot Nothing Then
                RemoveAttribute(roleManagerNode, DefaultProvider)
                'Remove the <add> for the default node
                RemoveNode(roleManagerProvidersNode, defaultSettingNode)
            End If

            RemoveChildIfItHasNoChildren(roleManagerNode, roleManagerProvidersNode)
            RemoveChildIfItHasNoChildren(systemWebNode, roleManagerNode)
            RemoveChildIfItHasNoChildren(GetConfigurationNode(appConfigDocument), systemWebNode)
        End Sub

        Private Shared Sub EnsureDefaultMembershipProvider(appConfigDocument As XmlDocument, enable As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing)
            If enable Then
                EnsureDefaultMembershipProviderNodeExists(appConfigDocument, projectHierarchy)
            Else
                EnsureDefaultMembershipProviderNodeDoesntExist(appConfigDocument, projectHierarchy)
            End If
        End Sub

        Private Shared Sub EnsureDefaultMembershipProviderNodeExists(appConfigDocument As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing)
            'We should have a block that looks like this when we're done:
            '<membership defaultProvider="DefaultFormAuthenticationProvider">
            '  <providers>
            '    <add name="DefaultFormAuthenticationProvider" type="System.Web.ClientServices.Providers.ClientFormsAuthenticationMembershipProvider, System.Web.ClientServices, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, processorArchitecture=MSIL"
            '              connectionStringName = "connSE"
            '              serviceUri = "http://localhost/testservices/rolesservice.svc"
            '              credentialsProvider = "AppServicesConsoleApp.UICredentialProvider,AppServicesConsoleApp"
            '              savePasswordHashLocally="false"
            '         />
            '  </providers>
            '</roleManager>
            Dim configurationNode As XmlNode = EnsureNode(appConfigDocument, Configuration, appConfigDocument)
            Dim systemWebNode As XmlNode = EnsureNode(appConfigDocument, SystemWeb, configurationNode)
            Dim membershipNode As XmlNode = EnsureNode(appConfigDocument, Membership, systemWebNode)
            Dim defaultSettingNode As XmlNode

            Dim defaultProviderAttribute As XmlAttribute = membershipNode.Attributes(DefaultProvider)

            'If we already have a default provider, make sure it's one of ours
            If defaultProviderAttribute IsNot Nothing Then
                defaultSettingNode = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    'We had a default, and it wasn't one of ours.  Remove the default attribute
                    RemoveAttribute(membershipNode, DefaultProvider)
                    defaultProviderAttribute = Nothing
                End If
            End If

            If defaultProviderAttribute Is Nothing Then
                'Remove any existing provider with the same type as clientFormsMembershipProviderType and clientWindowsMembershipProviderType
                RemoveProvidersByType(GetMembershipProvidersNode(appConfigDocument), s_clientFormsMembershipProviderType)
                RemoveProvidersByType(GetMembershipProvidersNode(appConfigDocument), s_clientWindowsMembershipProviderType)
                Dim addNodeList As XmlNodeList = GetXmlNodeList(GetMembershipProvidersNode(appConfigDocument), Add)
                Dim nameValue As String = FindUniqueValueInList(addNodeList, MembershipDefaultNameDefault, Name)
                SetAttribute(appConfigDocument, membershipNode, DefaultProvider, nameValue)
                defaultSettingNode = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
                If defaultSettingNode Is Nothing Then
                    Dim providersNode As XmlNode = EnsureNode(appConfigDocument, Providers, membershipNode)
                    Dim addNode As XmlNode = CreateNode(appConfigDocument, XmlNodeType.Element, Add)
                    SetAttribute(appConfigDocument, addNode, Name, nameValue)
                    SetAttribute(appConfigDocument, addNode, Type, GetSupportedType(s_clientFormsMembershipProviderType, projectHierarchy).AssemblyQualifiedName)
                    SetAttributeIfNonNull(appConfigDocument, addNode, ConnectionStringName, DefaultConnectionStringName(appConfigDocument, projectHierarchy))
                    SetAttribute(appConfigDocument, addNode, ServiceUri, s_serviceUriDefault)
                    providersNode.AppendChild(addNode)
                End If
            End If
        End Sub

        Private Shared Sub EnsureDefaultMembershipProviderNodeDoesntExist(appConfigDocument As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing)
            Dim systemWebNode As XmlNode = GetSystemWebNode(appConfigDocument)
            Dim membershipNode As XmlNode = GetMembershipNode(appConfigDocument)
            Dim membershipProvidersNode As XmlNode = GetMembershipProvidersNode(appConfigDocument)

            'Remove membership's defaultProvider and associated data, including "providers" if it's now
            'empty, and membership if it's now empty.
            Dim defaultSettingNode As XmlNode = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
            If defaultSettingNode IsNot Nothing Then
                RemoveAttribute(membershipNode, DefaultProvider)
                'Remove the <add> for the default node
                If membershipProvidersNode IsNot Nothing Then
                    membershipProvidersNode.RemoveChild(defaultSettingNode)
                End If
            End If

            RemoveChildIfItHasNoChildren(membershipNode, membershipProvidersNode)
            RemoveChildIfItHasNoChildren(systemWebNode, membershipNode)
            RemoveChildIfItHasNoChildren(GetConfigurationNode(appConfigDocument), systemWebNode)
        End Sub

        Private Shared Function DefaultConnectionStringName(appConfigDocument As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As String
            Dim node As XmlNode
            Dim returnValue As String

            node = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
            returnValue = GetAttribute(node, ConnectionStringName)
            If returnValue IsNot Nothing Then Return returnValue

            node = GetDefaultClientServicesMembershipProviderNode(appConfigDocument, projectHierarchy)
            returnValue = GetAttribute(node, ConnectionStringName)
            If returnValue IsNot Nothing Then Return returnValue

            node = GetAppSettingsConnectionStringNameNode(appConfigDocument)
            Return GetAttribute(node, Value)
        End Function

        Private Shared Function DefaultHonorCookieExpiry(appConfigDocument As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As String
            Dim node As XmlNode
            Dim returnValue As String

            node = GetDefaultClientServicesRoleManagerProviderNode(appConfigDocument, projectHierarchy)
            returnValue = GetAttribute(node, HonorCookieExpiry)
            If returnValue IsNot Nothing Then Return returnValue

            node = GetAppSettingsHonorCookieExpiryNode(appConfigDocument)
            Return GetAttribute(node, Value)
        End Function

        Private Shared Sub EnsureAppSettings(appConfigDocument As XmlDocument, enable As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing)
            If enable Then
                EnsureAppSettingsNodeExists(appConfigDocument, projectHierarchy)
            Else
                EnsureClientAppSettingsDontExist(appConfigDocument)
            End If
        End Sub

        Private Shared Sub EnsureAppSettingsNodeExists(appConfigDocument As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing)
            'We should have a block that looks like this when we're done:
            '<appSettings>
            '  <add key=ClientSettingsProvider.ServiceUri" value=""
            '</appSettings>
            Dim configurationNode As XmlNode = EnsureNode(appConfigDocument, Configuration, appConfigDocument)
            EnsureNode(appConfigDocument, AppSettings, configurationNode)

            If GetAppSettingsServiceUriNode(appConfigDocument) Is Nothing Then
                AddAppConfigNode(appConfigDocument, AppSettingsName(ServiceUri), s_serviceUriDefault)
            End If

            Dim node As XmlNode
            Dim currentConnectionStringName As String = DefaultConnectionStringName(appConfigDocument, projectHierarchy)
            If Not String.IsNullOrEmpty(currentConnectionStringName) Then
                node = GetAppSettingsConnectionStringNameNode(appConfigDocument)
                If node Is Nothing Then
                    AddAppConfigNode(appConfigDocument, AppSettingsName(ConnectionStringName), currentConnectionStringName)
                End If
            End If

            Dim honorCookieValue As String = DefaultHonorCookieExpiry(appConfigDocument, projectHierarchy)
            If honorCookieValue IsNot Nothing Then
                node = GetAppSettingsHonorCookieExpiryNode(appConfigDocument)
                If node Is Nothing Then
                    AddAppConfigNode(appConfigDocument, AppSettingsName(HonorCookieExpiry), honorCookieValue)
                End If
            End If
        End Sub

        Private Shared Sub AddAppConfigNode(appConfigDocument As XmlDocument, keyValue As String, valueValue As String)
            Dim addNode As XmlNode = CreateNode(appConfigDocument, XmlNodeType.Element, Add)
            SetAttribute(appConfigDocument, addNode, Key, keyValue)
            SetAttribute(appConfigDocument, addNode, Value, valueValue)
            GetAppSettingsNode(appConfigDocument).AppendChild(addNode)
        End Sub

        Private Shared Sub EnsureClientAppSettingsDontExist(appConfigDocument As XmlDocument)
            Dim appSettingsNode As XmlNode = GetAppSettingsNode(appConfigDocument)
            If appSettingsNode Is Nothing Then Return

            RemoveNode(appSettingsNode, GetAppSettingsServiceUriNode(appConfigDocument))
            RemoveNode(appSettingsNode, GetAppSettingsConnectionStringNameNode(appConfigDocument))
            RemoveNode(appSettingsNode, GetAppSettingsHonorCookieExpiryNode(appConfigDocument))
            Dim configurationNode As XmlNode = GetConfigurationNode(appConfigDocument)
            If configurationNode Is Nothing Then Return
            RemoveChildIfItHasNoChildren(configurationNode, appSettingsNode)
        End Sub

        'Change the input string from "serviceUri" to "ClientSettingsProvider.ServiceUri"
        Private Shared Function AppSettingsName(inputName As String) As String
            Return String.Concat(ClientSettingsProviderPrefix, Char.ToUpperInvariant(inputName(0)), inputName.Substring(1))
        End Function

        Private Shared Function EnsureNode(doc As XmlDocument, nodeName As String, parentNode As XmlNode) As XmlNode
            Dim newNode As XmlNode = GetXmlNode(parentNode, nodeName)
            If newNode Is Nothing Then
                newNode = CreateNode(doc, XmlNodeType.Element, nodeName)
                parentNode.AppendChild(newNode)
            End If

            Return newNode
        End Function

        Private Shared Function CreateNode(doc As XmlDocument, nodeType As XmlNodeType, nodeName As String) As XmlNode
            Return doc.CreateNode(nodeType, nodeName, String.Empty)
        End Function

        'Note: The child may have attributes and still be removed
        Private Shared Sub RemoveChildIfItHasNoChildren(parentNode As XmlNode, childNode As XmlNode)
            If parentNode Is Nothing OrElse childNode Is Nothing Then Return

            If String.IsNullOrEmpty(childNode.InnerXml) Then
                parentNode.RemoveChild(childNode)
            End If
        End Sub
#End Region

#Region "Helpers"
        Friend Shared Function IsClientRoleManagerProviderType(fullTypeName As String, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return TypesMatch(fullTypeName, GetSupportedType(s_clientRoleManagerType, projectHierarchy))
        End Function

        Friend Shared Function IsClientMembershipProviderType(fullTypeName As String, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return TypesMatch(fullTypeName, GetSupportedType(s_clientFormsMembershipProviderType, projectHierarchy)) OrElse
            TypesMatch(fullTypeName, GetSupportedType(s_clientWindowsMembershipProviderType, projectHierarchy))
        End Function

        Private Shared Function TypesMatch(typeNameToCheck As String, desiredType As Type) As Boolean
            Return typeNameToCheck IsNot Nothing AndAlso desiredType IsNot Nothing AndAlso typeNameToCheck.Equals(desiredType.AssemblyQualifiedName, StringComparison.OrdinalIgnoreCase)
        End Function

        Friend Shared Function GetServiceUri(node As XmlNode) As String
            Return GetAttribute(node, ServiceUri)
        End Function

        Friend Shared Function AuthenticationServiceUrl(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As String
            Return GetServiceUri(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy))
        End Function

        Friend Shared Function AuthenticationServiceHost(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As String
            Return GetHostFromUrl(AuthenticationServiceUrl(doc, projectHierarchy))
        End Function

        Friend Shared Function RolesServiceHost(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As String
            Dim url As String = GetServiceUri(GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy))
            Return GetHostFromUrl(url)
        End Function

        Friend Shared Function WebSettingsUrl(doc As XmlDocument) As String
            Return GetAttribute(GetAppSettingsServiceUriNode(doc), Value)
        End Function

        Friend Shared Function WebSettingsHost(doc As XmlDocument) As String
            Return GetHostFromUrl(WebSettingsUrl(doc))
        End Function

        Private Shared Function GetHostFromUrl(url As String) As String
            If url Is Nothing Then Return Nothing
            url = url.Trim()
            If url = "" Then Return ""
            Dim separatorIndex As Integer = url.LastIndexOf("/")
            If separatorIndex = -1 Or Not url.ToUpperInvariant().EndsWith(".AXD") Then
                Throw New InvalidOperationException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services_InvalidUrls)
            End If
            Return url.Substring(0, separatorIndex)
        End Function

        Friend Shared Function GetSavePasswordHashLocally(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return TryGettingBooleanAttributeValue(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), SavePasswordHashLocally, SavePasswordHashLocallyDefault)
        End Function

        'Return value Nothing means the connection strings don't match
        Friend Shared Function GetEffectiveDefaultConnectionString(doc As XmlDocument, ByRef connectionStringSpecified As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing) As String
            Dim appSettingsConnectionStringNode As XmlNode = GetAppSettingsConnectionStringNameNode(doc)
            Dim appSettingsConnectionStringName As String = GetAttribute(appSettingsConnectionStringNode, Value)

            Dim roleManagerProviderNode As XmlNode = GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy)
            Dim roleManagerConnectionStringName As String = GetAttribute(roleManagerProviderNode, ConnectionStringName)

            Dim membershipProviderNode As XmlNode = GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy)
            Dim membershipConnectionStringName As String = GetAttribute(membershipProviderNode, ConnectionStringName)

            'If no connection strings are specified, use the default
            If appSettingsConnectionStringName Is Nothing AndAlso
                    roleManagerConnectionStringName Is Nothing AndAlso
                    membershipConnectionStringName Is Nothing Then
                connectionStringSpecified = True
                Return Nothing
            End If

            If appSettingsConnectionStringName <> roleManagerConnectionStringName OrElse
                    appSettingsConnectionStringName <> membershipConnectionStringName Then
                connectionStringSpecified = False
                Return Nothing
            End If

            'OK, the connection string names match: get the actual connection string.
            connectionStringSpecified = True
            Return GetConnectionString(doc, appSettingsConnectionStringName)
        End Function

        Friend Shared Function GetConnectionStringNode(doc As XmlDocument, whichString As String) As XmlNode
            Dim addNodeList As XmlNodeList = GetXmlNodeList(GetConnectionStringsNode(doc), Add)
            Return GetXmlNodeWithValueFromList(addNodeList, Name, whichString)
        End Function

        Friend Shared Function GetConnectionString(doc As XmlDocument, whichString As String) As String
            Return GetAttribute(GetConnectionStringNode(doc, whichString), ConnectionString)
        End Function

        Private Shared Sub SetConnectionString(doc As XmlDocument, whichString As String, newValue As String)
            Dim node As XmlNode = GetConnectionStringNode(doc, whichString)
            If newValue Is Nothing Then
                If node IsNot Nothing Then
                    Dim configurationNode As XmlNode = EnsureNode(doc, Configuration, doc)
                    Dim connectionStringsNode As XmlNode = EnsureNode(doc, ConnectionStrings, configurationNode)
                    connectionStringsNode.RemoveChild(node)
                End If
            Else
                If node Is Nothing Then
                    Dim configurationNode As XmlNode = EnsureNode(doc, Configuration, doc)
                    Dim connectionStringsNode As XmlNode = EnsureNode(doc, ConnectionStrings, configurationNode)
                    node = CreateNode(doc, XmlNodeType.Element, Add)
                    connectionStringsNode.AppendChild(node)
                    SetAttribute(doc, node, Name, whichString)
                End If
                SetAttribute(doc, node, ConnectionString, newValue)
            End If
        End Sub

        Friend Shared Function GetEffectiveHonorCookieExpiry(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean?
            Dim node As XmlNode
            Dim stringValue As String
            Dim roleManagerHonorCookieExpiry, appConfigHonorCookieExpiry As Boolean

            node = GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy)
            stringValue = GetAttribute(node, HonorCookieExpiry)

            'If we didn't get a value, parse the default (string) value.
            If stringValue Is Nothing OrElse Not Boolean.TryParse(stringValue, roleManagerHonorCookieExpiry) Then
                roleManagerHonorCookieExpiry = Boolean.Parse(HonorCookieExpiryDefault)
            End If

            node = GetAppSettingsHonorCookieExpiryNode(doc)
            stringValue = GetAttribute(node, Value)

            'If we didn't get a value, parse the default (string) value.
            If stringValue Is Nothing OrElse Not Boolean.TryParse(stringValue, appConfigHonorCookieExpiry) Then
                appConfigHonorCookieExpiry = Boolean.Parse(HonorCookieExpiryDefault)
            End If

            If roleManagerHonorCookieExpiry = appConfigHonorCookieExpiry Then
                Return appConfigHonorCookieExpiry
            End If
            Return Nothing
        End Function

        Friend Shared Function GetCacheTimeout(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As Integer
            Return TryGettingIntegerAttributeValue(GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), CacheTimeout, CacheTimeoutDefault)
        End Function

        Private Shared Function TryGettingBooleanAttributeValue(node As XmlNode, attributeName As String, defaultValue As String) As Boolean
            Dim stringValue As String = GetAttribute(node, attributeName)
            If stringValue Is Nothing Then stringValue = defaultValue
            Dim result As Boolean
            If Boolean.TryParse(stringValue, result) Then
                Return result
            Else
                Return Boolean.Parse(defaultValue)
            End If
        End Function

        Private Shared Function TryGettingIntegerAttributeValue(node As XmlNode, attributeName As String, defaultValue As String) As Integer
            Dim stringValue As String = GetAttribute(node, attributeName)
            If stringValue Is Nothing Then stringValue = defaultValue
            Dim result As Integer
            If Integer.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, result) Then
                Return result
            Else
                Return Integer.Parse(defaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Shared Function CustomCredentialProviderType(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As String
            Return GetAttribute(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), CredentialsProvider)
        End Function

        Friend Shared Function WindowsAuthSelected(doc As XmlDocument, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return IsClientMembershipWindowsProviderNode(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), projectHierarchy)
        End Function

        'Return whether the value changed
        Friend Shared Function SetAuthenticationServiceUri(doc As XmlDocument, value As String, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), ServiceUri, Normalize(value, AuthenticationSuffix))
        End Function

        'Return whether the value changed
        Friend Shared Function SetCustomCredentialProviderType(doc As XmlDocument, value As String, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), CredentialsProvider, value)
        End Function

        'Return whether the value changed
        'We pass in whether we want windows auth (the alternative is form auth)
        Friend Shared Function SetMembershipDefaultProvider(doc As XmlDocument, changeToWindows As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Dim node As XmlNode = GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy)
            If node Is Nothing Then Return False

            Dim initialValue As Boolean = WindowsAuthSelected(doc, projectHierarchy)
            Dim typeName As String
            If changeToWindows Then
                typeName = GetSupportedType(s_clientWindowsMembershipProviderType, projectHierarchy).AssemblyQualifiedName
            Else
                typeName = GetSupportedType(s_clientFormsMembershipProviderType, projectHierarchy).AssemblyQualifiedName
            End If
            SetAttribute(doc, node, Type, typeName)
            If changeToWindows Then
                Dim connectionStringNameToUse As String = DefaultConnectionStringName(doc, projectHierarchy)
                If String.IsNullOrEmpty(connectionStringNameToUse) Then
                    connectionStringNameToUse = GetConnectionStringCreateDefaultProviderName(doc)
                End If
                If GetConnectionStringNode(doc, connectionStringNameToUse) Is Nothing Then
                    SetConnectionStringText(doc, ConnectionStringValueDefault, projectHierarchy)
                End If
                SetAttributeIfNull(doc, node, ConnectionStringName, connectionStringNameToUse)
                SetAttributeIfNull(doc, node, CredentialsProvider, s_credentialsProviderDefault)
            End If
            Return Not Equals(changeToWindows, initialValue)
        End Function

        Friend Shared Sub SetConnectionStringText(doc As XmlDocument, newConnectionString As String, Optional projectHierarchy As IVsHierarchy = Nothing)
            Dim appSettingsConnectionStringNameNode As XmlNode = GetAppSettingsConnectionStringNameNode(doc)
            Dim connStrName As String

            'Create the connection string node, if we have to.
            If appSettingsConnectionStringNameNode Is Nothing Then
                Dim configurationNode As XmlNode = EnsureNode(doc, Configuration, doc)
                Dim appSettingsNode As XmlNode = EnsureNode(doc, AppSettings, configurationNode)
                appSettingsConnectionStringNameNode = CreateNode(doc, XmlNodeType.Element, Add)
                SetAttribute(doc, appSettingsConnectionStringNameNode, Key, AppSettingsName(ConnectionStringName))
                connStrName = DefaultConnectionStringName(doc, projectHierarchy)
                If connStrName Is Nothing Then
                    connStrName = GetConnectionStringCreateDefaultProviderName(doc)
                End If

                SetAttribute(doc, appSettingsConnectionStringNameNode, Value, connStrName)
                appSettingsNode.AppendChild(appSettingsConnectionStringNameNode)
            Else
                connStrName = GetAttribute(appSettingsConnectionStringNameNode, Value)
            End If

            If newConnectionString Is Nothing Then
                RemoveAttribute(GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), ConnectionStringName)
                RemoveAttribute(GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), ConnectionStringName)
                EnsureAppSettingsNodeExists(doc, projectHierarchy)
                If appSettingsConnectionStringNameNode IsNot Nothing Then
                    GetAppSettingsNode(doc).RemoveChild(appSettingsConnectionStringNameNode)
                End If
            Else
                SetAttribute(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), ConnectionStringName, connStrName)
                SetAttribute(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), ConnectionStringName, connStrName)
            End If

            SetConnectionString(doc, connStrName, newConnectionString)
        End Sub

        'Return whether the value changed
        Friend Shared Function SetRoleServiceUri(doc As XmlDocument, inputValue As String, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), ServiceUri, Normalize(inputValue, RolesSuffix))
        End Function

        'Return whether the value changed
        Friend Shared Function SetAppServicesServiceUri(doc As XmlDocument, inputValue As String) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetAppSettingsServiceUriNode(doc), Value, Normalize(inputValue, ProfileSuffix))
        End Function

        'Return whether the value changed
        Friend Shared Function SetCacheTimeout(doc As XmlDocument, inputValue As Integer, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), CacheTimeout, inputValue)
        End Function

        Friend Shared Sub SetHonorCookieExpiry(doc As XmlDocument, inputValue As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing)
            Dim stringInputValue As String = inputValue.ToString(CultureInfo.InvariantCulture)
            SetAttribute(doc, GetDefaultClientServicesRoleManagerProviderNode(doc, projectHierarchy), HonorCookieExpiry, stringInputValue)
            Dim appSettingsNode As XmlNode = GetAppSettingsHonorCookieExpiryNode(doc)
            If appSettingsNode Is Nothing Then
                AddAppConfigNode(doc, AppSettingsName(HonorCookieExpiry), stringInputValue)
            Else
                SetAttribute(doc, appSettingsNode, Value, stringInputValue)
            End If
        End Sub

        'Return whether the value changed
        Friend Shared Function SetSavePasswordHashLocally(doc As XmlDocument, inputValue As Boolean, Optional projectHierarchy As IVsHierarchy = Nothing) As Boolean
            Return SetAttributeValueAndCheckForChange(doc, GetDefaultClientServicesMembershipProviderNode(doc, projectHierarchy), SavePasswordHashLocally, inputValue)
        End Function

        Private Shared Function Normalize(val As String, suffix As String) As String
            If val Is Nothing Then Return Nothing
            val = val.Trim()
            If val = String.Empty Then Return String.Empty
            If val.EndsWith("/") Then
                Return val & suffix
            End If
            Return val & "/" & suffix
        End Function

        Friend Shared ReadOnly Property AuthenticationSuffix As String
            Get
                Return GetSuffix("Authentication")
            End Get
        End Property

        Friend Shared ReadOnly Property RolesSuffix As String
            Get
                Return GetSuffix("Role")
            End Get
        End Property

        Friend Shared ReadOnly Property ProfileSuffix As String
            Get
                Return GetSuffix("Profile")
            End Get
        End Property

        Private Shared Function GetSuffix(input As String) As String
            Return String.Format("{0}_JSON_AppService.axd", input)
        End Function

        Friend Shared Function ClientSettingsProviderName() As String
            Return GetType(Web.ClientServices.Providers.ClientSettingsProvider).FullName
        End Function

        Private Shared Function GetSupportedType(sourceType As Type, projectHierarchy As IVsHierarchy) As Type
            If projectHierarchy Is Nothing Then Return sourceType
            Dim mtSvc As MultiTargetService = New MultiTargetService(projectHierarchy, VSConstants.VSITEMID_ROOT, False)
            Dim supportedType As Type = mtSvc.GetSupportedType(sourceType, True)
            If supportedType Is Nothing Then
                Return sourceType
            Else
                Return supportedType
            End If
        End Function
#End Region
    End Class
End Namespace
