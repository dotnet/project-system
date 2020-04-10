' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.WCFReference.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' This is the class we wrapped a WCF service reference and pushed to the propertyGrid
    ''' </summary>
    Friend Class ServiceReferenceComponent
        Inherits Component
        Implements ICustomTypeDescriptor, IReferenceComponent, IUpdatableReferenceComponent

        Private ReadOnly _collection As IVsWCFReferenceGroupCollection
        Private ReadOnly _referenceGroup As IVsWCFReferenceGroup

        Public Sub New(collection As IVsWCFReferenceGroupCollection, referenceGroup As IVsWCFReferenceGroup)
            _collection = collection
            _referenceGroup = referenceGroup
        End Sub

        <VBDescription(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_ServiceReferenceNamespaceDescription)>
        <MergableProperty(False)>
        <HelpKeyword("ServiceReference Properties.Namespace")>
        Public Property [Namespace] As String
            Get
                Return _referenceGroup.GetNamespace()
            End Get
            Set
                _referenceGroup.SetNamespace(value)
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
#Disable Warning CA1822 ' Mark members as static
        Private Function ShouldSerializeNamespace() As Boolean
#Enable Warning CA1822
            Return False
        End Function

        <VBDisplayName(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_ServiceReferenceUrlName)>
        <VBDescription(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_ServiceReferenceUrlDescription)>
        <HelpKeyword("ServiceReference Properties.ServiceReferenceURL")>
        <MergableProperty(False)>
        Public Property ServiceReferenceURL As String
            Get
                If _referenceGroup.GetReferenceCount() = 1 Then
                    Return _referenceGroup.GetReferenceUrl(0)
                ElseIf _referenceGroup.GetReferenceCount() > 1 Then
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.CSRDlg_MultipleURL
                Else
                    Return ""
                End If
                Return String.Empty
            End Get
            Set
                value = value.Trim()
                Dim currentCount As Integer = _referenceGroup.GetReferenceCount()
                If currentCount = 1 Then
                    If value <> "" Then
                        Dim currentUrl As String = _referenceGroup.GetReferenceUrl(0)
                        _referenceGroup.SetReferenceUrl(0, value)
                        Try
                            _referenceGroup.Update(Nothing)
                        Catch ex As Exception
                            _referenceGroup.SetReferenceUrl(0, currentUrl)
                            Throw ex
                        End Try
                    Else
                        Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ServiceReferenceProperty_SetReferenceUrlEmpty)
                    End If
                ElseIf currentCount > 1 Then
                    Throw New NotSupportedException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ServiceReferenceProperty_MultipleUrlNotSupported)
                Else
                    If value <> "" Then
                        _referenceGroup.AddReference(Nothing, value)
                    End If
                End If
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
#Disable Warning CA1822 ' Mark members as static
        Private Function ShouldSerializeServiceReferenceURL() As Boolean
#Enable Warning CA1822
            Return False
        End Function

        ''' <summary>
        ''' Service reference instance
        ''' </summary>
        Friend ReadOnly Property ReferenceGroup As IVsWCFReferenceGroup
            Get
                Return _referenceGroup
            End Get
        End Property

        ''' <summary>
        ''' Remove the service reference...
        ''' </summary>
        Private Sub Remove() Implements IReferenceComponent.Remove
            _collection.Remove(_referenceGroup)
        End Sub

        Private Function GetName() As String Implements IReferenceComponent.GetName
            Return [Namespace]
        End Function

        '''<summary>
        ''' Update the web reference
        '''</summary>
        Private Sub Update() Implements IUpdatableReferenceComponent.Update
            _referenceGroup.Update(Nothing)
        End Sub

#Region "System.ComponentModel.ICustomTypeDescriptor"
        ' we override the ICustomTypeDescriptor to replace the ClassName and ComponentName which are shown on the propertyGrid
        ' all other functions are implemented in its default way...

        Public Function GetAttributes() As AttributeCollection Implements ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes([GetType]())
        End Function

        Public Function GetClassName() As String Implements ICustomTypeDescriptor.GetClassName
            Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ServiceReferenceTypeName
        End Function

        Public Function GetComponentName() As String Implements ICustomTypeDescriptor.GetComponentName
            Return [Namespace]
        End Function

        Public Function GetConverter() As TypeConverter Implements ICustomTypeDescriptor.GetConverter
            Return TypeDescriptor.GetConverter([GetType]())
        End Function

        Public Function GetDefaultEvent() As EventDescriptor Implements ICustomTypeDescriptor.GetDefaultEvent
            Return TypeDescriptor.GetDefaultEvent([GetType]())
        End Function

        Public Function GetDefaultProperty() As PropertyDescriptor Implements ICustomTypeDescriptor.GetDefaultProperty
            Return TypeDescriptor.GetDefaultProperty([GetType]())
        End Function

        Public Function GetEditor(editorBaseType As Type) As Object Implements ICustomTypeDescriptor.GetEditor
            Return TypeDescriptor.GetEditor([GetType](), editorBaseType)
        End Function

        Public Function GetEvents() As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents([GetType]())
        End Function

        Public Function GetEvents1(attributes() As Attribute) As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return TypeDescriptor.GetEvents([GetType](), attributes)
        End Function

        ''' <summary>
        ''' Returns the Modified properties. 
        '''    - Makes the Metadata Location property readonly.
        ''' </summary>
        ''' <param name="orig">The original property list</param>
        Private Function GetModifiedPropertyList(orig As PropertyDescriptorCollection) As PropertyDescriptorCollection
            Dim modified As PropertyDescriptor() = New PropertyDescriptor(orig.Count - 1) {}

            Dim i As Integer = 0
            For Each prop As PropertyDescriptor In orig
                'Just modify the URL property to readonly if the reference has multiple URLs
                If prop.Name.Equals("ServiceReferenceURL", StringComparison.Ordinal) Then
                    modified(i) = TypeDescriptor.CreateProperty([GetType](), prop, New Attribute() {ReadOnlyAttribute.Yes})
                Else
                    modified(i) = orig(i)
                End If
                i += 1
            Next
            Return New PropertyDescriptorCollection(modified)
        End Function

        Public Function GetProperties() As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Dim orig As PropertyDescriptorCollection = TypeDescriptor.GetProperties([GetType]())

            If _referenceGroup.GetReferenceCount() > 1 Then
                Return GetModifiedPropertyList(orig)
            Else
                Return orig
            End If
        End Function

        Public Function GetProperties1(attributes() As Attribute) As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Dim orig As PropertyDescriptorCollection = TypeDescriptor.GetProperties([GetType](), attributes)

            If _referenceGroup.GetReferenceCount() > 1 Then
                Return GetModifiedPropertyList(orig)
            Else
                Return orig
            End If

        End Function

        Public Function GetPropertyOwner(pd As PropertyDescriptor) As Object Implements ICustomTypeDescriptor.GetPropertyOwner
            Return Me
        End Function
#End Region
    End Class

End Namespace
