' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.ProjectSystem.Query
Imports Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.ProjectSystem.Query.ProjectModel.Implementation
Imports Microsoft.VisualStudio.ProjectSystem.Query.ServiceHub
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Editors.Interop

Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' Shim layer to handle nexus and non-nexus APIs based on project
    ''' </summary>
    Friend NotInheritable Class ClientShimUtils

        Public Shared Function VSHierarchyShim(itemId As UInteger, vsHierarchy As IVsHierarchy, serviceProvider As System.IServiceProvider) As IVsHierarchy

            Dim tmpService As IServiceProvider
            vsHierarchy.GetSite(tmpService)

            'TODO - do we need to check if on nexus client ? If yes, how to check ?
            ' If isClient = true Then
            '   Return vsHierarchy
            ' End If

            ' Is Misc File and is nexus client then create the cps object
            If IsMiscProjectFile(itemId, vsHierarchy) Then
                Dim path As String
                vsHierarchy.GetCanonicalName(itemId, path)
                Return New ClientVsHierarchy(vsHierarchy, path, serviceProvider)
            End If

            Return vsHierarchy
        End Function

        Private Shared Function IsMiscProjectFile(itemId As UInteger, vsHierarchy As IVsHierarchy) As Boolean

            Dim pguid As Guid
            vsHierarchy.GetGuidProperty(itemId, __VSHPROPID.VSHPROPID_ProjectIDGuid, pguid)

            If pguid = VSConstants.CLSID.MiscellaneousFilesProject_guid Then
                Return True
            End If

            Return False
        End Function

    End Class

    Friend Class ClientVsHierarchy
        Implements IVsHierarchy, IVsProject

        Private ReadOnly _nexusClientHierarchy As IVsHierarchy
        Private ReadOnly _vsProject As IVsProject
        Private _project As IProject
        Private _projectFilePath As String = "TESTING"

        Public Sub New(vsHierarchy As IVsHierarchy, resxFilename As String, serviceProvider As System.IServiceProvider)
            _nexusClientHierarchy = vsHierarchy
            _vsProject = CType(vsHierarchy, IVsProject)

            ThreadHelper.JoinableTaskFactory.Run(
                Async Function() As Task(Of Task)
                    Dim systemQueryService As IProjectSystemQueryService = CType(serviceProvider.GetService(GetType(IProjectSystemQueryService)), IProjectSystemQueryService)

                    If systemQueryService IsNot Nothing Then

                        Dim queryableSpace = Await systemQueryService.GetProjectModelQueryableSpaceAsync()
                        '' TODO : Exception. this operation is not valid
                        'Dim projectQuery = queryableSpace.Projects.With(Function(project) project.SourceFiles.Where(Function(f) f.FileName = resxFilename))
                        'Dim projects = Await projectQuery.ExecuteQueryAsync()
                        '_project = projects.SingleOrDefault()

                        '_projectFilePath = _project.Path
                        ' 
                        ' Which properties should I set/change in _nexusClientHierarchy to make it consisten
                        'Dim pguid As Guid ' ??????
                        '_nexusClientHierarchy.SetGuidProperty(VSConstants.VSITEMID.Root, __VSHPROPID.VSHPROPID_ProjectDir, pguid)
                    End If

                End Function)

        End Sub

        Public Function SetSite(psp As IServiceProvider) As Integer Implements IVsHierarchy.SetSite
            _nexusClientHierarchy.SetSite(psp)
        End Function

        Public Function GetSite(ByRef ppSP As IServiceProvider) As Integer Implements IVsHierarchy.GetSite
            _nexusClientHierarchy.GetSite(ppSP)
        End Function

        Public Function QueryClose(ByRef pfCanClose As Integer) As Integer Implements IVsHierarchy.QueryClose
            _nexusClientHierarchy.QueryClose(pfCanClose)
        End Function

        Public Function Close() As Integer Implements IVsHierarchy.Close
            _nexusClientHierarchy.Close()
        End Function

        Public Function GetGuidProperty(itemid As UInteger, propid As Integer, ByRef pguid As Guid) As Integer Implements IVsHierarchy.GetGuidProperty
            ' If trying to get the Project name, path
            ' We should return _projectFilePath
            _nexusClientHierarchy.GetGuidProperty(itemid, propid, pguid)
        End Function

        Public Function SetGuidProperty(itemid As UInteger, propid As Integer, ByRef rguid As Guid) As Integer Implements IVsHierarchy.SetGuidProperty
            _nexusClientHierarchy.SetGuidProperty(itemid, propid, rguid)
        End Function

        Public Function GetProperty(itemid As UInteger, propid As Integer, ByRef pvar As Object) As Integer Implements IVsHierarchy.GetProperty
            _nexusClientHierarchy.GetProperty(itemid, propid, pvar)
        End Function

        Public Function SetProperty(itemid As UInteger, propid As Integer, var As Object) As Integer Implements IVsHierarchy.SetProperty
            _nexusClientHierarchy.SetProperty(itemid, propid, var)
        End Function

        Public Function GetNestedHierarchy(itemid As UInteger, ByRef iidHierarchyNested As Guid, ByRef ppHierarchyNested As IntPtr, ByRef pitemidNested As UInteger) As Integer Implements IVsHierarchy.GetNestedHierarchy
            _nexusClientHierarchy.GetNestedHierarchy(itemid, iidHierarchyNested, ppHierarchyNested, pitemidNested)
        End Function

        Public Function GetCanonicalName(itemid As UInteger, ByRef pbstrName As String) As Integer Implements IVsHierarchy.GetCanonicalName
            
            'We should return a valid pbstrName that matches the one in the server
            If itemid = VSConstants.VSITEMID.Root Then
                pbstrName = _projectFilePath
                pbstrName = pbstrName.ToLower()
                Return NativeMethods.S_OK
            End If

            Return _nexusClientHierarchy.GetCanonicalName(itemid, pbstrName)
        End Function

        Public Function ParseCanonicalName(pszName As String, ByRef pitemid As UInteger) As Integer Implements IVsHierarchy.ParseCanonicalName
            Return _nexusClientHierarchy.ParseCanonicalName(pszName, pitemid)
        End Function

        Public Function Unused0() As Integer Implements IVsHierarchy.Unused0
            Throw New NotImplementedException()
        End Function

        Public Function AdviseHierarchyEvents(pEventSink As IVsHierarchyEvents, ByRef pdwCookie As UInteger) As Integer Implements IVsHierarchy.AdviseHierarchyEvents
            Throw New NotImplementedException()
        End Function

        Public Function UnadviseHierarchyEvents(dwCookie As UInteger) As Integer Implements IVsHierarchy.UnadviseHierarchyEvents
            Throw New NotImplementedException()
        End Function

        Public Function Unused1() As Integer Implements IVsHierarchy.Unused1
            Throw New NotImplementedException()
        End Function

        Public Function Unused2() As Integer Implements IVsHierarchy.Unused2
            Throw New NotImplementedException()
        End Function

        Public Function Unused3() As Integer Implements IVsHierarchy.Unused3
            Throw New NotImplementedException()
        End Function

        Public Function Unused4() As Integer Implements IVsHierarchy.Unused4
            Throw New NotImplementedException()
        End Function

        Public Function IsDocumentInProject(pszMkDocument As String, <Out> ByRef pfFound As Integer, pdwPriority As VSDOCUMENTPRIORITY(), <Out> ByRef pitemid As UInteger) As Integer Implements IVsProject.IsDocumentInProject
            Throw New NotImplementedException
        End Function

        Public Function GetMkDocument(itemid As UInteger, <Out> ByRef pbstrMkDocument As String) As Integer Implements IVsProject.GetMkDocument
            Return _vsProject.GetMkDocument(itemid, pbstrMkDocument)
        End Function

        Public Function OpenItem(itemid As UInteger, ByRef rguidLogicalView As Guid, punkDocDataExisting As IntPtr, <Out> ByRef ppWindowFrame As IVsWindowFrame) As Integer Implements IVsProject.OpenItem
            Return _vsProject.OpenItem(itemid, rguidLogicalView, punkDocDataExisting, ppWindowFrame)
        End Function

        Public Function GetItemContext(itemid As UInteger, <Out> ByRef ppSP As IServiceProvider) As Integer Implements IVsProject.GetItemContext
            Return _vsProject.GetItemContext(itemid, ppSP)
        End Function

        Public Function GenerateUniqueItemName(itemidLoc As UInteger, pszExt As String, pszSuggestedRoot As String, <Out> ByRef pbstrItemName As String) As Integer Implements IVsProject.GenerateUniqueItemName
            Return _vsProject.GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, pbstrItemName)
        End Function

        Public Function AddItem(itemidLoc As UInteger, dwAddItemOperation As VSADDITEMOPERATION, pszItemName As String, cFilesToOpen As UInteger, rgpszFilesToOpen As String(), hwndDlgOwner As IntPtr, pResult As VSADDRESULT()) As Integer Implements IVsProject.AddItem
            Return _vsProject.AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult)
        End Function
    End Class

End Namespace
