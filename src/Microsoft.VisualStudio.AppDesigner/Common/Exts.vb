Imports System.Runtime.CompilerServices

Namespace Global.Microsoft
    Public Module Exts

        Public Enum Clusivity As Integer
            Ex = -1
            [In] = 0
        End Enum

        <Extension>
        Public Function IsBetween(Value As Integer,
                              LowerValue As Integer,
                              UpperValue As Integer,
                     Optional LowerClusivity As Clusivity = Clusivity.In,
                     Optional UpperClusivity As Clusivity = Clusivity.Ex
                             ) As Boolean
            Return (LowerValue.CompareTo(Value) <= LowerClusivity) AndAlso
               (Value.CompareTo(UpperValue) <= UpperClusivity)
        End Function
    End Module

End Namespace
