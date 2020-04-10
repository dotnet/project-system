' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' This is the class we wrapped a web reference and pushed to the propertyGrid
    ''' </summary>
    Friend Class WebReferenceComponent
        Inherits Component
        Implements ICustomTypeDescriptor, IReferenceComponent, IUpdatableReferenceComponent

        Private ReadOnly _page As ReferencePropPage
        Private ReadOnly _projectItem As EnvDTE.ProjectItem

        Public Sub New(page As ReferencePropPage, projectItem As EnvDTE.ProjectItem)
            _page = page
            _projectItem = projectItem
        End Sub

        <VBDescription(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_WebReferenceNameDescription)>
        <MergableProperty(False)>
        <HelpKeyword("Folder Properties.FileName")>
        Public Property Name As String
            Get
                Try
                    Return _projectItem.Name
                Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(Name), NameOf(WebReferenceComponent))
                    Return String.Empty
                End Try
            End Get
            Set
                _projectItem.Name = value
                _page.OnWebReferencePropertyChanged(Me)
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
#Disable Warning CA1822 ' Mark members as static
        Private Function ShouldSerializeName() As Boolean
#Enable Warning CA1822
            Return False
        End Function

        Friend ReadOnly Property WebReference As EnvDTE.ProjectItem
            Get
                Return _projectItem
            End Get
        End Property

        <VBDisplayName(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_UrlBehaviorName)>
        <VBDescription(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_UrlBehaviorDescription)>
        <HelpKeyword("Folder Properties.UrlBehavior")>
        Public Property UrlBehavior As UrlBehaviorType
            Get
                Dim prop As EnvDTE.[Property] = GetItemProperty(NameOf(UrlBehavior))
                If prop IsNot Nothing Then
                    Return CType(CInt(prop.Value), UrlBehaviorType)
                Else
                    Debug.Fail("Why we can not find UrlBehavior")
                    Return UrlBehaviorType.Static
                End If
            End Get
            Set
                Dim prop As EnvDTE.[Property] = GetItemProperty(NameOf(UrlBehavior))
                If prop IsNot Nothing Then
                    prop.Value = CInt(Value)
                    _page.OnWebReferencePropertyChanged(Me)
                Else
                    Debug.Fail("Why we can not find UrlBehavior")
                End If
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
#Disable Warning CA1822 ' Mark members as static
        Private Function ShouldSerializeUrlBehavior() As Boolean
#Enable Warning CA1822
            Return False
        End Function

        <VBDisplayName(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_WebReferenceUrlName)>
        <VBDescription(My.Resources.Microsoft_VisualStudio_Editors_Designer.ConstantResourceIDs.PPG_WebReferenceUrlDescription)>
        <HelpKeyword("Folder Properties.WebReference")>
        <MergableProperty(False)>
        Public Property WebReferenceURL As String
            Get
                Dim prop As EnvDTE.[Property] = GetItemProperty(NameOf(WebReference))
                If prop IsNot Nothing Then
                    Return CStr(prop.Value)
                Else
                    Debug.Fail("Why we can not find WebReference")
                    Return String.Empty
                End If
            End Get
            Set
                If value Is Nothing Then
                    value = String.Empty
                End If

                Dim prop As EnvDTE.[Property] = GetItemProperty(NameOf(WebReference))
                If prop IsNot Nothing Then
                    prop.Value = value
                    _page.OnWebReferencePropertyChanged(Me)
                Else
                    Debug.Fail("Why we can not find WebReference")
                End If
            End Set
        End Property

        ' Prevent using Bold Font in the property grid (the same style as other reference)
#Disable Warning CA1822 ' Mark members as static
        Private Function ShouldSerializeWebReferenceURL() As Boolean
#Enable Warning CA1822
            Return False
        End Function

        ' Access the property through EnvDTE.ProjectItem.Properties
        Private Function GetItemProperty(propertyName As String) As EnvDTE.[Property]
            Try
                Dim properties As EnvDTE.Properties = _projectItem.Properties
                If properties IsNot Nothing Then
                    Return properties.Item(propertyName)
                End If
            Catch e As ArgumentException When Common.ReportWithoutCrash(e, NameOf(GetItemProperty), NameOf(WebReferenceComponent))
            End Try
            Return Nothing
        End Function

        ' Remove the webReference...
        Private Sub Remove() Implements IReferenceComponent.Remove
            _projectItem.Remove()
        End Sub

        Private Function GetName() As String Implements IReferenceComponent.GetName
            Return Name
        End Function

        '''<summary>
        ''' Update the web reference
        '''</summary>
        Private Sub Update() Implements IUpdatableReferenceComponent.Update
            Dim referenceProperty As EnvDTE.[Property] = GetItemProperty("WebReferenceInterface")
            If referenceProperty IsNot Nothing Then
                Dim reference As VsWebSite.WebReference = TryCast(referenceProperty.Value, VsWebSite.WebReference)
                If reference IsNot Nothing Then
                    reference.Update()
                End If
            End If
        End Sub

