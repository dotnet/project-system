' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Imports Microsoft.VisualStudio.Editors.PropertyPages

Imports IVsHierarchy = Microsoft.VisualStudio.Shell.Interop.IVsHierarchy

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' A DesignTimeSettingInstance wraps a single setting. Each setting will generate 
    ''' a property with the appropriate attributes in the generated file.
    ''' </summary>
    <Serializable>
    Friend Class DesignTimeSettingInstance
        Inherits Component
        Implements ICustomTypeDescriptor, System.Runtime.Serialization.ISerializable

        ''' <summary>
        ''' Application or user scoped setting?
        ''' </summary>
        <TypeConverter(GetType(ScopeConverter))>
        Friend Enum SettingScope
            ' Don't use zero in the enum since that hides CType(NULL, SettingScope) 
            ' issues...
            User = 1
            Application = 2
        End Enum

        ''' <summary>
        ''' The name of this setting instance
        ''' </summary>
        Private _name As String

        ''' <summary>
        ''' The type name (as persisted in the .settings file) of this setting
        ''' </summary>
        Private _settingTypeName As String = GetType(String).FullName

        ''' <summary>
        ''' The setting scope (application or user) for this setting
        ''' </summary>
        Private _settingScope As SettingScope = SettingScope.User

        ''' <summary>
        ''' Is this setting a roaming setting?
        ''' </summary>
        Private _roaming As Boolean

        ''' <summary>
        ''' The serialized representation of this setting
        ''' </summary>
        Private _serializedValue As String = ""

        ''' <summary>
        ''' The setting provider if any
        ''' </summary>
        Private _provider As String

        ''' <summary>
        ''' The description for this setting
        ''' </summary>
        Private _description As String

        ''' <summary>
        ''' Flag indicating if we want to add the serialized value as a
        ''' DefaultSettingValue attribute on the setting. If the setting
        ''' contains sensitive information, the user can set this to false
        ''' through the property grid...
        ''' </summary>
        Private _generateDefaultValueInCode As Boolean = True

#Region "Cached property descriptors with this instance as the owner"
        Private ReadOnly _generateDefaultValueInCodePropertyDescriptor As New GenerateDefaultValueInCodePropertyDescriptor(Me)
        Private ReadOnly _namePropertyDescriptor As New NamePropertyDescriptor(Me)
        Private ReadOnly _descriptionPropertyDescriptor As New DescriptionPropertyDescriptor(Me)
        Private ReadOnly _providerPropertyDescriptor As New ProviderPropertyDescriptor(Me)
        Private ReadOnly _roamingPropertyDescriptor As New RoamingPropertyDescriptor(Me)
        Private ReadOnly _scopePropertyDescriptor As New ScopePropertyDescriptor(Me)
        Private ReadOnly _serializedValuePropertyDescriptor As New SerializedValuePropertyDescriptor(Me)
        Private ReadOnly _settingTypeNamePropertyDescriptor As New SettingTypeNamePropertyDescriptor(Me)
#End Region

        Public Sub New()

        End Sub

