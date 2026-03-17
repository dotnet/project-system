' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' WPF UserControl that provides a CPS-designer-styled UI for the Build property page.
    ''' This control is hosted inside the existing <see cref="BuildPropPage"/> via ElementHost,
    ''' bridging the WPF visual layer to the existing PropertyControlData binding infrastructure.
    ''' </summary>
    Partial Friend Class BuildPropPageWpfControl

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class

End Namespace
