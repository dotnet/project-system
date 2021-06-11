' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Microsoft.VisualStudio.Shell.Interop

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' A serializable class which is capable of storing different values of a property
    ''' for different configurations, to enable undo/redo in "All configurations" and 
    ''' "All Platforms" modes.
    ''' </summary>
    <Serializable>
    Public Class MultipleValuesStore

        'Note: the sizes of these arrays are all the same
        Public ConfigNames As String()   'The config name applicable to each stored value
        Public PlatformNames As String() 'The platform name applicable to each stored value
        Public Values As Object()        'The stored values themselves

        Public SelectedConfigName As String 'The currently-selected configuration in the comboboxes.  Empty value indicates "All Configurations".
        Public SelectedPlatformName As String 'The currently-selected platform in the comboboxes.  Empty value indicates "All Platforms".

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="VsCfgProvider">IVsCfgProvider2</param>
        ''' <param name="Objects">The current set of objects (IVsCfg) from which the matching Values were pulled.</param>
        ''' <param name="Values">The values to persist</param>
        ''' <param name="SelectedConfigName">The selected configuration in the drop-down combobox.  Empty string indicates "All Configurations".</param>
        ''' <param name="SelectedPlatformName">The selected platform in the drop-down combobox.  Empty string indicates "All Platforms".</param>
        Public Sub New(VsCfgProvider As IVsCfgProvider2, Objects() As Object, Values() As Object, SelectedConfigName As String, SelectedPlatformName As String)
            Requires.NotNull(Values, NameOf(Values))
            Requires.NotNull(Objects, NameOf(Objects))

            If Values.Length <> Objects.Length Then
                Debug.Fail("Bad array length returned from GetPropertyMultipleValues()")
                Throw Common.CreateArgumentException(NameOf(Values))
            End If

            Me.SelectedConfigName = SelectedConfigName
            Me.SelectedPlatformName = SelectedPlatformName

            ConfigNames = New String(Objects.Length - 1) {}
            PlatformNames = New String(Objects.Length - 1) {}
            Me.Values = New Object(Objects.Length - 1) {}

            For i As Integer = 0 To Objects.Length - 1
                Dim Cfg As IVsCfg = TryCast(Objects(i), IVsCfg)
                If Cfg IsNot Nothing Then
                    Dim ConfigName As String = ""
                    Dim PlatformName As String = ""
                    Common.ShellUtil.GetConfigAndPlatformFromIVsCfg(Cfg, ConfigName, PlatformName)

#If DEBUG Then
                    Dim Cfg2 As IVsCfg = Nothing
                    VsCfgProvider.GetCfgOfName(ConfigName, PlatformName, Cfg2)
                    Debug.Assert(Cfg2 IsNot Nothing AndAlso Cfg2 Is Cfg, "Unable to correctly decode config name and map it back to the config")
#End If
                    ConfigNames(i) = ConfigName
                    PlatformNames(i) = PlatformName
                    Me.Values(i) = Values(i)
                Else
                    Debug.Fail("Unexpected type passed in to MultipleValues.  Currently only IVsCfg supported.  If it's a common (non-config) property, why are we creating MultipleValues for it?")
                    Throw Common.CreateArgumentException(NameOf(Values))
                End If
            Next

            DebugTrace("MultiValues constructor")
        End Sub

        ''' <summary>
        ''' Determines the set of configurations which correspond to the stored
        '''   configuration names and platforms.
        ''' </summary>
        ''' <param name="VsCfgProvider"></param>
        Public Function GetObjects(VsCfgProvider As IVsCfgProvider2) As Object()
            Debug.Assert(ConfigNames IsNot Nothing AndAlso PlatformNames IsNot Nothing AndAlso Values IsNot Nothing)
            Debug.Assert(Values.Length = ConfigNames.Length AndAlso ConfigNames.Length = PlatformNames.Length, "Huh?")

            DebugTrace("MultiValues.GetObjects()")

            'Figure out the configurations which the config/platform name combinations refer to
            Dim Objects() As Object = New Object(ConfigNames.Length - 1) {}
            For i As Integer = 0 To ConfigNames.Length - 1
                Dim Cfg As IVsCfg = Nothing
                If VSErrorHandler.Succeeded(VsCfgProvider.GetCfgOfName(ConfigNames(i), PlatformNames(i), Cfg)) Then
                    Objects(i) = Cfg
                Else
                    Throw New Exception(My.Resources.Designer.GetString(My.Resources.Designer.PPG_ConfigNotFound_2Args, ConfigNames(i), PlatformNames(i)))
                End If
            Next

            Return Objects
        End Function

        <Conditional("DEBUG")>
        Public Sub DebugTrace(Message As String)
            Debug.Assert(ConfigNames.Length = PlatformNames.Length AndAlso PlatformNames.Length = Values.Length)
            Common.Switches.TracePDUndo(Message)
            Trace.Indent()
            For i As Integer = 0 To ConfigNames.Length - 1
                Common.Switches.TracePDUndo("[" & ConfigNames(i) & "|" & PlatformNames(i) & "] Value=" & Common.DebugToString(Values(i)))
            Next
            Trace.Unindent()
        End Sub

    End Class

End Namespace
