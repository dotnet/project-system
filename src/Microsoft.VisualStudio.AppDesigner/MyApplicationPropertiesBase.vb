' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.MyApplication

    Public Class MyApplicationPropertiesBase
        ''' <summary>
        ''' Returns the set of files that need to be checked out to change the given property
        ''' Must be overriden in sub-class
        ''' </summary>
        Public Overridable Function FilesToCheckOut(CreateIfNotExist As Boolean) As String()
            Return Array.Empty(Of String)
        End Function

    End Class ' Class MyApplicationPropertiesBase

End Namespace
