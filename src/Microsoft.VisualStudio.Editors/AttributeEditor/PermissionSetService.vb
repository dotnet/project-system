' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.IO
Imports System.Security
Imports System.Security.Permissions
Imports System.Xml

Imports Microsoft.Build.Tasks.Deployment.ManifestUtilities
Imports Microsoft.VisualStudio.Shell.Design.Serialization

Imports NativeMethods = Microsoft.VisualStudio.Editors.Interop.NativeMethods

Namespace Microsoft.VisualStudio.Editors.VBAttributeEditor

    '--------------------------------------------------------------------------
    ' PermissionSetService:
    '   Builder Service Class. Implements SVbPermissionSetService 
    '   exposed via the IVbPermissionSetService interface.
    '--------------------------------------------------------------------------
    <CLSCompliant(False)>
    Friend NotInheritable Class PermissionSetService
        Implements Interop.IVbPermissionSetService

        Private ReadOnly _serviceProvider As IServiceProvider

        Friend Sub New(sp As IServiceProvider)
            _serviceProvider = sp
        End Sub

        Public Function CreateSecurityElementFromXmlElement(element As XmlElement) As SecurityElement

            ' Create the new security element
            Dim securityElement As New SecurityElement(element.Name)

            ' Add the attributes
            For Each attribute As XmlAttribute In element.Attributes
                securityElement.AddAttribute(attribute.Name, attribute.Value)
            Next

            ' Add the child nodes
            For Each node As XmlNode In element.ChildNodes
                If node.NodeType = XmlNodeType.Element Then
                    securityElement.AddChild(CreateSecurityElementFromXmlElement(CType(node, XmlElement)))
                End If
            Next

            Return securityElement
        End Function

        Public Function LoadPermissionSet(strPermissionSet As String) As PermissionSet

            ' Load the XML
            Dim document As New XmlDocument With {
                .XmlResolver = Nothing
            }
            Using xmlReader As XmlReader = XmlReader.Create(New StringReader(strPermissionSet))
                document.Load(xmlReader)
            End Using

            ' Create the permission set from the XML
            Dim permissionSet As New PermissionSet(PermissionState.None)
            permissionSet.FromXml(CreateSecurityElementFromXmlElement(document.DocumentElement))
            Return permissionSet
        End Function

        Private Shared Function DocDataToStream(doc As DocData) As Stream
            Dim retStream As New MemoryStream()
            Using docReader As New DocDataTextReader(doc, False)
                Dim writer As New StreamWriter(retStream)
                writer.Write(docReader.ReadToEnd())
                writer.Flush()
                retStream.Seek(0, SeekOrigin.Begin)
            End Using
            Return retStream
        End Function

        Public Function ComputeZonePermissionSet(strManifestFileName As String, strTargetZone As String, strExcludedPermissions As String) As Object Implements Interop.IVbPermissionSetService.ComputeZonePermissionSet

            Try

                Dim projectPermissionSet As PermissionSet = Nothing

                If (strManifestFileName IsNot Nothing) AndAlso (strManifestFileName.Length > 0) Then

                    Dim manifestInfo As New TrustInfo With {
                        .PreserveFullTrustPermissionSet = True
                    }

                    Try
                        Using appManifestDocData As New DocData(_serviceProvider, strManifestFileName)

                            manifestInfo.ReadManifest(DocDataToStream(appManifestDocData))

                        End Using

                        projectPermissionSet = manifestInfo.PermissionSet

                    Catch

                        ' If this fails, there is no project permission set

                    End Try

                    If manifestInfo.IsFullTrust Then
                        Return Nothing
                    End If

                End If

                Dim identityList As String() = Nothing

                If (strExcludedPermissions IsNot Nothing) AndAlso (strExcludedPermissions.Length > 0) Then
                    identityList = StringToIdentityList(strExcludedPermissions)
                End If

                Return SecurityUtilities.ComputeZonePermissionSet(
                    strTargetZone,
                    projectPermissionSet,
                    identityList)

            Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(ComputeZonePermissionSet), NameOf(PermissionSetService))
            End Try

            Return Nothing

        End Function

        Public Function IsAvailableInProject(strPermissionSet As String, ProjectPermissionSet As Object, ByRef isAvailable As Boolean) As Integer Implements Interop.IVbPermissionSetService.IsAvailableInProject

            Try

                isAvailable = True

                ' Validate the project permission set
                If (ProjectPermissionSet IsNot Nothing) AndAlso (TypeOf ProjectPermissionSet Is PermissionSet) Then

                    ' Load the string permission set
                    Dim permissionSet As PermissionSet = LoadPermissionSet(strPermissionSet)
                    If permissionSet IsNot Nothing Then

                        ' Check the subset relationship
                        isAvailable = permissionSet.IsSubsetOf(CType(ProjectPermissionSet, PermissionSet))

                    End If

                End If

            Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(IsAvailableInProject), NameOf(PermissionSetService))
            End Try

            Return NativeMethods.S_OK
        End Function

        ' Returns S_FALSE if there is no tip
        Public Function GetRequiredPermissionsTip(strPermissionSet As String, ByRef strTip As String) As Integer Implements Interop.IVbPermissionSetService.GetRequiredPermissionsTip

            Dim hasTip As Boolean = False

            Try

                Dim isFirstPermission As Boolean = True

                ' Load the string permission set
                Dim permissionSet As PermissionSet = LoadPermissionSet(strPermissionSet)
                If permissionSet IsNot Nothing Then

                    Const strPrefix As String = "System.Security.Permissions."

                    For Each permission As Object In permissionSet

                        If Not isFirstPermission Then
                            strTip &= vbCrLf
                        Else

                            strTip &= My.Resources.Microsoft_VisualStudio_Editors_Designer.PermissionSet_Requires & vbCrLf

                            hasTip = True
                            isFirstPermission = False
                        End If

                        ' Chop off the type prefix if present
                        Dim strTemp As String = permission.GetType.ToString()
                        If strTemp.StartsWith(strPrefix) Then
                            strTemp = strTemp.Substring(strPrefix.Length)
                        End If

                        strTip &= strTemp
                    Next

                End If

            Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(IsAvailableInProject), NameOf(PermissionSetService))
            End Try

            If hasTip Then
                Return NativeMethods.S_OK
            Else
                Return NativeMethods.S_FALSE
            End If
        End Function

        Private Shared Function StringToIdentityList(s As String) As String()
            Dim a() As String = s.Split(CChar(";"))
            For i As Integer = 0 To a.Length - 1
                a(i) = a(i).Trim()
            Next
            Return a
        End Function

    End Class

End Namespace
