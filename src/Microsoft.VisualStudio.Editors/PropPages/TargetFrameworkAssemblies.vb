' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Support for retrieving and working with the available target framework assemblies
    '''   for a project.
    ''' </summary>
    Friend Class TargetFrameworkAssemblies

        ''' <summary>
        ''' Represents a supported target framework assembly.  Can be placed directly into 
        '''   a listbox or combobox (it will show the Description text in the listbox)
        ''' </summary>
        Friend Class TargetFramework
            Private ReadOnly _version As UInteger
            Private ReadOnly _description As String

            Public Sub New(version As UInteger, description As String)
                Requires.NotNull(description, NameOf(description))

                _version = version
                _description = description
            End Sub

            Public ReadOnly Property Version As UInteger
                Get
                    Return _version
                End Get
            End Property

            Public ReadOnly Property Description As String
                Get
                    Return _description
                End Get
            End Property

            ''' <summary>
            ''' Provides the text to show inside of a combobox/listbox
            ''' </summary>
            Public Overrides Function ToString() As String
                Return _description
            End Function
        End Class


        ''' <summary>
        ''' Retrieves the set of target framework assemblies that are supported
        ''' </summary>
        ''' <param name="vsTargetFrameworkAssemblies"></param>
        Public Shared Function GetSupportedTargetFrameworkAssemblies(vsTargetFrameworkAssemblies As IVsTargetFrameworkAssemblies) As IEnumerable(Of TargetFramework)
            Dim versions As UInteger() = GetSupportedTargetFrameworkAssemblyVersions(vsTargetFrameworkAssemblies)
            Dim targetFrameworks As New List(Of TargetFramework)
            For Each version As UInteger In versions
                targetFrameworks.Add(New TargetFramework(version, GetTargetFrameworkDescriptionFromVersion(vsTargetFrameworkAssemblies, version)))
            Next

            Return targetFrameworks.ToArray()
        End Function

        ''' <summary>
        ''' Retrieve the localized description string for a given target framework
        '''   version number.
        ''' </summary>
        ''' <param name="vsTargetFrameworkAssemblies"></param>
        ''' <param name="version"></param>
        Private Shared Function GetTargetFrameworkDescriptionFromVersion(vsTargetFrameworkAssemblies As IVsTargetFrameworkAssemblies, version As UInteger) As String
            Dim pszDescription As String = Nothing
            VSErrorHandler.ThrowOnFailure(vsTargetFrameworkAssemblies.GetTargetFrameworkDescription(version, pszDescription))
            Return pszDescription
        End Function

        ''' <summary>
        ''' Retrieve the list of assemblies versions (as uint) that are supported
        ''' </summary>
        ''' <param name="vsTargetFrameworkAssemblies"></param>
        Private Shared Function GetSupportedTargetFrameworkAssemblyVersions(vsTargetFrameworkAssemblies As IVsTargetFrameworkAssemblies) As UInteger()
            Dim targetFrameworkEnumerator As IEnumTargetFrameworks = Nothing
            VSErrorHandler.ThrowOnFailure(vsTargetFrameworkAssemblies.GetSupportedFrameworks(targetFrameworkEnumerator))
            Dim supportedFrameworks As New List(Of UInteger)
            While True
                Dim rgFrameworks(0) As UInteger
                Dim cReturned As UInteger
                If VSErrorHandler.Failed(targetFrameworkEnumerator.Next(1, rgFrameworks, cReturned)) OrElse cReturned = 0 Then
                    Exit While
                Else
                    supportedFrameworks.Add(rgFrameworks(0))
                End If
            End While

            Return supportedFrameworks.ToArray()
        End Function

    End Class

End Namespace
