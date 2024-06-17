' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Reflection
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common

Imports VSLangProj110

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Base class for the C# / VB build property pages
    ''' </summary>
    <TypeDescriptionProvider(GetType(AbstractControlTypeDescriptionProvider(Of BuildPropPageBase, PropPageUserControlBase)))>
    Friend MustInherit Class BuildPropPageBase
        Inherits PropPageUserControlBase

        Private Const _prefer32BitControlName As String = "Prefer32Bit"
        Private Const _preferNativeArm64ControlName As String = "PreferNativeArm64"

        Protected Const Const_OutputTypeEx As String = "OutputTypeEx"

        Private Function IsPrefer32BitSupportedForOutputType() As Boolean

            ' Get the current value of OutputTypeEx

            Dim propertyValue As Object = Nothing

            Try

                If Not GetCurrentProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, propertyValue) Then
                    Return False
                End If

            Catch exc As InvalidCastException
                Return False
            Catch exc As NullReferenceException
                Return False
            Catch ex As TargetInvocationException
                Return False
            End Try

            Dim uintValue As UInteger

            Try
                uintValue = CUInt(propertyValue)
            Catch ex As InvalidCastException
                Return False
            End Try

            ' Prefer32Bit is only allowed for Exe based output types
            Return uintValue = prjOutputTypeEx.prjOutputTypeEx_AppContainerExe OrElse
                   uintValue = prjOutputTypeEx.prjOutputTypeEx_Exe OrElse
                   uintValue = prjOutputTypeEx.prjOutputTypeEx_WinExe

        End Function

        Private Function IsPrefer32BitSupportedForTargetFramework() As Boolean

            Return IsTargetingDotNetFramework45OrAbove(ProjectHierarchy) OrElse
                   IsAppContainerProject(ProjectHierarchy) OrElse
                   IsTargetingDotNetCore(ProjectHierarchy)

        End Function

        Private Function IsPrefer32BitSupported() As Boolean

            Return IsAnyCPUPlatformTarget() AndAlso
                   IsPrefer32BitSupportedForOutputType() AndAlso
                   IsPrefer32BitSupportedForTargetFramework() AndAlso
                   IsFlagDisabled(_preferNativeArm64ControlName)

        End Function

        ' Holds the last value the Prefer32Bit check box had when enabled (or explicitly
        ' set by the project system), so that the proper state is restored if the 
        ' control is disabled and then later enabled
        Private _lastPrefer32BitValue As Boolean

        Protected Sub RefreshEnabledStatusForPrefer32Bit(control As CheckBox)

            Dim enabledBefore As Boolean = control.Enabled

            If control.Enabled Then
                _lastPrefer32BitValue = control.Checked
            End If

            EnableControl(control, IsPrefer32BitSupported())

            If enabledBefore AndAlso Not control.Enabled Then
                ' If transitioning from enabled to disabled, clear the checkbox.  When disabled, we
                ' want to show an unchecked checkbox regardless of the underlying property value.
                control.Checked = False

            ElseIf Not enabledBefore AndAlso control.Enabled Then

                ' If transitioning from disabled to enabled, restore the value of the checkbox.
                control.Checked = _lastPrefer32BitValue

            End If

        End Sub

        Protected Function Prefer32BitSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            If PropertyControlData.IsSpecialValue(value) Then
                ' Don't do anything if the value is missing or indeterminate
                Return False
            End If

            If TypeOf value IsNot Boolean Then
                ' Don't do anything if the value isn't of the expected type
                Return False
            End If

            If control.Enabled Then
                CType(control, CheckBox).Checked = CBool(value)
            Else
                ' The project is setting the property value while the control is disabled, so store the
                ' value for when the control is enabled
                _lastPrefer32BitValue = CBool(value)
            End If

            Return True
        End Function

        Protected Function Prefer32BitGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If Not control.Enabled Then

                ' If the control is not enabled, the checked state does not reflect the actual value
                ' of the property (the checkbox is always unchecked when disabled).  So we return the
                ' property value that we cached from when the control was last enabled (or explicitly
                ' set by the the project system while the control was disabled)
                value = _lastPrefer32BitValue
                Return True
            End If

            Dim checkBox As CheckBox = CType(control, CheckBox)
            value = checkBox.Checked

            Return True
        End Function

        Private Function IsAnyCPUPlatformTarget() As Boolean

            ' Get the current value of PlatformTarget

            Dim controlValue As Object = GetControlValueNative("PlatformTarget")

            If PropertyControlData.IsSpecialValue(controlValue) Then
                ' Property is missing or indeterminate
                Return False
            End If

            If TypeOf controlValue IsNot String Then
                Return False
            End If

            ' A flag is only allowed for AnyCPU

            Dim stringValue As String = CStr(controlValue)

            If String.IsNullOrEmpty(stringValue) Then
                ' Allow if the value is blank (means AnyCPU)
                Return True
            End If

            Return String.Equals(stringValue, "AnyCPU", StringComparison.Ordinal)

        End Function

        Private Function IsFlagDisabled(flagName As String) As Boolean

            ' Get the current value of control

            Dim controlValue As Object = GetControlValueNative(flagName)

            If PropertyControlData.IsSpecialValue(controlValue) Then
                ' Property is missing or indeterminate
                Return False
            End If

            If TypeOf controlValue IsNot Boolean Then
                Return False
            End If

            Return CBool(controlValue) = False

        End Function

        Private Function IsPreferNativeArm64Supported() As Boolean

            Return IsAnyCPUPlatformTarget() AndAlso
                IsFlagDisabled(_prefer32BitControlName)

        End Function

        ' Holds the last value the PreferNativeArm64 check box had when enabled (or explicitly
        ' set by the project system), so that the proper state is restored if the 
        ' control is disabled and then later enabled
        Private _lastPreferNativeArm64Value As Boolean

        Protected Sub RefreshEnabledStatusForPreferNativeArm64(control As CheckBox)

            Dim enabledBefore As Boolean = control.Enabled

            If control.Enabled Then
                _lastPreferNativeArm64Value = control.Checked
            End If

            EnableControl(control, IsPreferNativeArm64Supported())

            If enabledBefore AndAlso Not control.Enabled Then
                ' If transitioning from enabled to disabled, clear the checkbox.  When disabled, we
                ' want to show an unchecked checkbox regardless of the underlying property value.
                control.Checked = False

            ElseIf Not enabledBefore AndAlso control.Enabled Then

                ' If transitioning from disabled to enabled, restore the value of the checkbox.
                control.Checked = _lastPreferNativeArm64Value

            End If

        End Sub

        Protected Function PreferNativeArm64Set(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            If PropertyControlData.IsSpecialValue(value) Then
                ' Don't do anything if the value is missing or indeterminate
                Return False
            End If

            If TypeOf value IsNot Boolean Then
                ' Don't do anything if the value isn't of the expected type
                Return False
            End If

            If control.Enabled Then
                CType(control, CheckBox).Checked = CBool(value)
            Else
                ' The project is setting the property value while the control is disabled, so store the
                ' value for when the control is enabled
                _lastPreferNativeArm64Value = CBool(value)
            End If

            Return True
        End Function

        Protected Function PreferNativeArm64Get(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If Not control.Enabled Then

                ' If the control is not enabled, the checked state does not reflect the actual value
                ' of the property (the checkbox is always unchecked when disabled).  So we return the
                ' property value that we cached from when the control was last enabled (or explicitly
                ' set by the the project system while the control was disabled)
                value = _lastPreferNativeArm64Value
                Return True
            End If

            Dim checkBox As CheckBox = CType(control, CheckBox)
            value = checkBox.Checked

            Return True
        End Function

    End Class

End Namespace
