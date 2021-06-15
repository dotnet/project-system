Imports System
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

