' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.Versioning
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Provides an implementation of <see cref="ISupportedTargetFrameworksProvider"/> that returns frameworks
    ''' from <see cref="IVsFrameworkMultiTargeting"/> applicable to web application projects.
    ''' </summary>
    Friend Class WebFrameworkMultiTargetProvider
        Inherits FrameworkMultiTargetProvider

        Public Sub New(frameworkMultiTargeting As IVsFrameworkMultiTargeting)
            MyBase.New(frameworkMultiTargeting)
        End Sub

        Protected Overrides Function CanRetargetTo(current As FrameworkName, framework As FrameworkName) As Boolean

            Return MyBase.CanRetargetTo(current, framework) AndAlso CanReferenceSystemWeb(framework)

        End Function

        Private Function CanReferenceSystemWeb(framework As FrameworkName) As Boolean

            ' Web projects don't support profiles when targeting below 4.0, so filter those out
            If framework.Version.Major < 4 Then
                Return String.IsNullOrEmpty(framework.Profile)
            End If

            ' For web projects, filter out frameworks that don't contain System.Web (e.g. client profiles).
            Dim systemWebPath As String = Nothing
            Return VSErrorHandler.Succeeded(FrameworkMultiTargeting.ResolveAssemblyPath(
                      "System.Web.dll",
                      framework.FullName,
                      systemWebPath)) AndAlso Not String.IsNullOrEmpty(systemWebPath)

        End Function

    End Class

End Namespace
