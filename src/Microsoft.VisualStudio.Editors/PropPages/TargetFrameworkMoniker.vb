' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public ReadOnly Property Moniker() As String
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

        'TODO: Remove this hardcoded list (Refer Bug: #795)
        Private Shared Function AddDotNetCoreFramework(prgSupportedFrameworks As Array, supportedTargetFrameworksDescriptor As PropertyDescriptor) As Array
            Dim _TypeConverter As TypeConverter = supportedTargetFrameworksDescriptor.Converter
            If _TypeConverter IsNot Nothing Then
                Dim supportedFrameworksList As List(Of String) = New List(Of String)
                For Each moniker As String In prgSupportedFrameworks
                    supportedFrameworksList.Add(moniker)
                Next

                For Each frameworkValue In _TypeConverter.GetStandardValues()
                    Dim framework = CStr(frameworkValue)
                    If framework IsNot Nothing Then
                        supportedFrameworksList.Add(framework)
                    End If
                Next

                Return supportedFrameworksList.ToArray
            End If

            Return prgSupportedFrameworks
        End Function

        ''' <summary>
        ''' Gets the supported target framework monikers from DTAR
        ''' </summary>
        ''' <param name="vsFrameworkMultiTargeting"></param>
        Public Shared Function GetSupportedTargetFrameworkMonikers(
            vsFrameworkMultiTargeting As IVsFrameworkMultiTargeting,
            currentProject As Project,
            supportedTargetFrameworksDescriptor As PropertyDescriptor) As IEnumerable(Of TargetFrameworkMoniker)

            Dim supportedFrameworksArray As Array = Nothing
            VSErrorHandler.ThrowOnFailure(vsFrameworkMultiTargeting.GetSupportedFrameworks(supportedFrameworksArray))
            If supportedTargetFrameworksDescriptor IsNot Nothing Then
                supportedFrameworksArray = AddDotNetCoreFramework(supportedFrameworksArray, supportedTargetFrameworksDescriptor)
            End If

            Dim targetFrameworkMonikerProperty As [Property] = currentProject.Properties.Item(ApplicationPropPage.Const_TargetFrameworkMoniker)
            Dim currentTargetFrameworkMoniker As String = CStr(targetFrameworkMonikerProperty.Value)
            Dim currentFrameworkName As New FrameworkName(currentTargetFrameworkMoniker)

            Dim supportedTargetFrameworkMonikers As New List(Of TargetFrameworkMoniker)
            Dim hashSupportedTargetFrameworkMonikers As New HashSet(Of String)

            ' Determine if the project is a WAP (Web Application Project).
            Dim isWebProject As Boolean = False
            For i As Integer = 1 To currentProject.Properties.Count
                If currentProject.Properties.Item(i).Name.StartsWith("WebApplication.") Then
                    isWebProject = True
                    Exit For
                End If
            Next

            ' UNDONE: DTAR may currently send back duplicate monikers, so explicitly filter them out for now
            For Each moniker As String In supportedFrameworksArray
                If hashSupportedTargetFrameworkMonikers.Add(moniker) Then

                    Dim frameworkName As New FrameworkName(moniker)

                    ' Filter out frameworks with a different identifier since they are not applicable to the current project type
                    If String.Compare(frameworkName.Identifier, currentFrameworkName.Identifier, StringComparison.OrdinalIgnoreCase) = 0 Then

                        If isWebProject Then

                            ' Web projects don't support profiles when targeting below 4.0, so filter those out
                            If frameworkName.Version.Major < 4 AndAlso
                               Not String.IsNullOrEmpty(frameworkName.Profile) Then
                                Continue For
                            End If

                            ' For web projects, filter out frameworks that don't contain System.Web (e.g. client profiles).
                            Dim systemWebPath As String = Nothing
                            If VSErrorHandler.Failed(vsFrameworkMultiTargeting.ResolveAssemblyPath(
                                  "System.Web.dll",
                                  moniker,
                                  systemWebPath)) OrElse
                               String.IsNullOrEmpty(systemWebPath) Then
                                Continue For
                            End If
                        End If

                        ' Use DTAR to get the display name corresponding to the moniker
                        Dim displayName As String = ""
                        If String.Compare(frameworkName.Identifier, ".NETStandard", StringComparison.Ordinal) = 0 OrElse
                           String.Compare(frameworkName.Identifier, ".NETCoreApp", StringComparison.Ordinal) = 0 Then
                            displayName = CStr(supportedTargetFrameworksDescriptor.Converter?.ConvertTo(moniker, GetType(String)))
                        Else
                            VSErrorHandler.ThrowOnFailure(vsFrameworkMultiTargeting.GetDisplayNameForTargetFx(moniker, displayName))
                        End If

                        supportedTargetFrameworkMonikers.Add(New TargetFrameworkMoniker(moniker, displayName))

                    End If
                End If
            Next

            Return supportedTargetFrameworkMonikers

        End Function
    End Class

End Namespace
