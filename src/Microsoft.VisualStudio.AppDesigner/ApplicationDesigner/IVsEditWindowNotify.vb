' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    '''  The interface was implemented by PropPageDesignerView, the appDesigner view will fire this event when the current designer is activated or deactivated...
    ''' </summary>
    <ComVisible(False)>
    Public Interface IVsEditWindowNotify
        Sub OnActivated(activated As Boolean)
    End Interface

End Namespace
