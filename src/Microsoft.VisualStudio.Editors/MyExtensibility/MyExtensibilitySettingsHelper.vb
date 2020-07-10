' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    Partial Friend Class MyExtensibilitySettings

        ''' ;AssemblyAutoOption
        ''' <summary>
        ''' Simple structure / class containing the assembly auto-add / auto-remove option.
        ''' </summary>
        Private Class AssemblyAutoOption

            Public Sub New(autoAdd As AssemblyOption, autoRemove As AssemblyOption)
                _autoAdd = autoAdd
                _autoRemove = autoRemove
            End Sub

            Public Property AutoAdd As AssemblyOption
                Get
                    Return _autoAdd
                End Get
                Set
                    _autoAdd = value
                End Set
            End Property

            Public Property AutoRemove As AssemblyOption
                Get
                    Return _autoRemove
                End Get
                Set
                    _autoRemove = value
                End Set
            End Property

            Private _autoAdd As AssemblyOption = AssemblyOption.Prompt
            Private _autoRemove As AssemblyOption = AssemblyOption.Prompt
        End Class
    End Class
End Namespace