#Region "ICustomTypeDescriptor implementation. For a detailed description, see the MSDN docs"
        Private Function GetAttributes() As AttributeCollection Implements ICustomTypeDescriptor.GetAttributes
            Return New AttributeCollection(New Design.HelpKeywordAttribute("ApplicationSetting"))
        End Function

        Private Function GetClassName() As String Implements ICustomTypeDescriptor.GetClassName
            Return [GetType]().FullName
        End Function

        Private Function GetComponentName() As String Implements ICustomTypeDescriptor.GetComponentName
            Return Name
        End Function

        Private Function GetConverter() As TypeConverter Implements ICustomTypeDescriptor.GetConverter
            Return Nothing
        End Function

        Private Function GetDefaultEvent() As EventDescriptor Implements ICustomTypeDescriptor.GetDefaultEvent
            Return Nothing
        End Function

        Private Function GetDefaultProperty() As PropertyDescriptor Implements ICustomTypeDescriptor.GetDefaultProperty
            Return _namePropertyDescriptor
        End Function

        Private Function GetEditor(editorBaseType As Type) As Object Implements ICustomTypeDescriptor.GetEditor
            Return Nothing
        End Function

        Private Function GetEvents() As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return New EventDescriptorCollection(Array.Empty(Of EventDescriptor))
        End Function

        Private Function GetEvents(attributes() As Attribute) As EventDescriptorCollection Implements ICustomTypeDescriptor.GetEvents
            Return GetEvents()
        End Function

        Private Function GetProperties() As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return GetProperties(Nothing)
        End Function

        Private Function GetProperties(attributes() As Attribute) As PropertyDescriptorCollection Implements ICustomTypeDescriptor.GetProperties
            Return New PropertyDescriptorCollection(New PropertyDescriptor() {
                                                            _namePropertyDescriptor,
                                                            _roamingPropertyDescriptor,
                                                            _descriptionPropertyDescriptor,
                                                            _providerPropertyDescriptor,
                                                            _scopePropertyDescriptor,
                                                            _generateDefaultValueInCodePropertyDescriptor,
                                                            _settingTypeNamePropertyDescriptor,
                                                            _serializedValuePropertyDescriptor})
        End Function

        Private Function GetPropertyOwner(pd As PropertyDescriptor) As Object Implements ICustomTypeDescriptor.GetPropertyOwner
            If pd Is Nothing Then
                ' No property descriptor => should return the current instance...
                Return Me
            End If

            Dim dsicpd As DesignTimeSettingInstanceCustomPropertyDescriptorBase = TryCast(pd, DesignTimeSettingInstanceCustomPropertyDescriptorBase)
            If dsicpd IsNot Nothing Then
                Return dsicpd.Owner
            Else
                Debug.Fail(String.Format("Why did someone ask me what the owner of a property descriptor of type {0} is?", pd.GetType().FullName))
                Return Nothing
            End If
        End Function
#End Region

