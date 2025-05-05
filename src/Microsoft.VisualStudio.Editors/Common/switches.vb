' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Microsoft.VisualStudio.Editors.PropertyPages

Namespace Microsoft.VisualStudio.Editors.Common

#If 0 Then

     Using these is a three-step process.

     1) Define a switch
        E.g.:

      Friend Class Switches
        .
        .
        .
        Public Shared FileWatcher As New TraceSwitch("FileWatcher", "Trace the property page editor FileWatcher class.")
        .
        .
        .
      End Class

     2) Please also add your switch to the example <system.diagnostics> section below, so everyone can simply copy this
        directly into their devenv.exe.config file.


     3) Use it in your code
        E.g.:

        Debug.WriteLineIf(Switches.FileWatcher.TraceVerbose, "FileWatcher: file changed")

       or define a function to use:

        <Conditional("DEBUG")> _
        Public Shared Sub Trace(Message As String, ParamArray FormatArguments() As Object)
            #If DEBUG Then 'NOTE: The Conditional("DEBUG") attribute keeps callsites from compiling a call
                           '  to the function, but it does *not* keep the function body from getting compiled
            Debug.WriteLineIf(Switches.FileWatcher.TraceVerbose, "FileWatcher: " & String.Format(Message, FormatArguments))
            #End If
        End Sub


     4) Add tracing info to the devenv.exe.config file (make sure you've got the one in the directory you're
        actually running devenv from).  This is done in the configuration section.

        E.g.:

        <?xml version ="1.0"?>
           .
           .
           .
        <configuration>
           .
           .
           .

           ***COPY THE FOLLOWING SECTION INTO YOUR DEVENV.EXE.CONFIG FILE INTO THE CONFIGURATION SECTION:

            <system.diagnostics>
                <switches>
                    <add name="UndoEngine" value="0" />
                    <add name="RSEResourceSerializationService" value="0" />
                    <add name="RSEFileWatcher" value="0" />
                    <add name="RSEAddRemoveResources" value="0" />
                    <add name="RSEVirtualStringTable" value="0" />
                    <add name="RSEVirtualListView" value="0" />
                    <add name="RSEDelayCheckErrors" value="0" />
                    <add name="RSEDisableHighQualityThumbnails" value="false" />
                    <add name="DFContextMenu" value="0" />
                    <add name="ResourcesFolderService" value="0" />
                    <add name="DEBUGPBRS" value="0" />
                    <add name="RSEFindReplace" value="0" />
                    <add name="MSVBE_SCC" value="0" />
                    <add name="PDDesignerActivations" value="0" />
                    <add name="PDFocus" value="0" />
                    <add name="PDUndo" value="0" />
                    <add name="PDProperties" value="0" />
                    <add name="PDApplicationType" value="0" />
                    <add name="PDConfigs" value="0" />
                    <add name="PDPerf" value="0" />
                    <add name="PDCmdTarget" value="0" />
                    <add name="PDAlwaysUseSetParent" value="false" />
                    <add name="PDMessageRouting" value="0" />
                    <add name="SDSyncUserConfig" value="0" />
                    <add name="SDSerializeSettings" value="0" />
                    <add name="PDAddVBWPFApplicationPageToAllProjects" value="false" />
                    <add name="PDAccessModifierCombobox" value="0" />
                    <add name="PDLinqImports" value="0" />

                    <add name="WCF_Config_FileChangeWatch" value="0" />
                    <add name="WCF_ASR_DebugServiceInfoNodes" value="0" />

                    <add name="MyExtensibilityTrace" value="0" />

                    <!-- Uncomment one of the following to overload the SKU for the project designer -->
                    <!-- <add name="PDSku" value="Express" /> -->
                    <!-- <add name="PDSku" value="Standard" /> -->
                    <!-- <add name="PDSku" value="VSTO" /> -->
                    <!-- <add name="PDSku" value="Professional" /> -->
                    <!-- <add name="PDSku" value="AcademicStudent" /> -->
                    <!-- <add name="PDSku" value="DownloadTrial" /> -->
                    <!-- <add name="PDSku" value="Enterprise" /> -->

                    <!-- Uncomment one of the following to overload the sub-SKU for the project designer -->
                    <!-- <add name="PDSubSku" value="VC" /> -->
                    <!-- <add name="PDSubSku" value="VB" /> -->
                    <!-- <add name="PDSubSku" value="CSharp" /> -->
                    <!-- <add name="PDSubSku" value="Architect" /> -->
                    <!-- <add name="PDSubSku" value="IDE" /> -->
                    <!-- <add name="PDSubSku" value="Web" /> -->

                </switches>
            </system.diagnostics>


           *** END OF SECTION TO COPY

           .
           .
           .
        </configuration>


    5) modify the values of any switches you want to turn on

     Note: the valid "value" values (levels) are as follows:

        0      Off
        1      Error
        2      Warning
        3      Info
        4      Verbose    -> this is generally what you want when you want to enable one of these switches in the config file