#Region "System.ComponentModel.ICustomTypeDescriptor"
        ' we override the ICustomTypeDescriptor to replace the ClassName and ComponentName which are shown on the propertyGrid
        ' all other functions are implemented in its default way...

        Public Function GetAttributes() As AttributeCollection Implements ICustomTypeDescriptor.GetAttributes
            Return TypeDescriptor.GetAttributes([GetType]())
        End Function

        Public Function GetClassName() As String Implements ICustomTypeDescriptor.GetClassName
            Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WebReferenceTypeName
        End Function

        Public Function GetComponentName() As String Implements ICustomTypeDescriptor.GetComponentName
            Return Name
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

        Public Function GetProperties() As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties([GetType]())
        End Function

        Public Function GetProperties1(attributes() As Attribute) As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return TypeDescriptor.GetProperties([GetType](), attributes)
        End Function

        Public Function GetPropertyOwner(pd As PropertyDescriptor) As Object Implements ICustomTypeDescriptor.GetPropertyOwner
            Return Me
        End Function
#End Region
    End Class

#Region "UrlBehaviorType"
    <TypeConverter(GetType(UrlBehaviorTypeConverter))>
    Friend Enum UrlBehaviorType
        [Static]
        Dynamic
    End Enum

    ''' <summary>
    '''  a TypeConvert to localize the UrlBehavior property...
    ''' </summary>
    Friend Class UrlBehaviorTypeConverter
        Inherits TypeConverter

        Private Shared s_displayValues As String()

        ' a help collection to hold localized strings
        Private Shared ReadOnly Property DisplayValues As String()
            Get
                If s_displayValues Is Nothing Then
                    s_displayValues = New String() {My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_UrlBehavior_Static, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_UrlBehavior_Dynamic}
                End If
                Return s_displayValues
            End Get
        End Property

        ' we only implement converting from string...
        Public Overrides Function CanConvertFrom(context As ITypeDescriptorContext, sourceType As Type) As Boolean
            If sourceType Is GetType(String) Then
                Return True
            End If
            Return MyBase.CanConvertFrom(context, sourceType)
        End Function

        ' we only implement converting to string...
        Public Overrides Function CanConvertTo(context As ITypeDescriptorContext, destinationType As Type) As Boolean
            If destinationType Is GetType(String) Then
                Return True
            End If
            Return MyBase.CanConvertTo(context, destinationType)
        End Function

        ' we only implement converting from string...
        Public Overrides Function ConvertFrom(context As ITypeDescriptorContext, culture As Globalization.CultureInfo, value As Object) As Object
            If TypeOf value Is String Then
                Dim stringValue As String = CStr(value)
                For i As Integer = 0 To DisplayValues.Length - 1
                    If DisplayValues(i).Equals(stringValue) Then
                        Return CType(i, UrlBehaviorType)
                    End If
                Next
            End If
            Return MyBase.ConvertFrom(context, culture, value)
        End Function

        ' we only implement converting to string...
        Public Overrides Function ConvertTo(context As ITypeDescriptorContext, culture As Globalization.CultureInfo, value As Object, destinationType As Type) As Object
            If destinationType Is GetType(String) Then
                Dim type As UrlBehaviorType = CType(value, UrlBehaviorType)
                Return DisplayValues(CInt(type))
            End If
            Return MyBase.ConvertTo(context, culture, value, destinationType)
        End Function

        ' standard value collection... will be used in the dropdown of the propertyGrid
        Public Overrides Function GetStandardValues(context As ITypeDescriptorContext) As StandardValuesCollection
            Return New StandardValuesCollection(New UrlBehaviorType() {UrlBehaviorType.Static, UrlBehaviorType.Dynamic})
        End Function

        Public Overrides Function GetStandardValuesSupported(context As ITypeDescriptorContext) As Boolean
            Return True
        End Function
    End Class
#End Region

End Namespace

