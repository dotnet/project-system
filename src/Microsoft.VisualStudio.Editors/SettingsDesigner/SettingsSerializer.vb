' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Xml

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner


    ''' <summary> Utility class to (de)serialize the contents of a DesignTimeSetting object given a stream reader/writer. </summary>
    Friend NotInheritable Class SettingsSerializer

        Friend Class SettingsSerializerException
            Inherits ApplicationException

            Public Sub New(
                            message As String
                          )
                MyBase.New(message)
            End Sub

            Public Sub New(
                            message As String,
                            inner As Exception
                          )
                MyBase.New(message, inner)
            End Sub
        End Class

        Public Const SettingsSchemaUri = "http://schemas.microsoft.com/VisualStudio/2004/01/settings"
        Public Const SettingsSchemaUriOLD = "uri:settings"

        Public Const CultureInvariantVirtualTypeNameConnectionString = "(Connection string)"
        Public Const CultureInvariantVirtualTypeNameWebReference = "(Web Service URL)"

#If USE_SETTINGS_XML_SCHEMA_VALIDATION Then
        ' We have disabled the schema validation for now - it caused perf problems for a couple of user scenarios
        ' (i.e. adding a database to an empty project, which in turn adds a connection string to the settings object)
        ' 
        ' Loading the schema added ~1s which was not acceptable. I have left the code in here in case we find another
        ' way to load it....
        '
        ' #define:ing USE_SETTINGS_XML_SCHEMA_VALIDATION will re-enable schema validation...
        Private Shared s_SchemaLoadFailed As Boolean = False

        ''' <summary> Demand create an XML Schema instance for .settings files. </summary>
        Private Shared ReadOnly Property Schema() As System.Xml.Schema.XmlSchema
            Get
                Static schemaInstance As System.Xml.Schema.XmlSchema
                If schemaInstance Is Nothing AndAlso Not s_SchemaLoadFailed Then
                    Dim SchemaStream As Stream
                    SchemaStream = GetType(SettingsSerializer).Assembly.GetManifestResourceStream(GetType(SettingsSerializer), "SettingsSchema")
                    schemaInstance = System.Xml.Schema.XmlSchema.Read(SchemaStream, AddressOf SchemaValidationEventHandler)
                End If
                Return schemaInstance
            End Get
        End Property

        ''' <summary> If we fail to load the schema, things are bad indeed... </summary>
        ''' <param name="sender"/>
        ''' <param name="e"/>
        Private Shared Sub SchemaValidationEventHandler(sender As Object, e As System.Xml.Schema.ValidationEventArgs)
            System.Diagnostics.Debug.Fail("Failed to load XML schema from manifest resource stream!")
            s_SchemaLoadFailed = True
        End Sub

        ''' <summary> Stores all validation errors from a ValidatingReader. </summary>
        Private Class ValidationErrorBag
            Private m_ValidationErrors As New System.Collections.ArrayList

            Friend ReadOnly Property Errors() As System.Collections.ICollection
                Get
                    Return m_ValidationErrors
                End Get
            End Property

            Friend Sub ValidationEventHandler(sender As Object, e As System.Xml.Schema.ValidationEventArgs)
                m_ValidationErrors.Add(e)
            End Sub
        End Class
