' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Design.Serialization
Imports System.IO
Imports System.Runtime.Serialization
Imports Microsoft.VisualStudio.Editors.Common

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    <Serializable>
    Friend NotInheritable Class GenericComponentSerializationStore
        Inherits SerializationStore
        Implements ISerializable

        'The set of objects (IComponent instances or properties) that we wish to
        '  "serialize" into this store.  The actual values won't be serialized
        '  until we're Close:d, until then this just keeps track of what we
        '  want to serialize.  It will be cleared out when we Close.
        'Private m_HashedObjectsToSerialize As New Dictionary(Of Object, ObjectData)
        Private _hashedObjectsToSerialize As New Hashtable
        'The actual "serialized" data (binary serialized objects and
        '  property values) which is available after Close().  This data drives
        '  the deserialization process.
        Private _serializedState As ArrayList

        ''' <summary>
        ''' Public empty constructor
        ''' </summary>
        ''' <remarks>
        ''' 'cause this is a serializable, there is also a private Sub new - if we want to be able to 
        ''' create instances of this class without using serialization, we better define this constructor!
        '''</remarks>
        Public Sub New()
        End Sub

        ' default impl of abstract base member.  see serialization store for details.
        '	
        Public Overrides ReadOnly Property Errors As ICollection
            Get
                Return Array.Empty(Of Object)
            End Get
        End Property

        ''' <summary>
        ''' The Close method closes this store and prevents any further objects 
        '''   from being serialized into it.  Once closed, the serialization store 
        '''   may be saved (or deserialized).
        ''' </summary>
        Public Overrides Sub Close()
            If _serializedState Is Nothing Then
                Dim SerializedState As New ArrayList(_hashedObjectsToSerialize.Count)
                'Go through each object that we wanted to save anything from...
                For Each Data As ObjectData In _hashedObjectsToSerialize.Values
                    If Data.IsEntireObject Then
                        'We're saving the entire object.
                        '  The constructor for SerializedObjectData will do the
                        '  actual binary serialization for us.
                        SerializedState.Add(New SerializedObjectData(Data))
                    Else
                        'We're saving individual property values.  Go through each...
                        For Each Prop As PropertyDescriptor In Data.Members
                            '... and serialize it.
                            '  The constructor for SerializedObjectData will do the
                            '  actual binary serialization for us.
                            SerializedState.Add(New SerializedObjectData(Data, Prop))
                        Next
                    End If
                Next

                'Save what we've serialized, and clear out the old data - it's no longer
                '  needed.
                _serializedState = SerializedState
                _hashedObjectsToSerialize = Nothing
            End If
        End Sub

#Region "ISerialization implementation"

        'Serialization keys for ISerializable
        Private Const KEY_STATE As String = "State"

        ''' <summary>
        '''     Implements the save part of ISerializable.
        '''   Only needed if you're using the store for copy/paste implementation.
        ''' </summary>
        ''' <param name="info">Serialization info</param>
        ''' <param name="context">Serialization context</param>
        <Security.Permissions.SecurityPermission(Security.Permissions.SecurityAction.Demand, SerializationFormatter:=True)>
        Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
            info.AddValue(KEY_STATE, _serializedState)
        End Sub

        ''' <summary>
        ''' Constructor used to deserialize ourselves from binary serialization.
        '''   Only needed if you're using the store for copy/paste implementation.
        ''' </summary>
        ''' <param name="Info">Serialization info</param>
        ''' <param name="Context">Serialization context</param>
        Private Sub New(Info As SerializationInfo, Context As StreamingContext)
            _serializedState = DirectCast(Info.GetValue(KEY_STATE, GetType(ArrayList)), ArrayList)
        End Sub

#End Region

