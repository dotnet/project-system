' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace My.Resources

    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
    Partial Friend Class Designer

        ''' <summary>
        ''' Temporary compatibility function to make converting from Designer.txt to Designer.resx easier.
        ''' Just returns the input string unless there are arguments, in which case it calls String.Format.
        ''' </summary>
        ''' <param name="s"></param>
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

