' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Imports Microsoft.VisualStudio.ManagedInterfaces.ProjectDesigner

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' This property wraps a PropertyDescriptor that was received from the project system or a user-defined property descriptor for 
    '''   user-persisted properties (UserPropertyDescriptor).
    '''   We wrap the real property descriptor that we are given before passing it to the IComponentChangeService so that we can intercept 
    '''   GetValue and SetValue and redirect them through the property page's IVsProjectDesignerPage.GetProperty/SetProperty instead of going 
    '''   directly to the propdescriptor (and hence the project).  This allows us exact control of undo and redo and how properties
    '''   are set and got.
    ''' </summary>
    Public NotInheritable Class PropertyPagePropertyDescriptor
        Inherits PropertyDescriptor

        'The property descriptor for the Project or Config property we wrap
        Private ReadOnly _propDesc As PropertyDescriptor
        Private ReadOnly _typeConverter As TypeConverter
        Private ReadOnly _displayName As String
        Private ReadOnly _name As String
        Private ReadOnly _propertyType As Type
        Private ReadOnly _isReadOnly As Boolean

        ''' <summary>
        ''' Constructs a PropertyDescriptor using the wrapped properties property descriptor
        ''' </summary>
        ''' <param name="PropDesc">The property descriptor that is being wrapped.</param>
        Public Sub New(PropDesc As PropertyDescriptor, PropertyName As String)
            MyBase.New(PropDesc)

            Debug.Assert(PropDesc IsNot Nothing)

            _propDesc = PropDesc

            If _propDesc.PropertyType.IsEnum Then
                _propertyType = _propDesc.PropertyType.UnderlyingSystemType
                _typeConverter = New TypeConverter()
            Else
                _propertyType = _propDesc.PropertyType
                _typeConverter = _propDesc.Converter
            End If
            _displayName = _propDesc.DisplayName
            _name = PropertyName
            _isReadOnly = _propDesc.IsReadOnly
        End Sub

        ''' <summary>
        ''' Returns the type of the instance this property is bound to, which is PropPageDesignerRootComponent.
        ''' </summary>
        ''' <value>The component type.</value>
        Public Overrides ReadOnly Property ComponentType As Type
            Get
                Return GetType(PropPageDesignerRootComponent)
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
        ''' Returns the type of the property.
        ''' </summary>
        ''' <value>A Type that represents the type of the property.</value>
        Public Overrides ReadOnly Property PropertyType As Type
            Get
                Return _propertyType
            End Get
        End Property

        ''' <summary>
        '''  Gets the current value of the property on the specified PropPageDesignerRootComponent instance.
        ''' </summary>
        ''' <param name="Component">The component instance with the property to retrieve the value.</param>
        ''' <returns>The value of the property on the specified component instance.</returns>
        Public Overrides Function GetValue(Component As Object) As Object
            Debug.Assert(Component IsNot Nothing, "component is Nothing!!!")
            If TypeOf Component Is PropPageDesignerRootComponent Then
                Dim View As PropPageDesignerView = DirectCast(Component, PropPageDesignerRootComponent).RootDesigner.GetView()
                Debug.Assert(View IsNot Nothing)
                Return View.GetProperty(Name)
            Else
                Debug.Fail("PropertyPagePropertyDescriptor.GetValue() called with unexpected Component type.  Expected that this is also set up through the PropPageDesignerView (implementing IProjectDesignerPropertyPageUndoSite)")
                Throw AppDesCommon.CreateArgumentException(NameOf(Component))
            End If
        End Function

        ''' <summary>
        '''  Sets the value of the property on the specified component instance to a different value.
        ''' </summary>
        ''' <param name="Component">The component instance with the property to set the value.</param>
        ''' <param name="Value">The new value to set the property to.</param>
        Public Overrides Sub SetValue(Component As Object, Value As Object)
            Debug.Fail("This shouldn't get called directly - instead the serialization store should be calling in to the prop page designer view")
            If TypeOf Component Is PropPageDesignerRootComponent Then
                Dim View As PropPageDesignerView
                Dim PropPageUndo As IVsProjectDesignerPage
                View = DirectCast(Component, PropPageDesignerRootComponent).RootDesigner.GetView()
                PropPageUndo = TryCast(View.PropPage, IVsProjectDesignerPage)
                Debug.Assert(PropPageUndo IsNot Nothing, "How could this happen?")
                View.SetProperty(Name, Value)
            Else
                Debug.Fail("PropertyPagePropertyDescriptor.SetValue() called with unexpected Component type.  Expected that this is also set up through the PropPageDesignerView (implementing IProjectDesignerPropertyPageUndoSite)")
                Throw AppDesCommon.CreateArgumentException(NameOf(Component))
            End If
        End Sub

        ''' <summary>
        '''  Indicates whether the value of this property needs to be persisted.
        ''' </summary>
        ''' <param name="Component">The component instance with the property to be examined for persistence.</param>
        ''' <returns>TRUE if the property should be persisted. Otherwise, FALSE.</returns>
        ''' <remarks>Since these properties are for the shell's Property Window only, none needs to be persisted.</remarks>
        Public Overrides Function ShouldSerializeValue(Component As Object) As Boolean
            Return False
        End Function

        ''' <summary>
        '''  Indicates whether resetting an object changes its value.
        ''' </summary>
        ''' <param name="Component">The component instance to test for reset capability.</param>
        Public Overrides Function CanResetValue(Component As Object) As Boolean
            Return False
        End Function

        ''' <summary>
        '''  Resets the value of this property of the component instance to the default value.
        ''' </summary>
        ''' <param name="Component">The component instance with the property value that is to be reset.</param>
        ''' <remarks>Not implemented since there is no 'reset'.</remarks>
        Public Overrides Sub ResetValue(Component As Object)
            Debug.Fail("No ResetValue implementation!!!  Shouldn't have been enabled in the properties window because CanResetValue always returns False.")
        End Sub

        ''' <summary>
        ''' Returns the converter for the contained property
        ''' </summary>
        Public Overrides ReadOnly Property Converter As TypeConverter
            Get
                Return _typeConverter
            End Get
        End Property

        ''' <summary>
        ''' Returns the name of the property.
        ''' </summary>
        Public Overrides ReadOnly Property Name As String
            Get
                If _name = "" Then
                    Return MyBase.Name
                Else
                    Return _name
                End If
            End Get
        End Property

        ''' <summary>
        ''' Retrieves the display name of the property.  This is the name that will
        ''' be displayed in a properties window.  This will be the same as the property
        ''' name for most properties.
        ''' </summary>
        Public Overrides ReadOnly Property DisplayName As String
            Get
                Return _displayName
            End Get
        End Property

        Public Overrides Function GetChildProperties(instance As Object, filter As Attribute()) As PropertyDescriptorCollection
            If _propDesc IsNot Nothing Then
                Return _propDesc.GetChildProperties(instance, filter)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets an editor of the specified type.
        ''' </summary>
        ''' <param name="editorBaseType"></param>
        Public Overrides Function GetEditor(editorBaseType As Type) As Object
            If _propDesc IsNot Nothing Then
                Return _propDesc.GetEditor(editorBaseType)
            End If
            Return Nothing
        End Function

    End Class

End Namespace

