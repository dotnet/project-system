' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Runtime.Versioning

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Provides an implementation of <see cref="ISupportedTargetFrameworksProvider"/> that returns 
    ''' frameworks from a <see cref="TypeConverter"/>.
    ''' </summary>
    Friend Class TypeConverterTargetProvider
        Implements ISupportedTargetFrameworksProvider

        Private ReadOnly _converter As TypeConverter

        Public Sub New(converter As TypeConverter)
            Assumes.NotNull(converter)

            _converter = converter
        End Sub

        Public Function GetSupportedTargetFrameworks(framework As FrameworkName) As IReadOnlyList(Of TargetFrameworkMoniker) Implements ISupportedTargetFrameworksProvider.GetSupportedTargetFrameworks

            Requires.NotNull(framework, NameOf(framework))

            ' CPS-based projects implement a enum property that ends up delegating onto
            ' SupportedTargetFrameworkAliasEnumProvider, which ends up reading from evaluation
            Dim monikers As IEnumerable(Of String) = _converter.GetStandardValues() _
                                                               .Cast(Of String)

            Return monikers.Select(Function(moniker)
                                       Dim displayName As String = CStr(_converter.ConvertTo(moniker, GetType(String)))

                                       Return New TargetFrameworkMoniker(moniker, displayName)
                                   End Function) _
                           .ToList()

        End Function

    End Class

End Namespace