#Region "Load/Save the store from/to a stream"

        ''' <summary>
        ''' Loads our state from a stream.
        ''' </summary>
        ''' <param name="Stream">The stream to load from</param>
        Public Shared Function Load(Stream As Stream) As GenericComponentSerializationStore
            Return DirectCast(ObjectSerializer.Deserialize(Stream), GenericComponentSerializationStore)
        End Function

        ''' <summary>
        '''     The Save method saves the store to the given stream.  If the store 
        '''     is open, Save will automatically close it for you.  You 
        '''     can call save as many times as you wish to save the store 
        '''     to different streams.
        ''' </summary>
        ''' <param name="stream">The stream to save to</param>
        Public Overrides Sub Save(Stream As Stream)
            Close()
            ObjectSerializer.Serialize(Stream, Me)
        End Sub

#End Region

#Region "Add objects and properties to be serialized at Close"

        ''' <summary>
        ''' Adds a new object serialization to our list of things to serialize.
        ''' </summary>
        ''' <param name="Component">The component to be serialized as an entire object.</param>
        ''' <remarks>
        ''' This is used by UndoEngine when an object is added or removed.
        ''' Again, the object isn't actually serialized until Close(), it's just put in
        '''   our list of things to be serialized.
        ''' </remarks>
        Public Sub AddObject(Component As Object)
            If _serializedState IsNot Nothing Then
                Debug.Fail("State already serialization, shouldn't be adding new stuff")
                Throw New Package.InternalException
            End If

            'Get the current object (or create a new one) that stores all info to
            '  be saved from this instance.
            With GetSerializationData(Component)
                '... and tell it to store the entire object
                .IsEntireObject = True
            End With
        End Sub

        ''' <summary>
        ''' Adds a new property serialization to our list of things to serialize.
        ''' </summary>
        ''' <param name="Component">The object whose property needs to be serialized.</param>
        ''' <param name="Member">The property descriptor which should be serialized.</param>
        ''' <remarks>
        ''' This is used by UndoEngine when a objects property is changed.
        ''' Again, the property isn't actually serialized until Close(), it's just put in
        '''   our list of things to be serialized.
        ''' </remarks>
        Friend Sub AddMember(Component As Object, Member As PropertyDescriptor)
            If _serializedState IsNot Nothing Then
                Debug.Fail("State already serialization, shouldn't be adding new stuff")
                Throw New Package.InternalException
            End If

            'Get the current object (or create a new one) that stores all info to
            '  be saved from this instance.
            With GetSerializationData(Component)
                '... and add this property to the list of properties that we want serialized from this object
                .Members.Add(Member)
            End With
        End Sub

        ''' <summary>
        ''' Gets the current data for the given object that is contained in
        '''   m_HashedObjectsToSerialize.  Or, if there isn't one already, creates a
        '''   new one.
        ''' </summary>
        ''' <param name="Component">The component from which we want to serialize something.</param>
        ''' <returns>The DataToSerialize object associated with this component.</returns>
        Private Function GetSerializationData(Component As Object) As ObjectData
            Dim Data As ObjectData
            If _hashedObjectsToSerialize.ContainsKey(Component) Then
                Data = CType(_hashedObjectsToSerialize(Component), ObjectData)
            Else
                'No object created for this object yet.  Create one now.
                Data = New ObjectData(Component)
                _hashedObjectsToSerialize(Component) = Data
            End If

            Return Data
        End Function

#End Region

#Region "Deserialization of the saved objects/properties (used at Undo/Redo time)"

        ''' <summary>
        ''' Deserializes the saved bits.
        '''     This method deserializes the store, but rather than produce 
        '''     new objects object, the data in the store is applied to an existing 
        '''     set of objects that are taken from the provided container.  This 
        '''     allows the caller to pre-create an object however it sees fit.  If
        '''     an object has deserialization state and the object is not named in 
        '''     the set of existing objects, a new object will be created.  If that 
        '''     object also implements IComponent, it will be added to the given 
        '''     container.  Objects in the container must have names and types that 
        '''     match objects in the serialization store in order for an existing 
        '''     object to be used.
        ''' </summary>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        Friend Sub DeserializeTo(Container As IContainer)
            DeserializeHelper(Container, True)
        End Sub

        ''' <summary>
        ''' Deserializes the saved bits.
        '''     This method deserializes the store to produce a collection of 
        '''     objects contained within it.  If a container is provided, objects 
        '''     that are created that implement IComponent will be added to the container. 
        ''' </summary>
        ''' <returns>The set of components that were deserialized.</returns>
        Friend Function Deserialize() As ICollection
            Return DeserializeHelper(Nothing, False)
        End Function

        ''' <summary>
        ''' Deserializes the saved bits.
        '''     This method deserializes the store to produce a collection of 
        '''     objects contained within it.  If a container is provided, objects 
        '''     that are created that implement IComponent will be added to the container. 
        ''' </summary>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        ''' <returns>The list of objects that were deserialized.</returns>
        Friend Function Deserialize(Container As IContainer) As ICollection
            Return DeserializeHelper(Container, False)
        End Function

        ''' <summary>
        ''' This method does the actual deserialization work, based on the given
        '''   arguments.
        ''' </summary>
        ''' <param name="Container">The container to add deserialized objects to (or Nothing if none)</param>
        ''' <param name="RecycleInstances">If True, we are applying property changes to existing
        '''   instances of components (this is always the case for Undo/Redo).</param>
        ''' <returns>The objects which have been serialized.</returns>
        Private Function DeserializeHelper(Container As IContainer, RecycleInstances As Boolean) As ICollection
            Dim NewObjects As New ArrayList(_serializedState.Count)

            'Handle each individual component or property at a time...
            For Each SerializedObject As SerializedObjectData In _serializedState
                If SerializedObject.IsEntireObject Then
                    '... we have an entire object.  Go ahead and create it from
                    '  the stored binary serialization.
                    '
                    'For entire objects, we ignore the value of RecycleInstances (the Undo engine
                    '  calls us with RecycleInstances=True for the delete/redo case - I would have expected
                    '  False, but either way, we need to create a new instance, so we'll just ignore that 
                    '  flag).

                    Dim NewComponent As Object = SerializedObject.DeserializeObject()

                    '... and add it to the store and list.
                    If Container IsNot Nothing AndAlso TypeOf NewComponent Is IComponent Then
                        Container.Add(DirectCast(NewComponent, IComponent), SerializedObject.ObjectName)
                    End If
                    NewObjects.Add(NewComponent)
                Else
                    'We have just a property to deserialize
                    Dim ComponentToSerializeTo As IComponent = Nothing
                    If RecycleInstances AndAlso Container IsNot Nothing Then
                        'We're applying this property to an existing object.  Need to
                        '  find it in the container's list of components.
                        ComponentToSerializeTo = Container.Components(SerializedObject.ObjectName)
                        If ComponentToSerializeTo Is Nothing Then
                            'Whoops, didn't find it.
                            ' CONSIDER: should we expose a "CreateComponent" method that you can override
                            ' in order to create specific objects?
                            Debug.Fail("Couldn't find component in the container - hard to recycle an unknown component!")
                        End If
                    End If

                    If ComponentToSerializeTo Is Nothing Then
                        Debug.Fail("We didn't find the component to serialize to, and we haven't provided a mechanism to create a new component - this will be a NOOP!")
                    Else
                        'Deserialize the property value and apply it to the object
                        Dim pd As PropertyDescriptor = TypeDescriptor.GetProperties(ComponentToSerializeTo).Item(SerializedObject.PropertyName)
                        If pd Is Nothing Then
                            Debug.Fail("Failed to find named property descriptor on object!")
                        Else
                            pd.SetValue(ComponentToSerializeTo, SerializedObject.DeserializeObject())
                        End If

                        '... and add the component to our list
                        If Not NewObjects.Contains(ComponentToSerializeTo) Then
                            NewObjects.Add(ComponentToSerializeTo)
                        End If
                    End If
                End If
            Next

            'Return all Resources that were affected by the deserialization.
            Return NewObjects
        End Function