#End If

        ''' <summary> Deserialize XML stream of settings. </summary>
        ''' <param name="Settings">Instance to populate</param>
        ''' <param name="Reader">Text reader on stream containing serialized settings</param>
        Public Shared Sub Deserialize(
                                       Settings As DesignTimeSettings,
                                       Reader As TextReader,
                                       getRuntimeValue As Boolean
                                     )
            Dim XmlDoc2 As Linq.XNode

#If USE_SETTINGS_XML_SCHEMA_VALIDATION Then
            Dim ValidationErrors As New ValidationErrorBag
            If Schema IsNot Nothing Then
                Dim ValidatingReader As New XmlValidatingReader(New XmlTextReader(Reader))
                Dim SchemaCol As New System.Xml.Schema.XmlSchemaCollection()
                ValidatingReader.Schemas.Add(Schema)
                Try
                    AddHandler ValidatingReader.ValidationEventHandler, AddressOf ValidationErrors.ValidationEventHandler
                    XmlDoc2.Load(ValidatingReader)
                Finally
                    RemoveHandler ValidatingReader.ValidationEventHandler, AddressOf ValidationErrors.ValidationEventHandler
                End Try
            Else
#End If
            ' CONSIDER, should I throw here to prevent the designer loader from blowing up / loading only part
            ' of the file and clobber it on the next write

            Dim xmlReader As New System.Xml.XmlTextReader(Reader) With {.Normalization = False}


            XmlDoc2 = Linq.XDocument.ReadFrom(xmlReader)

#If USE_SETTINGS_XML_SCHEMA_VALIDATION Then
            End If
            If ValidationErrors.Errors.Count > 0 Then
                Dim sb As New System.Text.StringBuilder
                For Each e As System.Xml.Schema.ValidationEventArgs In ValidationErrors.Errors
                    sb.AppendLine(e.Message)
                Next
                Throw New XmlException(sb.ToString())
            End If
#End If

            Dim XmlNamespaceManager As New XmlNamespaceManager(xmlReader.NameTable)
            XmlNamespaceManager.AddNamespace("Settings", SettingsSchemaUri)

            Dim RootNode2 = XmlDoc2.Document.<SettingsFile>.FirstOrDefault

            ' Enable support of pre-Beta2 settings namespace files -- if we didn't find the root node using the new namespace, then try the old one
            '
            If (RootNode2 Is Nothing) Then
                XmlNamespaceManager.RemoveNamespace("Settings", SettingsSchemaUri)
                XmlNamespaceManager.AddNamespace("Settings", SettingsSchemaUriOLD)
                ' now that we have the old namespace set up, try selecting the root node again
                RootNode2 = XmlDoc2.Document.<SettingsFile>.First 'SelectSingleNode("Settings:SettingsFile", XmlNamespaceManager)
            End If

            ' Deserialize setting group/description
            If RootNode2 IsNot Nothing Then
                Dim r0 = RootNode2.@GeneratedClassNamespace
                ' Deserialize persisted namespace
                If r0 IsNot Nothing Then Settings.PersistedNamespace = r0
                'If RootNode.Attributes("GeneratedClassNamespace") IsNot Nothing Then Settings.PersistedNamespace = RootNode.Attributes("GeneratedClassNamespace").Value

                ' In some cases, we want to use a specific class name and not base it on the name of the 
                ' .settings file...
                Dim mungeClassNameAttribute = RootNode2.@UseMySettingsClassName
                If mungeClassNameAttribute IsNot Nothing Then
                    Try
                        Settings.UseSpecialClassName = XmlConvert.ToBoolean(mungeClassNameAttribute)
                    Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(Deserialize), NameOf(SettingsSerializer))
                        Settings.UseSpecialClassName = False
                    End Try
                End If
            End If

            ' Deserialize settings
            Dim SettingNodes = RootNode2.<Settings>.<Setting>
            For Each SettingNode In SettingNodes

                Dim typeAttr = SettingNode.@Type
                Dim scopeAttr = SettingNode.@Scope
                Dim nameAttr = SettingNode.@Name

                If typeAttr Is Nothing OrElse scopeAttr Is Nothing OrElse nameAttr Is Nothing Then Throw New SettingsSerializer.SettingsSerializerException(SR.GetString(SR.SD_Err_CantLoadSettingsFile))

                Dim newSettingName As String = Settings.CreateUniqueName(nameAttr)
                If Not Settings.IsValidName(newSettingName) Then Throw New SettingsSerializer.SettingsSerializerException(SR.GetString(SR.SD_ERR_InvalidIdentifier_1Arg, nameAttr))

                Dim Instance As DesignTimeSettingInstance = Settings.AddNew(typeAttr, newSettingName, True)

                If scopeAttr.Equals(SettingsDesigner.ApplicationScopeName, System.StringComparison.Ordinal) Then
                    Instance.SetScope(DesignTimeSettingInstance.SettingScope.Application)
                Else
                    Instance.SetScope(DesignTimeSettingInstance.SettingScope.User)
                End If

                Dim generateDefaultValueAttribute = SettingNode.@GenerateDefaultValueInCode
                Dim descriptionAttr = SettingNode.@Description
                Dim providerAttr = SettingNode.@Provider
                Dim RoamingAttr = SettingNode.@Roaming

                If descriptionAttr IsNot Nothing Then Instance.SetDescription(descriptionAttr)
                If providerAttr IsNot Nothing Then Instance.SetProvider(providerAttr)
                If RoamingAttr IsNot Nothing Then Instance.SetRoaming(XmlConvert.ToBoolean(RoamingAttr))

                If generateDefaultValueAttribute IsNot Nothing AndAlso generateDefaultValueAttribute <> "" AndAlso Not XmlConvert.ToBoolean(generateDefaultValueAttribute) Then
                    Instance.SetGenerateDefaultValueInCode(False)
                Else
                    Instance.SetGenerateDefaultValueInCode(True)
                End If

                ' Deserialize the value
                Dim ValueNode As Linq.XElement = Nothing
                ' First, unless explicitly told to only get runtime values, 
                ' let's check if we have design-time specific values for this guy...
                If Not getRuntimeValue Then ValueNode = SettingNode.<DesignTimeValue>.FirstOrDefault(Function(x) x.@Profile = "(Default)")

                If ValueNode Is Nothing Then
                    ' ...and if we didn't find any design-time specific info, let's check the "normal" value
                    ' element
                    ValueNode = SettingNode.<Value>.FirstOrDefault(Function(x) x.@Profile = "(Default)")
                End If
                If ValueNode IsNot Nothing Then Instance.SetSerializedValue(ValueNode.Value)

            Next SettingNode
            Common.Switches.TraceSDSerializeSettings(TraceLevel.Info, "Deserialized {0} settings", Settings.Count)
        End Sub

        Private Shared Function MakeSetting_Inner(Instance As DesignTimeSettingInstance) As Linq.XElement
            Dim output = <Tmp></Tmp>
            Dim designTimeValue As String = Nothing
            Dim defaultValue As String
            Dim valueSerializer As New SettingsValueSerializer()

            ' If this is a connection string, we have different values at design time and runtim.
            ' We serialize the design time value in the DesignTimeValue node, and add a Value node
            ' that contain the value that's going to be used at runtime...
            If String.Equals(Instance.SettingTypeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                designTimeValue = Instance.SerializedValue
                Dim scs = DirectCast(
                              valueSerializer.Deserialize(
                                  GetType(VSDesigner.VSDesignerPackage.SerializableConnectionString),
                                  designTimeValue, Globalization.CultureInfo.InvariantCulture),
                              VSDesigner.VSDesignerPackage.SerializableConnectionString)

                defaultValue = If(scs IsNot Nothing AndAlso scs.ConnectionString IsNot Nothing, scs.ConnectionString, String.Empty)
            Else
                defaultValue = Instance.SerializedValue
            End If
            ' If we did find a design-time specific value, we better write it out...
            If designTimeValue IsNot Nothing Then
                output.Add(<DesignTimeValue Profile=<%= SettingsDesigner.CultureInvariantDefaultProfileName %>><%= designTimeValue %></DesignTimeValue>)
            End If
            output.Add(<Value Profile=<%= SettingsDesigner.CultureInvariantDefaultProfileName %>><%= defaultValue %></Value>)
            Return output
        End Function

        Private Shared Function MakeSetting(Instance As DesignTimeSettingInstance) As Linq.XElement
            Return <Setting Name=<%= Instance.Name %>
                       Description=<%= If(Instance.Description <> "", Instance.Description, Nothing) %>
                       Provider=<%= If(Instance.Provider <> "", Instance.Provider, Nothing) %>
                       Roaming=<%= If(Instance.Roaming, XmlConvert.ToString(Instance.Roaming), Nothing) %>
                       GenerateDefaultValueInCode=<%= If(Instance.GenerateDefaultValueInCode, Nothing, XmlConvert.ToString(False)) %>
                       Type=<%= Instance.SettingTypeName %>
                       Scope=<%= Instance.Scope.ToString() %>>
                       <%= MakeSetting_Inner(Instance).Descendants %>
                   </Setting>
        End Function

        ''' <summary> Serialize design time settings instance. </summary>
        ''' <param name="Settings">Instance to serialize</param>
        ''' <param name="Writer">Text writer on stream to serialize settings to</param>
        Public Shared Sub Serialize(
                                     Settings As DesignTimeSettings,
                                     GeneratedClassNameSpace As String,
                                     ClassName As String,
                                     Writer As TextWriter,
                                     DeclareEncodingAs As System.Text.Encoding
                                   )

            Common.Switches.TraceSDSerializeSettings(TraceLevel.Info, "Serializing {0} settings", Settings.Count)

            ' Gotta store the namespace here in case it changes from under us!
            Settings.PersistedNamespace = GeneratedClassNameSpace
            DeclareEncodingAs = If(DeclareEncodingAs, System.Text.Encoding.UTF8)

            Dim XML_SettingsFile = <SettingsFile xmls=<%= SettingsSerializer.SettingsSchemaUri %>
                                       CurrentProfile=<%= SettingsDesigner.CultureInvariantDefaultProfileName %>>
                                       GeneratedClassNameSpace=<%= If(Settings.Count > 0, GeneratedClassNameSpace, Nothing) %>
                                       GeneratedClassName=<%= If(Settings.Count > 0, ClassName, Nothing) %>
                                       UseMySettingsClassName=<%= If(Settings.UseSpecialClassName, XmlConvert.ToString(True), Nothing) %>
                                       <Profile>
                                       </Profile>
                                       <Settings>
                                           <%= From d In Settings, i In MakeSetting(d).Elements
                                               Select i %>
                                       </Settings>
                                   </SettingsFile>
            Dim version = 1.0
            Dim proc As New Linq.XProcessingInstruction("?xml", $"verion='{version}' encoding='{DeclareEncodingAs}' ")
            XML_SettingsFile.AddBeforeSelf(proc)
            Dim SettingsWriter As New XmlTextWriter(Writer) With {.Formatting = Formatting.Indented, .Indentation = 2}
            XML_SettingsFile.WriteTo(SettingsWriter)
        End Sub

    End Class

End Namespace