#Region "Custom property descriptors"
#Region "Abstract property descriptor base classes"
        ''' <summary>
        ''' Base implementation for our custom property descriptors. Adding some default
        ''' behavior and overrides to make it more explicit what we expect from each
        ''' propertydescriptor.
        ''' </summary>
        Private MustInherit Class DesignTimeSettingInstanceCustomPropertyDescriptorBase
            Inherits PropertyDescriptor

            Private ReadOnly _owner As DesignTimeSettingInstance

            Public Sub New(owner As DesignTimeSettingInstance, name As String)
                MyBase.New(name, Array.Empty(Of Attribute))
                _owner = owner
            End Sub

            Public ReadOnly Property Owner As DesignTimeSettingInstance
                Get
                    Return _owner
                End Get
            End Property

            Public Overrides ReadOnly Property ComponentType As Type
                Get
                    Return GetType(DesignTimeSettingInstance)
                End Get
            End Property

            Public Overrides Function CanResetValue(component As Object) As Boolean
                Return False
            End Function

            Public Overrides Sub ResetValue(component As Object)
                Throw New NotSupportedException()
            End Sub

            Public Overrides Function ShouldSerializeValue(component As Object) As Boolean
                Return True
            End Function

            Public Overrides Function GetValue(component As Object) As Object
                Return GetValue(DirectCast(component, DesignTimeSettingInstance))
            End Function

            Protected Overrides Sub FillAttributes(attributeList As IList)
                MyBase.FillAttributes(attributeList)
                If DescriptionAttributeText <> "" Then
                    attributeList.Add(New DescriptionAttribute(DescriptionAttributeText))
                End If
            End Sub

            ''' <summary>
            ''' Override to set the description (shown in the properties window) for this
            ''' property
            ''' </summary>
            Protected MustOverride ReadOnly Property DescriptionAttributeText As String

            ''' <summary>
            ''' Override to get the value for the current component in a type-safe way
            ''' </summary>
            ''' <param name="component"></param>
            Protected MustOverride Overloads Function GetValue(component As DesignTimeSettingInstance) As Object
        End Class

        ''' <summary>
        ''' Add Undo support (component change notifications and designer transactions w. descriptions)
        ''' whenever we change the value
        ''' </summary>
        Private MustInherit Class DesignTimeSettingInstanceCustomPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptorBase

            Public Sub New(owner As DesignTimeSettingInstance, name As String)
                MyBase.New(owner, name)
            End Sub

            ''' <summary>
            ''' Wrap the call to the derived SetValue in a designer transaction, providing the 
            ''' description to the transaction from the actual property descriptor
            ''' </summary>
            ''' <param name="component"></param>
            ''' <param name="value"></param>
            Public NotOverridable Overrides Sub SetValue(component As Object, value As Object)
                Dim instance As DesignTimeSettingInstance = DirectCast(component, DesignTimeSettingInstance)
                Dim ccsvc As Design.IComponentChangeService = Nothing
                Dim host As Design.IDesignerHost = Nothing
                If instance IsNot Nothing AndAlso instance.Site IsNot Nothing Then
                    ccsvc = DirectCast(instance.Site.GetService(GetType(Design.IComponentChangeService)),
                                        Design.IComponentChangeService)
                    host = DirectCast(instance.Site.GetService(GetType(Design.IDesignerHost)),
                                        Design.IDesignerHost)
                End If

                Dim undoTran As Design.DesignerTransaction = Nothing
                Try
                    Dim oldValue As Object = GetValue(component)

                    If Equals(oldValue, value) Then
                        ' We don't want to create an undounit if the values are equal...
                        Return
                    End If

                    ' Create transaction/fire component changing
                    If host IsNot Nothing Then
                        undoTran = host.CreateTransaction(UndoDescription)
                    End If
                    If ccsvc IsNot Nothing Then
                        ccsvc.OnComponentChanging(component, Me)
                    End If

                    SetValue(instance, value)

                    ' Fire component changed/close transaction
                    If ccsvc IsNot Nothing Then
                        ccsvc.OnComponentChanged(component, Me, oldValue, value)
                    End If
                    If undoTran IsNot Nothing Then
                        undoTran.Commit()
                    End If
                Finally
                    If undoTran IsNot Nothing Then
                        undoTran.Cancel()
                    End If
                End Try
            End Sub

            ''' <summary>
            ''' Override to provide the description showing up in the undo menu
            ''' </summary>
            Protected MustOverride ReadOnly Property UndoDescription As String

            ''' <summary>
            ''' Override for the actual set of the value
            ''' </summary>
            ''' <param name="component"></param>
            ''' <param name="value"></param>
            Protected MustOverride Overloads Sub SetValue(component As DesignTimeSettingInstance, value As Object)

        End Class
#End Region

#Region "Exposed property descriptors"
        Private Class GenerateDefaultValueInCodePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "GenerateDefaultValueInCode")
            End Sub

            Protected Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.GenerateDefaultValueInCode
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(Boolean)
                End Get
            End Property

            Protected Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetGenerateDefaultValueInCode(CBool(value))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_GenerateDefaultValueInCode
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_GenerateDefaultValueInCode
                End Get
            End Property
        End Class

        Private Class NamePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Name")
            End Sub

            Public Overrides Function CanResetValue(component As Object) As Boolean
                Return False
            End Function

            Protected Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.Name
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return IsNameReadOnly(Owner)
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetName(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_NameChanged
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_Name
                End Get
            End Property
        End Class

        Private Class RoamingPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Roaming")
            End Sub

            Protected Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.Roaming
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return IsRoamingReadOnly(Owner)
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(Boolean)
                End Get
            End Property

            Protected Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetRoaming(CBool(value))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_RoamingChanged
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_Roaming
                End Get
            End Property
        End Class

        Private Class DescriptionPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Description")
            End Sub

            Protected Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.Description
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetDescription(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_DescriptionChanged
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_Description
                End Get
            End Property
        End Class

        Private Class ProviderPropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Provider")
            End Sub

            Protected Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.Provider
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetProvider(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_ProviderChanged
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_Provider
                End Get
            End Property
        End Class

        Private Class ScopePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "Scope")
            End Sub

            Protected Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.Scope
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    ' Connection string typed settings are application scoped only...
                    Return IsScopeReadOnly(Owner, ProjectSupportsUserScopedSettings(Owner))
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(SettingScope)
                End Get
            End Property

            Protected Overrides Sub FillAttributes(attributeList As IList)
                MyBase.FillAttributes(attributeList)
                attributeList.Add(New TypeConverterAttribute(GetType(ScopeConverter)))
            End Sub
            Protected Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetScope(CType(value, SettingScope))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_ScopeChanged
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_Scope
                End Get
            End Property

            Public Overrides ReadOnly Property Converter As TypeConverter
                Get
                    Return New ScopeConverter
                End Get
            End Property

        End Class

        Private Class SettingTypeNamePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "SettingTypeName")
            End Sub

            Protected Overloads Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.SettingTypeName
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    ' The type name is always read-only in the property grid...
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overloads Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetSettingTypeName(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_TypeChanged
                End Get
            End Property

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_SerializedSettingType
                End Get
            End Property
        End Class

        Private Class SerializedValuePropertyDescriptor
            Inherits DesignTimeSettingInstanceCustomPropertyDescriptor

            Public Sub New(owner As DesignTimeSettingInstance)
                MyBase.New(owner, "SerializedValue")
            End Sub

            Protected Overloads Overrides Function GetValue(component As DesignTimeSettingInstance) As Object
                Return component.SerializedValue
            End Function

            Public Overrides ReadOnly Property IsReadOnly As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property PropertyType As Type
                Get
                    Return GetType(String)
                End Get
            End Property

            Protected Overloads Overrides Sub SetValue(component As DesignTimeSettingInstance, value As Object)
                component.SetSerializedValue(DirectCast(value, String))
            End Sub

            Protected Overrides ReadOnly Property UndoDescription As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_UndoTran_SerializedValueChanged
                End Get
            End Property

            Protected Overrides Sub FillAttributes(attributeList As IList)
                MyBase.FillAttributes(attributeList)
                attributeList.Add(New BrowsableAttribute(False))
            End Sub

            Protected Overrides ReadOnly Property DescriptionAttributeText As String
                Get
                    Return My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DESCR_Value
                End Get
            End Property
        End Class

#End Region

#Region "Type converters used not only by the property descriptors"

        ''' <summary>
        ''' Translate to/from localized string representation of User and Application
        ''' </summary>
        Friend Class ScopeConverter
            Inherits EnumConverter

            Public Sub New()
                MyBase.New(GetType(SettingScope))
            End Sub

            Public Overrides Function CanConvertFrom(context As ITypeDescriptorContext, type As Type) As Boolean
                If GetType(String).Equals(type) Then
                    Return True
                Else
                    Return MyBase.CanConvertFrom(context, type)
                End If
            End Function

            Public Overrides Function CanConvertTo(context As ITypeDescriptorContext, type As Type) As Boolean
                If GetType(String).Equals(type) Then
                    Return True
                Else
                    Return MyBase.CanConvertTo(context, type)
                End If
            End Function

            Public Overrides Function ConvertFrom(context As ITypeDescriptorContext, culture As Globalization.CultureInfo, value As Object) As Object
                Dim stringValue = TryCast(value, String)
                If stringValue IsNot Nothing Then
                    If String.Equals(stringValue, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_ApplicationScope, StringComparison.Ordinal) Then
                        Return SettingScope.Application
                    End If
                    If String.Equals(stringValue, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_UserScope, StringComparison.Ordinal) Then
                        Return SettingScope.User
                    End If
                End If
                Return MyBase.ConvertFrom(context, culture, value)
            End Function

            Public Overrides Function ConvertTo(context As ITypeDescriptorContext, culture As Globalization.CultureInfo, value As Object, destinationType As Type) As Object
                If GetType(String).Equals(destinationType) Then
                    Dim instance As DesignTimeSettingInstance = Nothing
                    If context IsNot Nothing Then
                        instance = TryCast(context.Instance, DesignTimeSettingInstance)
                    End If
                    Return ConvertToLocalizedString(instance, CType(value, SettingScope))
                End If
                Return MyBase.ConvertTo(context, culture, value, destinationType)
            End Function

            ''' <summary>
            ''' Shared helper method to convert a scope value to a string suitable for
            ''' display in the UI. Since the string value may depend on the instance which
            ''' it is associated (if the provider is the web settings provider, it is different
            ''' than for other providers) we also pass in an DesignTimeSettingInstance
            ''' </summary>
            ''' <remarks>Returns the localized version for the scope</remarks>
            Public Shared Function ConvertToLocalizedString(instance As DesignTimeSettingInstance, scope As SettingScope) As String
                Select Case scope
                    Case SettingScope.Application
                        If IsWebProvider(instance) Then
                            Static WebApplicationScopeString As String = Nothing
                            If WebApplicationScopeString Is Nothing Then
                                WebApplicationScopeString = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_WebApplicationScope
                            End If
                            Return WebApplicationScopeString
                        Else
                            Static ApplicationScopeString As String = Nothing
                            If ApplicationScopeString Is Nothing Then
                                ApplicationScopeString = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_ApplicationScope
                            End If
                            Return ApplicationScopeString
                        End If
                    Case SettingScope.User
                        If IsWebProvider(instance) Then
                            Static WebUserScopeString As String = Nothing
                            If WebUserScopeString Is Nothing Then
                                WebUserScopeString = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_WebUserScope
                            End If
                            Return WebUserScopeString
                        Else
                            Static UserScopeString As String = Nothing
                            If UserScopeString Is Nothing Then
                                UserScopeString = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_UserScope
                            End If
                            Return UserScopeString
                        End If
                    Case Else
                        Debug.Fail("Unknown scope")
                        Return scope.ToString()
                End Select
            End Function
        End Class
#End Region

#End Region

#Region "DesignTimeSettingInstance persisted fields/properties"

#Region "Name"
        ''' <summary>
        ''' The name of a setting
        ''' </summary>
        Public ReadOnly Property NameProperty As PropertyDescriptor
            Get
                Return _namePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Sub SetName(value As String)
            If value = "" Then
                Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_NameEmpty)
            End If
            If Not DesignTimeSettings.EqualIdentifiers(Name, value) AndAlso Site IsNot Nothing Then

                For Each probeComponent As IComponent In Site.Container.Components
                    Dim instance As DesignTimeSettingInstance = TryCast(probeComponent, DesignTimeSettingInstance)
                    If instance IsNot Nothing AndAlso DesignTimeSettings.EqualIdentifiers(instance.Name, value) Then
                        Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_DuplicateName_1Arg, value))
                    End If
                Next

                Dim nameCreationService As Design.Serialization.INameCreationService = TryCast(
                    Site.GetService(GetType(Design.Serialization.INameCreationService)),
                    Design.Serialization.INameCreationService)

                If nameCreationService IsNot Nothing AndAlso Not nameCreationService.IsValidName(value) Then
                    Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_InvalidIdentifier_1Arg, value))
                End If
            End If
            _name = value
        End Sub
