' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.Versioning
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Provides an implementation of <see cref="ISupportedTargetFrameworksProvider"/> that returns frameworks
    ''' from <see cref="IVsFrameworkMultiTargeting"/> applicable to a specified framework.
    ''' </summary>
    Friend Class FrameworkMultiTargetProvider
        Implements ISupportedTargetFrameworksProvider

        Private ReadOnly _frameworkMultiTargeting As IVsFrameworkMultiTargeting

        Public Sub New(frameworkMultiTargeting As IVsFrameworkMultiTargeting)
            Assumes.NotNull(frameworkMultiTargeting)

            _frameworkMultiTargeting = frameworkMultiTargeting
        End Sub

        Protected ReadOnly Property FrameworkMultiTargeting As IVsFrameworkMultiTargeting
            Get
                Return _frameworkMultiTargeting
            End Get
        End Property

        Public Function GetSupportedTargetFrameworks(framework As FrameworkName) As IReadOnlyList(Of TargetFrameworkMoniker) Implements ISupportedTargetFrameworksProvider.GetSupportedTargetFrameworks

            Requires.NotNull(framework, NameOf(framework))

            Dim frameworks = GetSupportedFrameworkNames(framework)

            Return frameworks.Select(Function(frameworkName)
                                         Return New TargetFrameworkMoniker(frameworkName.FullName, GetDisplayName(frameworkName))
                                     End Function) _
                             .ToList()

        End Function

        Protected Overridable Function CanRetargetTo(current As FrameworkName, framework As FrameworkName) As Boolean

            ' We don't let you retarget from one framework to another, because they are often very different project types
            Return String.Equals(current.Identifier, framework.Identifier, StringComparison.OrdinalIgnoreCase)

        End Function

        Private Function GetSupportedFrameworkNames(current As FrameworkName) As IEnumerable(Of FrameworkName)
            Dim monikers As Array = Nothing

            VSErrorHandler.ThrowOnFailure(FrameworkMultiTargeting.GetSupportedFrameworks(monikers))

            Return monikers.Cast(Of String) _
                           .Select(Function(moniker) New FrameworkName(moniker)) _
                           .Where(Function(framework) CanRetargetTo(current, framework))

        End Function

        Private Function GetDisplayName(frameworkName As FrameworkName) As String

            Dim displayName As String = ""
            VSErrorHandler.ThrowOnFailure(FrameworkMultiTargeting.GetDisplayNameForTargetFx(frameworkName.FullName, displayName))

            Return displayName

        End Function

    End Class

End Namespace
