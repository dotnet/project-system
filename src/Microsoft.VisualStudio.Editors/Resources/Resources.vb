' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace My.Resources

    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
    Partial Friend Class Microsoft_VisualStudio_Editors_Designer

        ''' <summary>
        ''' Temporary compatibility function to make converting from Designer.txt to Designer.resx easier.
        ''' Just returns the input string unless there are arguments, in which case it calls String.Format.
        ''' </summary>
        ''' <param name="s"></param>
        <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
        Friend Shared Function GetString(s As String, ParamArray Arguments() As Object) As String
            If Arguments Is Nothing OrElse Arguments.Length = 0 Then
                Return s
            Else
                Return String.Format(s, Arguments)
            End If
        End Function

        ''' <summary>
        ''' These are some string resource IDs (just the resource ID name, not the 
        '''   actual string value).  These are not automatically kept up to date from
        '''   the .resx file, so they must be edited manually.
        ''' </summary>
        Friend Class ConstantResourceIDs

            'IMPORTANT: These must be kept manually up to date, they are not automatically
            '  synchronized with the .resx file.

            Friend Const PPG_WebReferenceNameDescription As String = "PPG_WebReferenceNameDescription"
            Friend Const PPG_ServiceReferenceNamespaceDescription As String = "PPG_ServiceReferenceNamespaceDescription"
            Friend Const PPG_UrlBehaviorName As String = "PPG_UrlBehaviorName"
            Friend Const PPG_UrlBehaviorDescription As String = "PPG_UrlBehaviorDescription"
            Friend Const PPG_WebReferenceUrlName As String = "PPG_WebReferenceUrlName"
            Friend Const PPG_WebReferenceUrlDescription As String = "PPG_WebReferenceUrlDescription"
            Friend Const PPG_ServiceReferenceUrlName As String = "PPG_ServiceReferenceUrlName"
            Friend Const PPG_ServiceReferenceUrlDescription As String = "PPG_ServiceReferenceUrlDescription"

        End Class

    End Class
End Namespace