#End If

    ''' <summary>
    ''' Contains predefined switches for enabling/disabling trace output or code instrumentation.
    ''' </summary>
    Friend Class Switches

        '------------- Designer Framework -------------

        ''' <summary>
        ''' Trace the showing of context menus via the base control classes in DesignerFramework
        ''' </summary>
        Public Shared DFContextMenu As New TraceSwitch("DFContextMenu", "Trace the showing of context menus via the base control classes in DesignerFramework")

        '------------- Common switches for Microsoft.VisualStudio.Editors -------------

        ''' <summary>
        ''' Trace source code control integration behavior in Microsoft.VisualStudio.Editors.dll
        ''' </summary>
        Public Shared MSVBE_SCC As New TraceSwitch("MSVBE_SCC", "Trace source code control integration behavior in Microsoft.VisualStudio.Editors.dll")

        '------------- Project Designer -------------

        ''' <summary>
        ''' Trace when the active designer changes in the project designer
        ''' </summary>
        Public Shared PDDesignerActivations As New TraceSwitch("PDDesignerActivations", "Trace when the active designer changes in the project designer")

        ''' <summary>
        ''' Trace project designer focus-related events
        ''' </summary>
        Public Shared PDFocus As New TraceSwitch("PDFocus", "Trace project designer focus-related events")

        ''' <summary>
        ''' Trace behavior of multiple-value undo and redo
        ''' </summary>
        Public Shared PDUndo As New TraceSwitch("PDUndo", "Trace behavior of multiple-value undo and redo")

        ''' <summary>
        ''' Trace the creation and dirtying of properties, apply, etc.
        ''' </summary>
        Public Shared PDProperties As New TraceSwitch("PDProperties", "Trace the creation and dirtying of properties in property pages, apply, etc.")

        ''' <summary>
        ''' Trace mapping of application type - output type, MyType
        ''' </summary>
        Public Shared PDApplicationType As New TraceSwitch("PDApplicationType", "Trace mapping of application type properties")

        ''' <summary>
        ''' Trace the functionality of extender properties
        ''' </summary>
        Public Shared PDExtenders As New TraceSwitch("PDExtenders", "Trace the functionality of extender properties")

        ''' <summary>
        ''' Trace configuration setup and changes tracking in the project designer
        ''' </summary>
        Public Shared PDConfigs As New TraceSwitch("PDConfigs", "Trace configuration setup and changes tracking in the project designer")

        ''' <summary>
        ''' Trace performance issues for the project designer
        ''' </summary>
        Public Shared PDPerf As New TraceSwitch("PDPerf", "Trace performance issues for the project designer")

        ''' <summary>
        ''' Trace command routing (CmdTargetHelper, etc.) in the project designer
        ''' </summary>
        Public Shared PDCmdTarget As New TraceSwitch("PDCmdTarget", "Trace command routing (CmdTargetHelper, etc.) in the project designer")

        ''' <summary>
        ''' Always use native ::SetParent() instead of setting the WinForms Parent property for property page hosting.
        ''' This is useful for testing the hosting of pages as it would occur for non-native pages.
        ''' </summary>
        Public Shared PDAlwaysUseSetParent As New BooleanSwitch("PDAlwaysUseSetParent", "Always use native ::SetParent() instead of setting the WinForms Parent property for property page hosting")

        ''' <summary>
        ''' Traces message routing in the project designer and its property pages
        ''' </summary>
        Public Shared PDMessageRouting As New TraceSwitch("PDMessageRouting", "Traces message routing in the project designer and its property pages")

        ''' <summary>
        ''' Overrides the SKU edition value for the project designer
        ''' </summary>
        Public Shared PDSku As New EnumSwitch(Of VSProductSKU.VSASKUEdition)("PDSku", "Overrides the SKU edition value for the project designer")

        ''' <summary>
        ''' Overrides the Sub-SKU edition value for the project designer
        ''' </summary>
        Public Shared PDSubSku As New EnumSwitch(Of VSProductSKU.VSASubSKUEdition)("PDSubSku", "Overrides the Sub-SKU edition value for the project designer")

        Public Shared PDAddVBWPFApplicationPageToAllProjects As New BooleanSwitch("PDAddVBWPFApplicationPageToAllProjects",
            "Add the VB WPF Application property page to all projects, even non-WPF projects.  This allows for debugging " _
            & "this page without the new WPF flavor")

        Public Shared PDAccessModifierCombobox As New TraceSwitch("PDAccessModifierCombobox", "Traces the access modifier combobox functionality")

        Public Shared PDLinqImports As New TraceSwitch("PDLinqImports", "Traces the adding and removing of Linq imports during target framework upgrade/downgrade")

        '------------- Settings Designer -------------
        Public Shared SDSyncUserConfig As New TraceSwitch("SDSyncUserConfig", "Trace synhronization/deletion of user.config files")

        ''' <summary>
        ''' Tracing whenever we read/write .settings and/or app.config files...
        ''' </summary>
        Public Shared SDSerializeSettings As New TraceSwitch("SDSerializeSettings", "Serialization/deserialization of settings")

        '------------- WCF Tooling -------------

        Public Shared WCF_Config_FileChangeWatch As New TraceSwitch("WCF_Config_FileChangeWatch", "Changes to configuration files in the current project")

        Public Shared WCF_ASR_DebugServiceInfoNodes As New TraceSwitch("WCF_ASR_DebugServiceInfoNodes", "Displays additional information about the ServiceInfoNodes in the Services treeview in the ASR dialog")

        '------------- MyExtensibility -------------
        Public Shared MyExtensibilityTraceSwitch As New TraceSwitch("MyExtensibilityTrace", "Trace switch for MyExtensibility Feature")

        '--------------- Functions (optional, but make usage easier) ------------------

