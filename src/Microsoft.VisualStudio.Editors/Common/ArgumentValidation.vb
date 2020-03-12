' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.Common

    Friend Module ArgumentValidation

        ''' <summary>
        ''' Creates an ArgumentException based on the name of the argument that is invalid.
        ''' </summary>
        ''' <param name="argumentName"></param>
        Public Function CreateArgumentException(argumentName As String) As Exception
            Return New ArgumentException(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.General_InvalidArgument_1Arg, argumentName))
        End Function
    End Module
End Namespace
