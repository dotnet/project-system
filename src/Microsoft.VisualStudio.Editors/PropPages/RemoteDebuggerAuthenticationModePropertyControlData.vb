' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.PropPageDesigner
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class RemoteDebuggerAuthenticationModePropertyControlData
        Inherits PropertyControlData

        Private Const RemoteDebuggerAuthenticationModePropertyName As String = "AuthenticationMode"

        Private ReadOnly _propertyPage As PropPageUserControlBase

        Public Sub New(propertyPage As PropPageUserControlBase, formControl As Control, assocControls As Control())

            MyBase.New(18000,
                       RemoteDebuggerAuthenticationModePropertyName,
                       formControl,
                       AddressOf SetAuthenticationMode,
                       AddressOf GetAuthenticationMode,
                       Nothing,
                       Nothing,
                       ControlDataFlags.UserPersisted,
                       assocControls)

            _propertyPage = propertyPage

        End Sub

        Protected Overrides Function GetUserDefinedPropertyDescriptor() As PropertyDescriptor

            Return New UserPropertyDescriptor(RemoteDebuggerAuthenticationModePropertyName, GetType(RemoteDebuggerAuthenticationMode))

        End Function

        Protected Overrides Function ReadUserDefinedProperty(propertyName As String, ByRef value As Object) As Boolean

            Dim propertyStorage = TryCast(_propertyPage.ProjectHierarchy, IVsBuildPropertyStorage)

            Dim configurationItems As ConfigurationState.DropdownItem() = Nothing
            Dim platformItems As ConfigurationState.DropdownItem() = Nothing
            GetConfigurationAndPlatformItems(configurationItems, platformItems)

            Dim stringValues = New List(Of String)

            For Each configurationItem In configurationItems
                If configurationItem.SelectionType <> ConfigurationState.SelectionTypes.All Then
                    For Each platformItem In platformItems
                        If platformItem.SelectionType <> ConfigurationState.SelectionTypes.All Then
                            Dim configName As String = configurationItem.Name + "|" + platformItem.Name
                            Dim configSpecificValue As String = Nothing
                            propertyStorage.GetPropertyValue(propertyName, configName, CType(_PersistStorageType.PST_USER_FILE, UInteger), configSpecificValue)
                            stringValues.Add(configSpecificValue)
                        End If
                    Next
                End If
            Next

            Dim uniqueStringValues = stringValues.Distinct()
            If (uniqueStringValues.Count > 1) Then
                value = RemoteDebuggerAuthenticationMode.NotSelected
            Else
                Dim enumValue As RemoteDebuggerAuthenticationMode
                [Enum].TryParse(uniqueStringValues.First(), enumValue)
                value = enumValue
            End If

            Return True

        End Function

        Protected Overrides Function WriteUserDefinedProperty(propertyName As String, value As Object) As Boolean

            Dim enumValue = CType(value, RemoteDebuggerAuthenticationMode)

            If enumValue = RemoteDebuggerAuthenticationMode.NotSelected Then
                Return False
            End If

            Dim propertyStorage = TryCast(_propertyPage.ProjectHierarchy, IVsBuildPropertyStorage)

            Dim configurationItems As ConfigurationState.DropdownItem() = Nothing
            Dim platformItems As ConfigurationState.DropdownItem() = Nothing
            GetConfigurationAndPlatformItems(configurationItems, platformItems)

            For Each configurationItem In configurationItems
                If configurationItem.SelectionType <> ConfigurationState.SelectionTypes.All Then
                    For Each platformItem In platformItems
                        If platformItem.SelectionType <> ConfigurationState.SelectionTypes.All Then
                            Dim configName As String = configurationItem.Name + "|" + platformItem.Name
                            propertyStorage.SetPropertyValue(propertyName, configName, CType(_PersistStorageType.PST_USER_FILE, UInteger), value.ToString())
                        End If
                    Next
                End If
            Next

            Return True

        End Function

        Private Sub GetConfigurationAndPlatformItems(
                    ByRef configurationItems() As ConfigurationState.DropdownItem,
                    ByRef platformItems() As ConfigurationState.DropdownItem)

            Dim propertyPageDesignerView = CType(_propertyPage.Parent.Parent.Parent, PropPageDesignerView)

            Dim selectedConfigurationItem = propertyPageDesignerView.GetSelectedConfigItem()
            If selectedConfigurationItem.SelectionType = ConfigurationState.SelectionTypes.All Then
                configurationItems = propertyPageDesignerView.ConfigurationState.ConfigurationDropdownEntries
            Else
                configurationItems = New ConfigurationState.DropdownItem() {selectedConfigurationItem}
            End If

            Dim selectedPlatformItem = propertyPageDesignerView.GetSelectedPlatformItem()
            If selectedPlatformItem.SelectionType = ConfigurationState.SelectionTypes.All Then
                platformItems = propertyPageDesignerView.ConfigurationState.PlatformDropdownEntries
            Else
                platformItems = New ConfigurationState.DropdownItem() {selectedPlatformItem}
            End If

        End Sub

        Private Shared Function SetAuthenticationMode(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            Dim authenticationModeComboBox = CType(control, ComboBox)
            Dim enumValue As RemoteDebuggerAuthenticationMode = CType(value, RemoteDebuggerAuthenticationMode)

            If authenticationModeComboBox.Items.Count = 0 Then
                authenticationModeComboBox.Items.Add(RemoteDebuggerAuthenticationMode.Windows)
                authenticationModeComboBox.Items.Add(RemoteDebuggerAuthenticationMode.None)
            End If

            Dim foundEntry As Object = Nothing
            For Each entry In authenticationModeComboBox.Items
                Dim authenticationModeEntry = CType(entry, RemoteDebuggerAuthenticationMode)
                If authenticationModeEntry = enumValue Then
                    foundEntry = entry
                    Exit For
                End If
            Next

            authenticationModeComboBox.SelectedItem = foundEntry
            Return True

        End Function

        Private Shared Function GetAuthenticationMode(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            Dim authenticationModeComboBox = CType(control, ComboBox)

            If authenticationModeComboBox.SelectedItem Is Nothing Then
                value = RemoteDebuggerAuthenticationMode.NotSelected
            Else
                Dim enumValue = CType(authenticationModeComboBox.SelectedItem, RemoteDebuggerAuthenticationMode)
                value = enumValue
            End If

            Return True

        End Function

    End Class

End Namespace
