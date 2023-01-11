' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell.Interop

Imports NativeMethods = Microsoft.VisualStudio.Editors.Interop.NativeMethods

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Base page for the C# / VB application pages.
    ''' 
    ''' This exists for shared logic that is not expected to be visible outside of this assembly (unlike
    ''' ApplicationPropPageBase which is public).
    ''' </summary>
    <TypeDescriptionProvider(GetType(AbstractControlTypeDescriptionProvider(Of ApplicationPropPageInternalBase, ApplicationPropPageBase)))>
    Friend MustInherit Class ApplicationPropPageInternalBase
        Inherits ApplicationPropPageBase

        Protected Const Const_OutputType As String = "OutputType"
        Protected Const Const_OutputTypeEx As String = "OutputTypeEx"

        Protected TargetFrameworkPropertyControlData As TargetFrameworkPropertyControlData

        Protected Overrides Sub CleanupCOMReferences()
            MyBase.CleanupCOMReferences()

            TargetFrameworkPropertyControlData.Cleanup()
        End Sub

        Protected Function SupportsProperty(propertyName As String) As Boolean

            Dim value As Object = TryGetNonCommonPropertyValue(GetPropertyDescriptor(propertyName))

            ' Verifies that the value is not missing or indeterminate
            Return Not PropertyControlData.IsSpecialValue(value)

        End Function

#Region "Output Type"
        Protected Function SupportsOutputTypeProperty() As Boolean
            ' Note:  For backwards compatibility, also verify that the old OutputType property is supported
            Return SupportsProperty(Const_OutputTypeEx) AndAlso
                   SupportsProperty(Const_OutputType)
        End Function

        Protected Function PopulateOutputTypeComboBoxFromProjectProperty(comboBox As ComboBox) As Boolean

            comboBox.Items.Clear()

            Dim propertyValue As Object = Nothing

            ' See if the project wants to override the defaults
            If ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID5.VSHPROPID_SupportedOutputTypes, propertyValue) = NativeMethods.S_OK Then

                ' Verify that the value is of the expected type and add the output types to the combo box
                Dim arrayValue As UInteger() = TryCast(propertyValue, UInteger())
                If arrayValue IsNot Nothing Then

                    For Each value As UInteger In arrayValue
                        comboBox.Items.Add(New OutputTypeComboBoxValue(value))
                    Next

                    Return True
                End If
            End If

            Return False

        End Function

        Protected Shared Function SelectItemInOutputTypeComboBox(comboBox As ComboBox, value As UInteger) As Boolean

            For Each item As Object In comboBox.Items
                Dim comboBoxValue As OutputTypeComboBoxValue = TryCast(item, OutputTypeComboBoxValue)
                If comboBoxValue IsNot Nothing AndAlso comboBoxValue.Value = value Then
                    comboBox.SelectedItem = item
                    Return True
                End If
            Next

            Return False
        End Function
#End Region

#Region "Start-up Object"

        ''' <summary>
        ''' Returns true if start-up objects other than "(None)" are supported
        ''' </summary>
        Protected Function StartUpObjectSupported() As Boolean
            Dim controlValue As Object = GetControlValueNative(Const_OutputTypeEx)

            If PropertyControlData.IsSpecialValue(controlValue) Then
                ' Startup object is not supported if the output type is missing or indeterminate
                Return False
            End If

            Return StartUpObjectSupported(CUInt(controlValue))
        End Function

        ''' <summary>
        ''' Returns true if start-up objects other than "(None)" are supported for this output type
        ''' </summary>
        Protected Shared Function StartUpObjectSupported(OutputType As UInteger) As Boolean
            Return OutputType = VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_Exe OrElse
                   OutputType = VSLangProj110.prjOutputTypeEx.prjOutputTypeEx_WinExe
        End Function
#End Region

