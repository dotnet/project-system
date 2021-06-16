' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Xunit
Imports Microsoft.VisualStudio.Editors.Common
Imports System.IO

Namespace Microsoft.VisualStudio.Editors.UnitTests
    Public Class UnitTest1
        <Fact>
        Sub TestSub()
            Dim stream As New MemoryStream()
            Utils.SerializationProvider.Serialize(stream, "Caketown")
        End Sub
    End Class
End Namespace

