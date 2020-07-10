' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio

    Friend NotInheritable Class StringComparers

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property SettingNames As StringComparer
            Get
                Return StringComparer.OrdinalIgnoreCase
            End Get
        End Property

        Public Shared ReadOnly Property ResourceNames As StringComparer
            Get
                Return StringComparer.OrdinalIgnoreCase
            End Get
        End Property

        Public Shared ReadOnly Property Paths As StringComparer
            Get
                Return StringComparer.OrdinalIgnoreCase
            End Get
        End Property

    End Class

End Namespace