#Region "Utility functions"

#If DEBUG Then
        ''' <summary>
        ''' Uses String.Format if there are arguments, otherwise simply returns the string.
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        Private Shared Function Format(Message As String, ParamArray FormatArguments() As Object) As String
            If FormatArguments Is Nothing OrElse FormatArguments.Length = 0 Then
                Return Message
            Else
                Try
                    Return String.Format(Message, FormatArguments)
                Catch ex As FormatException
                    'If there was an exception trying to format (e.g., the Message contained the {} characters), just
                    '  return the string - this stuff is only for debug 
                    Return Message
                End Try
            End If

            Return String.Empty
        End Function
#End If

#If DEBUG Then
        Private Shared s_timeCodeStart As Date
        Private Shared s_firstTimeCodeTaken As Boolean
#End If

        Friend Shared Function TimeCode() As String
#If DEBUG Then
            If Not s_firstTimeCodeTaken Then
                ResetTimeCode()
            End If

            Dim ts As TimeSpan = Now.Subtract(s_timeCodeStart)
            Return ts.TotalSeconds.ToString("0000.00000") & vbTab
            'Return n.ToString("hh:mm:ss.") & Microsoft.VisualBasic.Format(n.Millisecond, "000") & VB.vbTab
#Else
            Return ""
#End If
        End Function

        <Conditional("DEBUG")>
        Friend Shared Sub ResetTimeCode()
#If DEBUG Then
            s_timeCodeStart = Now
            s_firstTimeCodeTaken = True
#End If
        End Sub

