' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Generator for strongly typed settings wrapper class
    ''' </summary>
    <Guid("3b4c204a-88a2-3af8-bcfd-cfcb16399541")>
    <ProvideObject(GetType(SettingsSingleFileGenerator))>
    <CodeGeneratorRegistration(GetType(SettingsSingleFileGenerator), "Generator for strongly typed settings class", VBPackage.LegacyVBPackageGuid, GeneratesSharedDesignTimeSource:=True)>
    <CodeGeneratorRegistration(GetType(SettingsSingleFileGenerator), "Generator for strongly typed settings class", VBPackage.LegacyCSharpPackageGuid, GeneratesSharedDesignTimeSource:=True)>
    Public Class SettingsSingleFileGenerator
        Inherits SettingsSingleFileGeneratorBase

        Public Const SingleFileGeneratorName As String = "SettingsSingleFileGenerator"

    End Class
End Namespace