#End Region

#Region "Scope"

        ''' <summary>
        ''' The scope for a setting
        ''' </summary>
        Public ReadOnly Property ScopeProperty As PropertyDescriptor
            Get
                Return _scopePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property Scope As SettingScope
            Get
                Return _settingScope
            End Get
        End Property

        Public Sub SetScope(value As SettingScope)
            _settingScope = value
        End Sub
#End Region

#Region "TypeName"

        ''' <summary>
        ''' The name of the type for a setting
        ''' </summary>
        Public ReadOnly Property TypeNameProperty As PropertyDescriptor
            Get
                Return _settingTypeNamePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property SettingTypeName As String
            Get
                Return _settingTypeName
            End Get
        End Property

        Public Sub SetSettingTypeName(value As String)
            _settingTypeName = value
        End Sub
#End Region

#Region "SerializedValue"

        ''' <summary>
        ''' The serialized (string) representation of the value of this setting
        ''' </summary>
        Public ReadOnly Property SerializedValueProperty As PropertyDescriptor
            Get
                Return _serializedValuePropertyDescriptor
            End Get
        End Property

        Public ReadOnly Property SerializedValue As String
            Get
                Return _serializedValue
            End Get
        End Property

        Public Sub SetSerializedValue(value As String)
            _serializedValue = value
        End Sub
#End Region

#Region "Roaming"

        ''' <summary>
        ''' Flag indicating if this is a roaming setting 
        ''' (value is stored in the roaming user.config by the runtime)
        ''' </summary>
        Public ReadOnly Property Roaming As Boolean
            Get
                Return _roaming
            End Get
        End Property

        Public Sub SetRoaming(value As Boolean)
            _roaming = value
        End Sub
#End Region

#Region "Description"

        ''' <summary>
        ''' The description for this setting
        ''' </summary>
        Public ReadOnly Property Description As String
            Get
                Return _description
            End Get
        End Property

        Public Sub SetDescription(value As String)
            _description = value
        End Sub
