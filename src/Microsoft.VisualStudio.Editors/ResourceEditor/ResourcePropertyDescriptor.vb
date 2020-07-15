' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.ComponentModel
Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Provides information about a specific property of a Resource instance.
    ''' </summary>
    Friend NotInheritable Class ResourcePropertyDescriptor
        Inherits PropertyDescriptor

        '======================================================================
        '= Fields =                                                           =
        '======================================================================

        ' Property names to show in Property Window.  Not localized.
        Public Const PROPERTY_NAME As String = "(Name)"
        Public Const PROPERTY_COMMENT As String = "Comment"
        Public Const PROPERTY_ENCODING As String = "Encoding"
        Public Const PROPERTY_FILENAME As String = "Filename"
        Public Const PROPERTY_FILETYPE As String = "FileType"
        Public Const PROPERTY_PERSISTENCE As String = "Persistence"
        Public Const PROPERTY_TYPE As String = "Type"
        Public Const PROPERTY_VALUE As String = "Value"

        'Category names to show in the Properties window
        Public Const CATEGORY_RESOURCE As String = "Resource"

        ' The type of the property
        Private ReadOnly _propertyType As Type

        ' Indicates whether this property is read-only or not
        Private ReadOnly _isReadOnly As Boolean

        ' Indicates whether the property can be reset or not
        Private ReadOnly _canReset As Boolean

        '======================================================================
        '= Constructors =                                                     =
        '======================================================================

        ''' <summary>
        ''' Initializes a new instance of ResourcePropertyDescriptor.
        ''' </summary>
        ''' <param name="Name">The name of the property.</param>
        ''' <param name="Type">The type of the property.</param>
        ''' <param name="IsReadOnly">TRUE means the property is read-only. Otherwise FALSE.</param>
        ''' <param name="CanReset">TRUE means the property should have a Reset option in the property browser. Otherwise FALSE.</param>
        ''' <param name="Attributes">Optional. An array of type Attribute containing the property's attributes.</param>
        ''' <remarks>Used by Resource class to describe itself.</remarks>
        Public Sub New(Name As String, Type As Type, IsReadOnly As Boolean,
               Optional CanReset As Boolean = False, Optional Attributes() As Attribute = Nothing)
            MyBase.New(Name, Attributes)
            _propertyType = Type
            _isReadOnly = IsReadOnly
            _canReset = CanReset
        End Sub

        '======================================================================
        '= Properties =                                                       =
        '======================================================================

        ''' <summary>
        ''' Returns the type of the instance this property is bound to, which is Resource.
        ''' </summary>
        ''' <value>The Resource type.</value>
        Public Overrides ReadOnly Property ComponentType As Type
            Get
                Return GetType(Resource)
            End Get
        End Property

        ''' <summary>
        '''  Returns a value indicating whether this property is read-only.
        ''' </summary>
        ''' <value>True if the property is read-only, False otherwise.</value>
        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return _isReadOnly
            End Get
        End Property

        ''' <summary>
        '''  Returns the type of the property.
        ''' </summary>
        ''' <value>A Type that represents the type of the property.</value>
        Public Overrides ReadOnly Property PropertyType As Type
            Get
                Return _propertyType
            End Get
        End Property

        '======================================================================
        '= Methods =                                                          =
        '======================================================================

        ''' <summary>
        '''  Gets the current value of the property on the specified Resource instance.
        ''' </summary>
        ''' <param name="Component">The Resource instance with the property to retrieve the value.</param>
        ''' <returns>The value of the property on the specified Resource instance.</returns>
        ''' <remarks>We delegate this to the Resource instance.</remarks>
        Public Overrides Function GetValue(Component As Object) As Object
            Debug.Assert(Component IsNot Nothing, "component is Nothing!!!")
            Debug.Assert(TypeOf Component Is Resource, "component is not a Resource!!!")
            Return DirectCast(Component, Resource).GetPropertyValue(Name)
        End Function

        ''' <summary>
        '''  Sets the value of the property on the specified Resource instance to a different value.
        ''' </summary>
        ''' <param name="Component">The Resource instance with the property to set the value.</param>
        ''' <param name="Value">The new value to set the property to.</param>
        ''' <remarks>We delegate this to the Resource instance.</remarks>
        Public Overrides Sub SetValue(Component As Object, Value As Object)
            Debug.Assert(Component IsNot Nothing, "ResourcePropertyDescriptor.SetValue: Component is Nothing")
            If Component IsNot Nothing Then
                Debug.Assert(TypeOf Component Is Resource, "ResourcePropertyDescriptor.SetValue: Component is not a Resource")
                Dim Resource = TryCast(Component, Resource)
                If Resource IsNot Nothing Then
                    Dim Site As ISite = GetSite(Resource)
                    Dim ChangeService As IComponentChangeService = Nothing
                    Dim oldValue As Object = Nothing

                    If Site IsNot Nothing Then
                        ChangeService = DirectCast(Site.GetService(GetType(IComponentChangeService)), IComponentChangeService)
                        Debug.Assert(ChangeService IsNot Nothing, "IComponentChangeService not found")
                    End If

                    If Not _isReadOnly Then
                        Dim NotifyComponentChange As Boolean = True
                        If Name = PROPERTY_NAME Then
                            'We must special-case the "Name" property.  In this case, we want the Undo engine
                            '  to pick up on the ComponentRename event only.  If we fire a component change
                            '  event here, too, it tends to complicate matters (there's confusion on Redo
                            '  as to whether we should be looking for the resource component via its old name
                            '  or new name, and the order of applying the component rename vs the name property
                            '  change is not defined).  So, we won't fire ComponentChanging/Changed for the
                            '  Name property.
                            NotifyComponentChange = False
                        End If

                        'Announce via ComponentChangeService that we are about to change this 
                        '  component.  The Undo engine listens to this service, so this is
                        '  required in order for Undo to work.
                        If ChangeService IsNot Nothing AndAlso NotifyComponentChange Then
                            oldValue = GetValue(Component)

                            'Note: the Fx property descriptor stuff catches and ignores checkout canceled
                            '  errors here.  We're more complex than that and want the exceptions to go
                            '  ahead and bubble up.  However, we ignore them in our View's DSMsgBox
                            '  implementation and don't bother showing them to the user.
                            ChangeService.OnComponentChanging(Component, Me)
                        End If

                        Resource.SetPropertyValueWithoutUndo(Name, Value)

                        ' Notify the change service that the change was successful.
                        If ChangeService IsNot Nothing AndAlso NotifyComponentChange Then
                            ChangeService.OnComponentChanged(Component, Me, oldValue, Value)
                        End If
                    Else
                        Debug.Fail("SetValue attempted on read-only property " & Name)
                    End If
                End If
            End If
        End Sub

        ''' <summary>
        '''  Indicates whether the value of this property needs to be persisted.
        ''' </summary>
        ''' <param name="Component">The Resource instance with the property to be examined for persistence.</param>
        ''' <returns>TRUE if the property should be persisted. Otherwise, FALSE.</returns>
        ''' <remarks>Since these properties are for the shell's Property Window only, none needs to be persisted.</remarks>
        Public Overrides Function ShouldSerializeValue(Component As Object) As Boolean
            Return False
        End Function

        ''' <summary>
        '''  Indicates whether resetting an object changes its value.
        ''' </summary>
        ''' <param name="Component">The Resource instance to test for reset capability.</param>
        ''' <returns>TRUE if resetting the Resource instance changes this property value; otherwise, FALSE.</returns>
        ''' <remarks>Since there is no 'reset' on a resource, we always return False.</remarks>
        Public Overrides Function CanResetValue(Component As Object) As Boolean
            Return _canReset
        End Function

        ''' <summary>
        '''  Resets the value of this property of the Resource instance to the default value.
        ''' </summary>
        ''' <param name="Component">The Resource instance with the property value that is to be reset.</param>
        ''' <remarks>Not implemented since there is no 'reset'.</remarks>
        Public Overrides Sub ResetValue(Component As Object)
            Debug.Assert(_canReset)
            Debug.Assert(Component IsNot Nothing, "ResourcePropertyDescriptor.ResetValue: Component is Nothing")
            If Component IsNot Nothing Then
                Debug.Assert(TypeOf Component Is Resource, "ResourcePropertyDescriptor.ResetValue: Component is not a Resource")
                Dim Resource = TryCast(Component, Resource)
                If Resource IsNot Nothing Then
                    Resource.ResetPropertyValue(Name)
                End If
            End If
        End Sub

    End Class

End Namespace

