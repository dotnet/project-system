' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Validation Result,
    '''   Warning means this can be postponed for delay-validation
    '''   Failed means the user must fix this before leaving the page/field...
    ''' </summary>
    Public Enum ValidationResult
        Succeeded = 0
        Warning = 1
        Failed = 2
    End Enum

End Namespace
