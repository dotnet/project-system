' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Generator for strongly typed settings wrapper class
    ''' </summary>
    <Guid("940f36b5-a42e-435e-8ef4-20b9d4801d22")>
    <ProvideObject(GetType(PublicSettingsSingleFileGenerator))>
    <CodeGeneratorRegistration(GetType(PublicSettingsSingleFileGenerator), "Generator for strongly typed settings class (public class)", VBPackage.LegacyVBPackageGuid, GeneratesSharedDesignTimeSource:=True)>
    <CodeGeneratorRegistration(GetType(PublicSettingsSingleFileGenerator), "Generator for strongly typed settings class (public class)", VBPackage.LegacyCSharpPackageGuid, GeneratesSharedDesignTimeSource:=True)>
    Public Class PublicSettingsSingleFileGenerator
        Inherits SettingsSingleFileGeneratorBase

        Public Const SingleFileGeneratorName As String = "PublicSettingsSingleFileGenerator"

        ''' <summary>
        ''' Returns the default visibility of this properties
        ''' </summary>
        ''' <value>MemberAttributes indicating what visibility to make the generated properties.</value>
        Friend Overrides ReadOnly Property SettingsClassVisibility As TypeAttributes
            Get
                Return TypeAttributes.Sealed Or TypeAttributes.Public
            End Get
        End Property

    End Class
End Namespace