#Region "Target Framework"

        ''' <summary>
        ''' Fill up the allowed values in the target framework combo box
        ''' </summary>
        Protected Sub PopulateTargetFrameworkComboBox(targetFrameworkComboBox As ComboBox)
            Dim targetFrameworkSupported As Boolean = False
            targetFrameworkComboBox.Items.Clear()
            targetFrameworkComboBox.SelectedIndex = -1

            Try
                Dim siteServiceProvider As OLE.Interop.IServiceProvider = Nothing
                VSErrorHandler.ThrowOnFailure(ProjectHierarchy.GetSite(siteServiceProvider))
                Dim sp As New Shell.ServiceProvider(siteServiceProvider)
                Dim vsFrameworkMultiTargeting As IVsFrameworkMultiTargeting = TryCast(sp.GetService(GetType(SVsFrameworkMultiTargeting).GUID), IVsFrameworkMultiTargeting)

                ' TODO: Remove IsTargetFrameworksDefined check after issue #800 is resolved.
                If TargetFrameworksDefined() = False And vsFrameworkMultiTargeting IsNot Nothing Then

                    Dim supportedTargetFrameworksDescriptor As PropertyDescriptor = GetPropertyDescriptor("SupportedTargetFrameworks")

                    Dim supportedFrameworks As IReadOnlyList(Of TargetFrameworkMoniker) = TargetFrameworkMoniker.GetSupportedTargetFrameworkMonikers(vsFrameworkMultiTargeting, DTEProject, supportedTargetFrameworksDescriptor?.Converter)

                    'If the list doesn't contain any tfm, it means the project can't retarget.
                    If Not supportedFrameworks.Any() Then
                        targetFrameworkSupported = False
                        targetFrameworkComboBox.Items.Clear()
                    Else
                        For Each supportedFramework As TargetFrameworkMoniker In supportedFrameworks
                            targetFrameworkComboBox.Items.Add(supportedFramework)
                        Next

                        ' Set the service provider to be used when choosing the 'Install other frameworks...' item
                        targetFrameworkComboBox.Items.Add(New InstallOtherFrameworksComboBoxValue())
                        TargetFrameworkPropertyControlData.Site = siteServiceProvider

                        targetFrameworkSupported = True
                    End If

                End If

            Catch ex As Exception When ReportWithoutCrash(ex, "Couldn't retrieve target framework assemblies, disabling combobox", NameOf(ApplicationPropPageInternalBase))
                Switches.TracePDProperties(TraceLevel.Warning, ": {0}", ex.ToString())
                targetFrameworkSupported = False
                targetFrameworkComboBox.Items.Clear()
            End Try

            If Not targetFrameworkSupported Then
                targetFrameworkComboBox.Enabled = False
            End If
        End Sub

        Private Function TargetFrameworksDefined() As Boolean
            Dim obj As Object
            Dim propTargetFrameworks As PropertyDescriptor
            propTargetFrameworks = GetPropertyDescriptor("TargetFrameworks")
            obj = TryGetNonCommonPropertyValue(propTargetFrameworks)
            Dim stTargetFrameworks As String = TryCast(obj, String)
            If String.IsNullOrEmpty(stTargetFrameworks) Then
                Return False
            End If
            Return True
        End Function
        ''' <summary>
        ''' Takes the current value of the TargetFrameworkMoniker property (in string format), and sets
        '''   the current dropdown list to that value.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function SetTargetFrameworkMoniker(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Dim combobox As ComboBox = CType(control, ComboBox)
            Dim previouslySelectedIndex = combobox.SelectedIndex
            combobox.SelectedIndex = -1
            If value Is Nothing Or PropertyControlData.IsSpecialValue(value) Then 'Indeterminate or IsMissing
                'Leave it unselected
            Else
                Dim stringValue As String = DirectCast(value, String)
                For Each entry As Object In combobox.Items
                    Dim targetFrameworkMoniker As TargetFrameworkMoniker = TryCast(entry, TargetFrameworkMoniker)
                    If targetFrameworkMoniker IsNot Nothing AndAlso targetFrameworkMoniker.Moniker = stringValue Then
                        combobox.SelectedItem = entry
                        Exit For
                    End If
                Next
            End If

            TargetFrameworkPropertyControlData.IndexOfLastCommittedValue = combobox.SelectedIndex

            If combobox.SelectedIndex <> previouslySelectedIndex Then
                TargetFrameworkMonikerChanged()
            End If

            Return True
        End Function

        ''' <summary>
        ''' Retrieves the current value of the TargetFramework dropdown text and converts it into
        '''   the native property type of string so it can be stored into the project's property.
        '''   Called by the base class code.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Shared Function GetTargetFrameworkMoniker(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim currentTarget As TargetFrameworkMoniker = CType(CType(control, ComboBox).SelectedItem, TargetFrameworkMoniker)
            If currentTarget IsNot Nothing Then
                value = currentTarget.Moniker
                Return True
            End If

            Debug.Fail("The combobox should not have still been unselected yet be dirty")
            Return False
        End Function

        ''' <summary>
        ''' Called by <see cref="SetTargetFrameworkMoniker(Control, PropertyDescriptor, Object)"/>
        ''' if the target framework changes. Derived controls can override this method to take
        ''' actions such as showing or hiding the Auto-generate Binding Redirects check box.
        ''' </summary>
        Protected Overridable Sub TargetFrameworkMonikerChanged()

        End Sub

#End Region

#Region "Auto-generate Binding Redirects"

        Protected Sub ShowAutoGeneratedBindingRedirectsCheckBox(bindingRedirectsCheckBox As CheckBox)
            If (IsTargetingDotNetFramework(ProjectHierarchy)) Then
                bindingRedirectsCheckBox.Visible = True
            Else
                bindingRedirectsCheckBox.Visible = False
            End If
        End Sub

#End Region

    End Class

End Namespace
