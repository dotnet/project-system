' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace My.Resources


    'Hide the Microsoft_VisualStudio_AppDesigner_Designer class.  To keep the .resources file
    '  with the same fully-qualified name in the assembly manifest, we need to have the
    '  Designer.resx file actually named "Microsoft.VisualStudio.Editors.Designer.resx",
    '  or else change the project's root namespace which I don't want to do at this point.
    '  But then the class name gets generated as "Microsoft_VisualStudio_AppDesigner_Designer".
    'So hide that one and introduce a "Designer" class instead.
    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
    Partial Friend Class Microsoft_VisualStudio_AppDesigner_Designer

        ''' <summary>
        ''' Temporary compatibility function to make converting from Designer.txt to Designer.resx easier.
        ''' Just returns the input string unless there are arguments, in which case it calls String.Format.
        ''' </summary>
        ''' <param name="s"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Public Shared Function GetString(s As String, ParamArray Arguments() As Object) As String
            If Arguments Is Nothing OrElse Arguments.Length = 0 Then
                Return s
            Else
                Return String.Format(s, Arguments)
            End If
        End Function

    End Class

End Namespace