#End Region

#Region "Private class - ObjectData"

        ''' <summary>
        ''' Keeps track of everything that we want to serialized about a single
        '''   object instance. (either the entire object itself, or a set of
        '''   its properties)
        ''' </summary>
        <Serializable>
        Protected Class ObjectData

            'Backing for public properties
            Private _isEntireObject As Boolean
            Private _members As ArrayList
            Private ReadOnly _value As Object
            Private ReadOnly _objectName As String

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <param name="Value">The component from which we want to serialize stuff.</param>
            Public Sub New(Value As Object)
                Requires.NotNull(Value, NameOf(Value))

                ' If it is an IComponent, we'll try to get its name from 
                ' its site
                Dim comp = TryCast(Value, IComponent)
                If comp IsNot Nothing AndAlso comp.Site IsNot Nothing Then
                    _objectName = comp.Site.Name
                End If

                If _objectName = "" Then
                    ' We better create a unique name for this...
                    _objectName = Guid.NewGuid.ToString().Replace("-", "_")
                End If

                ' Store the value for later
                _value = Value
            End Sub

            ''' <summary>
            ''' Get tha name of this object
            ''' </summary>
            Public ReadOnly Property Name As String
                Get
                    Return _objectName
                End Get
            End Property

            ''' <summary>
            ''' The object from which we want to serialize stuff.
            ''' </summary>
            Public ReadOnly Property Value As Object
                Get
                    Return _value
                End Get
            End Property

            ''' <summary>
            ''' If True, the entire Resource instance should be serialized.  If false,
            '''   then only the properties in PropertiesToSerialize should be serialized.
            ''' </summary>
            Public Property IsEntireObject As Boolean
                Get
                    Return _isEntireObject
                End Get
                Set
                    If Value AndAlso _members IsNot Nothing Then
                        _members.Clear()
                    End If
                    _isEntireObject = Value
                End Set
            End Property

            ''' <summary>
            ''' A list of PropertyDescriptors representing the properties on
            '''   the Resource which should be serialized.
            ''' </summary>
            Public ReadOnly Property Members As ArrayList
                Get
                    If _members Is Nothing Then
                        _members = New ArrayList
                    End If
                    Return _members
                End Get
            End Property

        End Class 'ObjectData

#End Region

#Region "Private class - SerializedObjectData"

        ''' <summary>
        ''' Keeps track of everything that we want to serialized about a single
        '''   Resource instance (either the entire Resource itself, or a set of
        '''   its properties)
        ''' </summary>
        <Serializable>
        Private Class SerializedObjectData

            'Backing for public properties
            Private ReadOnly _objectName As String
            Private ReadOnly _propertyName As String
            Private ReadOnly _serializedValue As Byte()

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <param name="Value">The component from which we want to serialize stuff.</param>
            Friend Sub New(Value As ObjectData)
                Requires.NotNull(Value, NameOf(Value))

                _objectName = Value.Name
                _serializedValue = SerializeObject(Value.Value)
            End Sub

            ''' <summary>
            ''' Constructor
            ''' </summary>
            ''' <param name="Value">The component from which we want to serialize stuff.</param>
            Public Sub New(Value As ObjectData, [Property] As PropertyDescriptor)
                Requires.NotNull(Value, NameOf(Value))
                Requires.NotNull([Property], NameOf([Property]))

                _objectName = Value.Name
                _propertyName = [Property].Name
                _serializedValue = SerializeObject([Property].GetValue(Value.Value))
            End Sub

            ''' <summary>
            ''' If True, the entire Resource instance should be serialized.  If false,
            '''   then only the properties in PropertiesToSerialize should be serialized.
            ''' </summary>
            Friend ReadOnly Property IsEntireObject As Boolean
                Get
                    Return _propertyName = ""
                End Get
            End Property

            Friend ReadOnly Property ObjectName As String
                Get
                    Return _objectName
                End Get
            End Property

            Friend ReadOnly Property PropertyName As String
                Get
                    Return _propertyName
                End Get
            End Property

            Friend Shared Function SerializeObject([Object] As Object) As Byte()
                If [Object] Is Nothing Then
                    Return Array.Empty(Of Byte)
                Else
                    Dim MemoryStream As New MemoryStream
                    ObjectSerializer.Serialize(MemoryStream, [Object])
                    Return MemoryStream.ToArray()
                End If
            End Function

            Public Function DeserializeObject() As Object
                If _serializedValue.Length = 0 Then
                    Return Nothing
                Else
                    Dim MemoryStream As New MemoryStream(_serializedValue)
                    Return ObjectSerializer.Deserialize(MemoryStream)
                End If
            End Function

        End Class
#End Region

    End Class

End Namespace
