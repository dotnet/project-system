' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