#Region "EnumSwitch(Of T)"

        ''' <summary>
        ''' A Switch which has a simple enum value (either as integer or string representation)
        ''' </summary>
        Friend Class EnumSwitch(Of T)
            Inherits Switch

            Public Sub New(DisplayName As String, Description As String)
                MyBase.New(DisplayName, Description)
                Debug.Assert(GetType([Enum]).IsAssignableFrom(GetType(T)), "EnumSwitch() requires an Enum as a type parameter")
            End Sub

            ''' <summary>
            ''' True iff the switch has a non-empty value
            ''' </summary>
            Public ReadOnly Property ValueDefined As Boolean
                Get
                    Return MyBase.Value <> "" AndAlso CInt(Convert.ChangeType(Value, TypeCode.Int32)) <> 0
                End Get
            End Property

            ''' <summary>
            ''' Gets/sets the current value of the switch
            ''' </summary>
            Public Shadows Property Value As T
                Get
                    Return CType([Enum].Parse(GetType(T), MyBase.Value), T)
                End Get
                Set
                    MyBase.Value = Value.ToString()
                End Set
            End Property

            ''' <summary>
            ''' Interprets the new (string-based) correctly, based on the string or
            '''   integer representation.
            ''' </summary>
            Protected Overrides Sub OnValueChanged()
                SwitchSetting = CInt(Convert.ChangeType(Value, TypeCode.Int32))
            End Sub

        End Class

#End Region

#End Region

        ''' <summary>
        ''' Trace messages for the MSVBE_SCC flag
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        <Conditional("DEBUG")>
        Public Shared Sub TraceSCC(Message As String, ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(MSVBE_SCC.TraceVerbose, "MSVBE_SCC: " & Format(Message, FormatArguments))
#End If
        End Sub

        ''' <summary>
        ''' Trace project designer focus-related events
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        <Conditional("DEBUG")>
        Public Shared Sub TracePDFocus(Level As TraceLevel, Message As String, ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDFocus.Level >= Level, "PDFocus:" & vbTab & TimeCode() & Format(Message, FormatArguments))
#End If
        End Sub

        ''' <summary>
        ''' Trace project designer focus-related events
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        <Conditional("DEBUG")>
        Public Shared Sub TracePDProperties(Level As TraceLevel, Message As String, ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDProperties.Level >= Level, "PDProperties: " & Format(Message, FormatArguments))
#End If
        End Sub

        ''' <summary>
        ''' Trace configuration setup and changes tracking in the project designer
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <param name="FormatArguments"></param>
        <Conditional("DEBUG")>
        Public Shared Sub TracePDPerf(Message As String, ParamArray FormatArguments() As Object)
#If DEBUG Then
            Trace.WriteLineIf(PDPerf.TraceInfo, "PDPerf:" & vbTab & TimeCode() & Format(Message, FormatArguments))
#End If
        End Sub

        ''' <summary>
        ''' Traces the access modifier combobox functionality
        ''' </summary>
        <Conditional("DEBUG")>
        Public Shared Sub TracePDAccessModifierCombobox(traceLevel As TraceLevel, message As String)
#If DEBUG Then
            Trace.WriteLineIf(PDAccessModifierCombobox.Level >= traceLevel, "PDAccessModifierCombobox: " & message)
#End If
        End Sub

        ''' <summary>
        ''' Trace serialization of settings
        ''' </summary>
        ''' <param name="tracelevel"></param>
        ''' <param name="message"></param>
        <Conditional("DEBUG")>
        Public Overloads Shared Sub TraceSDSerializeSettings(tracelevel As TraceLevel, message As String)
#If DEBUG Then
            Trace.WriteLineIf(SDSerializeSettings.Level >= tracelevel, message)
#End If
        End Sub

        ''' <summary>
        ''' Trace serialization of settings
        ''' </summary>
        ''' <param name="tracelevel"></param>
        ''' <param name="formatString"></param>
        ''' <param name="parameters"></param>
        <Conditional("DEBUG")>
        Public Overloads Shared Sub TraceSDSerializeSettings(tracelevel As TraceLevel, formatString As String, ParamArray parameters() As Object)
#If DEBUG Then
            Trace.WriteLineIf(SDSerializeSettings.Level >= tracelevel, String.Format(formatString, parameters))
#End If
        End Sub

        <Conditional("DEBUG")>
        Public Shared Sub TraceMyExtensibility(traceLevel As TraceLevel, message As String)
#If DEBUG Then
            Trace.WriteLineIf(MyExtensibilityTraceSwitch.Level >= traceLevel, String.Format("MyExtensibility {0} {1}: ", Date.Now.ToLongDateString(), Date.Now.ToLongTimeString()))
            Trace.WriteLineIf(MyExtensibilityTraceSwitch.Level >= traceLevel, message)
#End If
        End Sub

    End Class

End Namespace
