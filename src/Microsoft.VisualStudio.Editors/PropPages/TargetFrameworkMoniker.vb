' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Runtime.Versioning

Imports EnvDTE

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Represents a target framework moniker and can be placed into a control
    ''' </summary>
    Friend Class TargetFrameworkMoniker

        ''' <summary>
        ''' Stores the target framework moniker
        ''' </summary>
        Private ReadOnly _moniker As String

        ''' <summary>
        ''' Stores the display name of the target framework moniker
        ''' </summary>
        Private ReadOnly _displayName As String

        ''' <summary>
        ''' Constructor that uses the target framework moniker and display name provided by DTAR
        ''' </summary>
        Public Sub New(moniker As String, displayName As String)

            _moniker = moniker
            _displayName = displayName

        End Sub

        ''' <summary>
        ''' Gets the target framework moniker
        ''' </summary>
        Public ReadOnly Property Moniker As String
            Get
                Return _moniker
            End Get
        End Property

        ''' <summary>
        ''' Use the display name provided by DTAR for the string display
        ''' </summary>
        Public Overrides Function ToString() As String
            Return _displayName
        End Function

        ''' <summary>
        ''' Gets the supported target framework monikers from DTAR
        ''' </summary>
        Public Shared Function GetSupportedTargetFrameworkMonikers(
                frameworkMultiTargeting As IVsFrameworkMultiTargeting,
                project As Project,
                converter As TypeConverter) As IReadOnlyList(Of TargetFrameworkMoniker)

            Dim provider = GetSupportedTargetFrameworksProvider(frameworkMultiTargeting, project, converter)

            Dim moniker As [Property] = project.Properties.Item(ApplicationPropPage.Const_TargetFrameworkMoniker)

            Dim framework = New FrameworkName(CStr(moniker.Value))

            Return provider.GetSupportedTargetFrameworks(framework)

        End Function

        Private Shared Function GetSupportedTargetFrameworksProvider(
                frameworkMultiTargeting As IVsFrameworkMultiTargeting,
                project As Project,
                converter As TypeConverter) As ISupportedTargetFrameworksProvider

            ' CPS provides a type converter via a project property that 
            ' returns the supported frameworks, use them if available.
            If (converter IsNot Nothing) Then
                Return New TypeConverterTargetProvider(converter)
            End If

            ' Is this Web Application project?
            If IsWebProject(project) Then
                Return New WebFrameworkMultiTargetProvider(frameworkMultiTargeting)
            End If

            ' Otherwise, a legacy project
            Return New FrameworkMultiTargetProvider(frameworkMultiTargeting)

        End Function

        Private Shared Function IsWebProject(project As Project) As Boolean

            ' Determine if the project is a WAP (Web Application Project).
            For i As Integer = 1 To project.Properties.Count
                If project.Properties.Item(i).Name.StartsWith("WebApplication.") Then
                    Return True
                End If
            Next

            Return False

        End Function

    End Class

End Namespace
