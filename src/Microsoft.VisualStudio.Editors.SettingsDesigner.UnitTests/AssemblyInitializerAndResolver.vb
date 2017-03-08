Imports System

Imports Microsoft.VisualStudio.TestTools.UnitTesting

' Helper class that resolves assemblies from the GAC. 
' Currently we only need to resolve Microsoft.VisualStudio.Editors 
' from the GAC...
<TestClass()> _
Public Class AssemblyInitializerAndResolver

    Private Shared WithEvents myCurrentDomain As System.AppDomain

    <AssemblyInitialize()> _
    Public Shared Sub Initialize(ByVal context As TestContext)
        myCurrentDomain = AppDomain.CurrentDomain
    End Sub

    <AssemblyCleanup()> _
    Public Shared Sub Cleanup()
        myCurrentDomain = Nothing
    End Sub

    Private Shared Function ResolveAssembly(ByVal sender As Object, ByVal e As ResolveEventArgs) As System.Reflection.Assembly Handles myCurrentDomain.AssemblyResolve
        Dim resolvedAssembly As System.Reflection.Assembly = Nothing
        If e.Name.StartsWith("Microsoft.VisualStudio.Editors") Then
            resolvedAssembly = System.Reflection.Assembly.Load("Microsoft.VisualStudio.Editors, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
        End If
        Return resolvedAssembly
    End Function
End Class
