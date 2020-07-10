' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;INamedDescribedObject
    ''' <summary>
    ''' Shared interface implemented by MyExtensionsProjectFile and MyExtensionTemplate
    ''' to display them in a list view / list box.
    ''' </summary>
    Friend Interface INamedDescribedObject
        ReadOnly Property DisplayName As String
        ReadOnly Property Description As String
    End Interface
End Namespace
