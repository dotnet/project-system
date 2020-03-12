' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors

    Public NotInheritable Class Constants

        '
        ' Window Styles
        '
        Public Const WS_CHILD As Integer = &H40000000L
        Public Const WS_CLIPSIBLINGS As Integer = &H4000000L

    End Class

    <ComVisible(False)>
    Friend Enum VSITEMIDAPPDES As UInteger
        NIL = &HFFFFFFFFUI '-1
        ROOT = &HFFFFFFFEUI '-2
        SELECTION = &HFFFFFFFDUI '-3
    End Enum

End Namespace
