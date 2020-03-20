' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Represents the 'Install other frameworks...' item that appears in the target framework combo box
    ''' </summary>
    Friend Class InstallOtherFrameworksComboBoxValue

        Public Overrides Function ToString() As String
            Return My.Resources.Strings.InstallOtherFrameworks
        End Function

    End Class

End Namespace
