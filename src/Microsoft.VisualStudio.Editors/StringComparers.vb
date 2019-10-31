' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
