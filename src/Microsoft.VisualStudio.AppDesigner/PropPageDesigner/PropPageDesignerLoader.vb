' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design.Serialization

Imports Microsoft.VisualStudio.Editors.AppDesCommon

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' Designer loader for the PropPageDesigner
    ''' </summary>
    Public NotInheritable Class PropPageDesignerLoader
        Inherits BasicDesignerLoader
        Implements IDisposable

        ''' <summary>
        ''' This method is called immediately after the first time
        '''   BeginLoad is invoked.  This is an appropriate place to
        '''   add custom services to the loader host.  Remember to
        '''   remove any custom services you add here by overriding
        '''   Dispose.
        ''' </summary>
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            'Add our ComponentSerializationService so that the basic designer will give us automatic Undo/Redo
            Dim SerializationService As New PropertyPageSerializationService(LoaderHost)
            LoaderHost.AddService(GetType(ComponentSerializationService), SerializationService)
            LoaderHost.AddService(GetType(Shell.Design.WindowPaneProviderService),
                New DeferrableWindowPaneProviderService(LoaderHost))
            Debug.Assert(GetService(GetType(ComponentSerializationService)) IsNot Nothing,
                "We just made the ComponentSerializationService service available.  Why isn't it there?")
        End Sub

        ''' <summary>
        ''' This is how we handle save (although it does not necessarily correspond
        ''' to the exact point at which the file is saved, just to when the IDE thinks
        ''' it needs an updated version of the file contents).
        ''' </summary>
        ''' <param name="serializationManager"></param>
        Protected Overrides Sub PerformFlush(serializationManager As IDesignerSerializationManager)
            Debug.Assert(Modified, "PerformFlush shouldn't get called if the designer's not dirty")

            If LoaderHost.RootComponent IsNot Nothing Then
                ' Make sure the property page changes have been flushed from the UI
                PropPageDesignerRootDesigner.CommitAnyPendingChanges()
            Else
                Debug.Fail("LoaderHost.RootComponent is Nothing")
            End If
        End Sub

        ''' <summary>
        ''' Initializes the designer.  We are not file based, so not much to do
        ''' </summary>
        ''' <param name="serializationManager"></param>
        ''' <remarks>
        ''' If the load fails, this routine should throw an exception.  That exception
        ''' will automatically be added to the ErrorList by VSDesignerLoader.  If there
        ''' are more specific local exceptions, they can be added to ErrorList manually.
        '''</remarks>
        Protected Overrides Sub PerformLoad(serializationManager As IDesignerSerializationManager)

            '... BasicDesignerLoader requires that we call SetBaseComponentClassName() during load.
            SetBaseComponentClassName(GetType(PropPageDesignerRootComponent).AssemblyQualifiedName)

            Dim NewPropPageDesignerRoot As PropPageDesignerRootComponent

            Using New WaitCursor
                Debug.Assert(LoaderHost IsNot Nothing, "No host")
                If LoaderHost IsNot Nothing Then
                    NewPropPageDesignerRoot = CType(LoaderHost.CreateComponent(GetType(PropPageDesignerRootComponent), "PropPageDesignerRootComponent"), PropPageDesignerRootComponent)
                End If
            End Using

        End Sub

#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Dispose of managed and unmanaged resources
        ''' </summary>
        ''' <param name="disposing">True if calling from Dispose()</param>
        Private Overloads Sub Dispose(disposing As Boolean)
            If disposing Then
                'Remove our ComponentSerializationService
                LoaderHost.RemoveService(GetType(ComponentSerializationService))
            End If
        End Sub

        ''' <summary>
        ''' Semi-standard IDisposable implementation
        ''' </summary>
        ''' <remarks>MyBase.Dispose called since base does not implement IDisposable</remarks>
        Public Overloads Overrides Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            MyBase.Dispose() 'Necessary because the base does not implement IDisposable
            GC.SuppressFinalize(Me)
        End Sub
#End Region
    End Class

End Namespace