#End Region

#Region "Provider"

        ''' <summary>
        ''' The settings provider for this setting
        ''' </summary>
        Public ReadOnly Property Provider As String
            Get
                Return _provider
            End Get
        End Property

        Public Sub SetProvider(value As String)
            _provider = value

            ' Setting the provider may actually change the scope display name...
            TypeDescriptor.Refresh(Me)
        End Sub
#End Region

#Region "Generate default value in code"

        ''' <summary>
        ''' Flag indicating if we should generate a defaultsettingsvalue attribute
        ''' on this setting
        ''' </summary>
        Public ReadOnly Property GenerateDefaultValueInCode As Boolean
            Get
                Return _generateDefaultValueInCode
            End Get
        End Property

        Public Sub SetGenerateDefaultValueInCode(value As Boolean)
            _generateDefaultValueInCode = value
        End Sub
#End Region

#Region "Helper methods to determine the state of the current instance"

        ''' <summary>
        ''' Is the provided setting instance of type connection string?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsConnectionString(instance As DesignTimeSettingInstance) As Boolean
            If instance IsNot Nothing AndAlso String.Equals(instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is the provided setting instance using the Web settings provider?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsWebProvider(instance As DesignTimeSettingInstance) As Boolean
            If instance IsNot Nothing AndAlso String.Equals(instance.Provider, ServicesPropPageAppConfigHelper.ClientSettingsProviderName, StringComparison.Ordinal) Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is the provided setting instance using the local file settings provider?
        ''' </summary>
        ''' <remarks>Returns true for a NULL setting instance</remarks>
        Friend Shared Function IsLocalFileSettingsProvider(instance As DesignTimeSettingInstance) As Boolean
            If instance Is Nothing Then
                Return True
            ElseIf instance.Provider = "" Then
                Return True
            ElseIf String.Equals(instance.Provider, GetType(Configuration.LocalFileSettingsProvider).Name, StringComparison.Ordinal) Then
                Return True
            ElseIf String.Equals(instance.Provider, GetType(Configuration.LocalFileSettingsProvider).FullName, StringComparison.Ordinal) Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Should the scope for the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsScopeReadOnly(instance As DesignTimeSettingInstance, projectSupportsUserScopedSettings As Boolean) As Boolean
            If IsConnectionString(instance) Then
                Return True
            End If

            If IsWebProvider(instance) Then
                Return True
            End If

            If IsLocalFileSettingsProvider(instance) AndAlso Not projectSupportsUserScopedSettings _
               AndAlso (instance Is Nothing OrElse instance.Scope = DesignTimeSettingInstance.SettingScope.Application) _
            Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Does the current project system support user scoped settings?
        ''' </summary>
        ''' <param name="instance"></param>
        Friend Shared Function ProjectSupportsUserScopedSettings(instance As DesignTimeSettingInstance) As Boolean
            If instance Is Nothing OrElse instance.Site Is Nothing Then
                Return True
            Else
                Return ProjectSupportsUserScopedSettings(TryCast(instance.Site.GetService(GetType(IVsHierarchy)), IVsHierarchy))
            End If
        End Function

        ''' <summary>
        ''' Does the current project system support user scoped settings?
        ''' </summary>
        ''' <param name="hierarchy"></param>
        Friend Shared Function ProjectSupportsUserScopedSettings(hierarchy As IVsHierarchy) As Boolean
            If hierarchy Is Nothing Then
                Return True
            Else
                Return Not Common.ShellUtil.IsWebProject(hierarchy)
            End If
        End Function

        ''' <summary>
        ''' Should the roaming mode for the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsRoamingReadOnly(instance As DesignTimeSettingInstance) As Boolean
            If IsWebProvider(instance) Then
                Return True
            End If

            If instance IsNot Nothing AndAlso instance.Scope = DesignTimeSettingInstance.SettingScope.Application Then
                ' Application scoped settings can't be roaming, so we'll 
                ' indicate that this is a read-only property in that case...
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Should the name of the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsNameReadOnly(instance As DesignTimeSettingInstance) As Boolean
            If IsWebProvider(instance) Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Should the type of the provided setting type be read-only?
        ''' </summary>
        ''' <remarks>Returns false for a NULL setting instance</remarks>
        Friend Shared Function IsTypeReadOnly(instance As DesignTimeSettingInstance) As Boolean
            If IsWebProvider(instance) Then
                Return True
            Else
                Return False
            End If
        End Function

#End Region

#End Region

#Region "ISerializable implementation"

        Private Const SERIALIZATION_DESCRIPTION As String = "Description"
        Private Const SERIALIZATION_GENERATE_DEFAULT_VALUE_IN_CODE As String = "GenerateDefaultValueInCode"
        Private Const SERIALIZATION_IS_ROAMING As String = "Roaming"
        Private Const SERIALIZATION_NAME As String = "Name"
        Private Const SERIALIZATION_PROVIDER As String = "Provider"
        Private Const SERIALIZATION_TYPE As String = "SerializedType"
        Private Const SERIALIZATION_VALUE As String = "SerializedValue"
        Private Const SERIALIZATION_SCOPE As String = "Scope"

        'See .NET Framework Developer's Guide, "Custom Serialization" for more information
        Protected Sub New(Info As System.Runtime.Serialization.SerializationInfo, Context As System.Runtime.Serialization.StreamingContext)
            _description = Info.GetString(SERIALIZATION_DESCRIPTION)
            _generateDefaultValueInCode = Info.GetBoolean(SERIALIZATION_GENERATE_DEFAULT_VALUE_IN_CODE)
            _name = Info.GetString(SERIALIZATION_NAME)
            _roaming = Info.GetBoolean(SERIALIZATION_IS_ROAMING)
            _provider = Info.GetString(SERIALIZATION_PROVIDER)
            _settingTypeName = Info.GetString(SERIALIZATION_TYPE)
            _serializedValue = Info.GetString(SERIALIZATION_VALUE)
            _settingScope = CType(Info.GetInt32(SERIALIZATION_SCOPE), SettingScope)
        End Sub

        <Security.Permissions.SecurityPermission(Security.Permissions.SecurityAction.Demand, SerializationFormatter:=True)>
        Private Sub GetObjectData(Info As System.Runtime.Serialization.SerializationInfo, Context As System.Runtime.Serialization.StreamingContext) Implements System.Runtime.Serialization.ISerializable.GetObjectData
            Info.AddValue(SERIALIZATION_DESCRIPTION, _description)
            Info.AddValue(SERIALIZATION_GENERATE_DEFAULT_VALUE_IN_CODE, _generateDefaultValueInCode)
            Info.AddValue(SERIALIZATION_NAME, _name)
            Info.AddValue(SERIALIZATION_IS_ROAMING, _roaming)
            Info.AddValue(SERIALIZATION_PROVIDER, _provider)
            Info.AddValue(SERIALIZATION_TYPE, _settingTypeName)
            Info.AddValue(SERIALIZATION_VALUE, _serializedValue)
            Info.AddValue(SERIALIZATION_SCOPE, CType(_settingScope, Integer))
        End Sub

#End Region
    End Class

End Namespace
