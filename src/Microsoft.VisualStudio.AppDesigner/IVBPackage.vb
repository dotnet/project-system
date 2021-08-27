' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors

    Public Interface IVBPackage

        Function GetLastShownApplicationDesignerTab(projectHierarchy As IVsHierarchy) As Integer

        Sub SetLastShownApplicationDesignerTab(projectHierarchy As IVsHierarchy, tab As Integer)

        ReadOnly Property GetService(serviceType As Type) As Object

        ReadOnly Property MenuCommandService As IMenuCommandService

    End Interface

    Public Class VBPackageUtils

        Private Shared s_editorsPackage As IVBPackage
#Disable Warning IDE1006 ' Naming Styles (Compat)
        Public Delegate Function getServiceDelegate(ServiceType As Type) As Object
#Enable Warning IDE1006 ' Naming Styles
        ''' <param name="GetService"></param>
        Public Shared ReadOnly Property PackageInstance(GetService As getServiceDelegate) As IVBPackage
            Get
                If s_editorsPackage Is Nothing Then
                    Dim shell As IVsShell = DirectCast(GetService(GetType(IVsShell)), IVsShell)
                    Dim pPackage As IVsPackage = Nothing
                    If shell IsNot Nothing Then
                        Dim hr As Integer = shell.IsPackageLoaded(New Guid(My.Resources.Designer.VBPackage_GUID), pPackage)
                        Debug.Assert(NativeMethods.Succeeded(hr) AndAlso pPackage IsNot Nothing, "VB editors package not loaded?!?")
                    End If

                    s_editorsPackage = TryCast(pPackage, IVBPackage)
                End If
                Return s_editorsPackage
            End Get
        End Property
    End Class
End Namespace
