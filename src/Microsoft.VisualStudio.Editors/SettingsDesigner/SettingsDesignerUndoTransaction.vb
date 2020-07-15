' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Wrapper around a DesignerTransaction. Since we don't always have a designer host available,
    ''' we can't assume that we can create a transaction. This class allows the use of a "Using" statement
    ''' regardless of if a designer host is available or not...
    ''' </summary>
    Friend NotInheritable Class SettingsDesignerUndoTransaction
        Implements IDisposable

        ''' <summary>
        ''' Our wrapped transaction, or nothing if not designer host is available
        ''' </summary>
        Private _transaction As DesignerTransaction

        ''' <summary>
        ''' Create a new instance
        ''' </summary>
        ''' <param name="Provider"></param>
        ''' <param name="Description"></param>
        Public Sub New(Provider As IServiceProvider, Description As String)
            If Provider IsNot Nothing Then
                Initialize(DirectCast(Provider.GetService(GetType(IDesignerHost)), IDesignerHost), Description)
            Else
                Initialize(Nothing, Description)
            End If
        End Sub

        ''' <summary>
        ''' Create a new instance
        ''' </summary>
        ''' <param name="DesignerHost"></param>
        ''' <param name="Description"></param>
        Public Sub New(DesignerHost As IDesignerHost, Description As String)
            Initialize(DesignerHost, Description)
        End Sub

        ''' <summary>
        ''' Common initialization of this instance
        ''' </summary>
        ''' <param name="DesignerHost"></param>
        ''' <param name="Description"></param>
        Private Sub Initialize(DesignerHost As IDesignerHost, Description As String)
            If DesignerHost IsNot Nothing Then
                _transaction = DesignerHost.CreateTransaction(Description)
            End If
        End Sub

        ''' <summary>
        ''' Cancel this transaction (if any)
        ''' </summary>
        ''' <remarks>If no wrapped transaction is available, this is a nop</remarks>
        Public Sub Cancel()
            If _transaction IsNot Nothing Then
                _transaction.Cancel()
            End If
        End Sub

        ''' <summary>
        ''' Commit this transaction (if any)
        ''' </summary>
        ''' <remarks>If no wrapped transaction is available, this is a nop</remarks>
        Public Sub Commit()
            If _transaction IsNot Nothing Then
                _transaction.Commit()
            End If
        End Sub

        ''' <summary>
        ''' Cancel the transaction (if any)
        ''' </summary>
        ''' <remarks>Allows for a "using" statement</remarks>
        Public Sub Dispose() Implements IDisposable.Dispose
            If _transaction IsNot Nothing AndAlso Not _transaction.Committed() Then
                _transaction.Cancel()
            End If
        End Sub

    End Class

End Namespace
