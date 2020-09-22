' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' The root component for the settings designer.
    ''' The DesignTimeSettings class is a list of all the settings that are currently
    ''' available in a .Settings file.
    ''' </summary>
    <
    Designer(GetType(SettingsDesigner), GetType(Design.IRootDesigner)),
    DesignerCategory("Designer")
    >
    Friend NotInheritable Class DesignTimeSettings
        Inherits Component
        Implements IEnumerable(Of DesignTimeSettingInstance)

        ' The namespace used the last time this instance was serialized
        Private _persistedNamespace As String

        ''' <summary>
        ''' We may want to special-case handling of the generated class name to avoid
        ''' name clashes with updated projects...
        ''' </summary>
        Private _useSpecialClassName As Boolean

        Private ReadOnly _settings As New List(Of DesignTimeSettingInstance)(16)

        Private Function IEnumerableOfDesignTimeSettingInstance_GetEnumerator() As IEnumerator(Of DesignTimeSettingInstance) Implements IEnumerable(Of DesignTimeSettingInstance).GetEnumerator
            Return _settings.GetEnumerator()
        End Function

        Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Return IEnumerableOfDesignTimeSettingInstance_GetEnumerator()
        End Function

        <Browsable(False)>
        Public ReadOnly Property Count As Integer
            Get
                Return _settings.Count
            End Get
        End Property

        ''' <summary>
        ''' Is the UseMySettingsClassName flag set in the underlying .settings file?
        ''' If so, we may want to special-case the class name...
        ''' </summary>
        Friend Property UseSpecialClassName As Boolean
            Get
                Return _useSpecialClassName
            End Get
            Set
                _useSpecialClassName = value
            End Set
        End Property

        ''' <summary>
        ''' The namespace as persisted in the .settings file
        ''' </summary>
        ''' <remarks>May return NULL if no namespace was persisted!!!</remarks>
        Friend Property PersistedNamespace As String
            Get
                Return _persistedNamespace
            End Get
            Set
                _persistedNamespace = value
            End Set
        End Property

#Region "Valid/unique name handling"

        Private ReadOnly Property CodeProvider As CodeDom.Compiler.CodeDomProvider
            Get
                Dim codeProviderInstance As CodeDom.Compiler.CodeDomProvider = Nothing

                Dim mdCodeDomProvider As Designer.Interfaces.IVSMDCodeDomProvider = TryCast(GetService(GetType(Designer.Interfaces.IVSMDCodeDomProvider)),
                                            Designer.Interfaces.IVSMDCodeDomProvider)
                If mdCodeDomProvider IsNot Nothing Then
                    Try
                        codeProviderInstance = TryCast(mdCodeDomProvider.CodeDomProvider, CodeDom.Compiler.CodeDomProvider)
                    Catch ex As System.Runtime.InteropServices.COMException
                        ' Some project systems (i.e. C++) throws if you try to get the CodeDomProvider
                        ' property :(
                    End Try
                End If
                Return codeProviderInstance
            End Get
        End Property
        ''' <summary>
        ''' Is this a valid name for a setting in this collection?
        ''' </summary>
        ''' <param name="Name">Name to test</param>
        ''' <param name="instanceToIgnore">If we want to rename an existing setting, we want to that particular it from the unique name check</param>
        Friend Function IsValidName(Name As String, Optional checkForUniqueness As Boolean = False, Optional instanceToIgnore As DesignTimeSettingInstance = Nothing) As Boolean
            Return IsValidIdentifier(Name) AndAlso (Not checkForUniqueness OrElse IsUniqueName(Name, instanceToIgnore))
        End Function

        ''' <summary>
        ''' Is this a unique name 
        ''' </summary>
        ''' <param name="Name"></param>
        ''' <param name="IgnoreThisInstance"></param>
        Friend Function IsUniqueName(Name As String, Optional IgnoreThisInstance As DesignTimeSettingInstance = Nothing) As Boolean
            ' Empty name not considered unique!
            If Name = "" Then
                Return False
            End If

            For Each ExistingInstance As DesignTimeSettingInstance In Me
                If EqualIdentifiers(Name, ExistingInstance.Name) Then
                    If ExistingInstance IsNot IgnoreThisInstance Then
                        Return False
                    End If
                End If
            Next

            ' Since this component is also added to the designer host, we have to check this as well...
            '
            ' This *shouldn't* happen, so we assert here
            If Site IsNot Nothing Then
                If EqualIdentifiers(Site.Name, Name) Then
                    Debug.Fail("Why is the setting name equal to the DesignTimeSettings site name?")
                    Return False
                End If
            End If
            Return True
        End Function

        ''' <summary>
        ''' Is this a valid identifier for us to use?
        ''' </summary>
        ''' <param name="Name"></param>
        ''' <remarks>
        ''' We are more strict than the language specific code provider (if any) since the language specific code provider
        ''' may allow escaped identifiers. 
        ''' We need to know the un-escaped identifier ('cause that is what's going in to the app.config file), so we can't
        ''' allow that...
        ''' </remarks>
        Private Function IsValidIdentifier(Name As String) As Boolean
            If Name Is Nothing Then
                Return False
            End If

            If CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(Name) Then
                If CodeProvider IsNot Nothing Then
                    Return CodeProvider.IsValidIdentifier(Name)
                Else
                    Return True
                End If
            End If
            Return False
        End Function

        ''' <summary>
        ''' Determine if two identifiers are equal or not. We don't allow to identifiers to differ only in name
        ''' since we use the same name for the component as we do for the setting name, and the name of the component
        ''' is case insensitive... (adding two components with a site name that only differs in casing will cause the
        ''' DesignerHost to throw - this is consistent with how the windows forms designer handles component names)
        ''' </summary>
        ''' <param name="Id1"></param>
        ''' <param name="Id2"></param>
        Friend Shared Function EqualIdentifiers(Id1 As String, Id2 As String) As Boolean
            Return StringComparers.SettingNames.Equals(Id1, Id2)
        End Function

        Friend Function CreateUniqueName(Optional Base As String = Nothing) As String
            If String.IsNullOrEmpty(Base) Then
                Base = My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_DefaultSettingName
            End If

            Dim ExistingNames As New Hashtable(StringComparers.SettingNames)
            For Each Instance As DesignTimeSettingInstance In _settings
                ExistingNames.Item(Instance.Name) = Nothing
            Next

            Dim SuggestedName As String = MakeValidIdentifier(Base)
            If Not ExistingNames.ContainsKey(SuggestedName) Then
                Return SuggestedName
            End If

            For i As Integer = 1 To _settings.Count + 1
                SuggestedName = MakeValidIdentifier(Base & i.ToString())
                If Not ExistingNames.ContainsKey(SuggestedName) Then
                    Return SuggestedName
                End If
            Next
            Debug.Fail("You should never reach this line of code!")
            Return ""
        End Function

        Private Function MakeValidIdentifier(name As String) As String
            If CodeProvider IsNot Nothing AndAlso Not IsValidIdentifier(name) Then
                Return CodeProvider.CreateValidIdentifier(name)
            Else
                Return name
            End If
        End Function

#End Region

#Region "Adding/removing settings"
        ''' <summary>
        ''' Add a new setting instance
        ''' </summary>
        ''' <param name="TypeName"></param>
        ''' <param name="SettingName"></param>
        ''' <param name="AllowMakeUnique">If true, we are allowed to change the name in order to make this setting unique</param>
        Friend Function AddNew(TypeName As String, SettingName As String, AllowMakeUnique As Boolean) As DesignTimeSettingInstance
            Dim Instance As New DesignTimeSettingInstance
            Instance.SetSettingTypeName(TypeName)
            If Not IsUniqueName(SettingName) Then
                If Not AllowMakeUnique Then
                    Debug.Fail("Can't add two settings with the same name")
                    Throw Common.CreateArgumentException(NameOf(AllowMakeUnique))
                Else
                    If SettingName = "" Then
                        SettingName = CreateUniqueName()
                    Else
                        SettingName = CreateUniqueName(SettingName)
                    End If
                End If
            End If

            Instance.SetName(SettingName)
            Add(Instance)
            Return Instance
        End Function

        ''' <summary>
        ''' Add a settings instance to our list of components
        ''' </summary>
        ''' <param name="Instance"></param>
        ''' <param name="MakeNameUnique"></param>
        Friend Sub Add(Instance As DesignTimeSettingInstance, Optional MakeNameUnique As Boolean = False)
            If Contains(Instance) Then
                Return
            End If

            If Not IsUniqueName(Instance.Name) Then
                If MakeNameUnique Then
                    Instance.SetName(CreateUniqueName(Instance.Name))
                Else
                    Throw New ArgumentException()
                End If
            End If

            If Not IsValidIdentifier(Instance.Name) Then
                Throw New ArgumentException()
            End If

            _settings.Add(Instance)
            If Site IsNot Nothing AndAlso Site.Container IsNot Nothing Then
                ' Let's make sure we have this instance in "our" container (if any)
                If Instance.Site Is Nothing OrElse Site.Container IsNot Instance.Site.Container Then
                    Static uniqueNumber As Integer
                    uniqueNumber += 1
                    Dim newName As String = "Setting" & uniqueNumber.ToString()
                    Site.Container.Add(Instance, newName)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Do we already contain this instance? 
        ''' </summary>
        ''' <param name="instance"></param>
        ''' <remarks>
        ''' Useful to prevent adding the same setting multiple times
        ''' </remarks>
        Friend Function Contains(instance As DesignTimeSettingInstance) As Boolean
            Return _settings.Contains(instance)
        End Function

        ''' <summary>
        ''' Remove a setting from our list of components...
        ''' </summary>
        ''' <param name="instance"></param>
        Friend Sub Remove(instance As DesignTimeSettingInstance)
            ' If the instance is site:ed, and it's containers components contains the instance, we better remove it...
            ' ...but only if our m_settings collection contains this instance...
            '
            ' Removing an instance from the site's container will fire a component removed event,
            ' which in turn will make us try and remove the item again. By removing the item from
            ' our internal collection and guarding against doing this multiple times, we avoid the
            ' nasty stack overflow...
            If _settings.Remove(instance) AndAlso
                instance.Site IsNot Nothing AndAlso
                instance.Site.Container IsNot Nothing _
            Then
                instance.Site.Container.Remove(instance)
            End If

        End Sub
#End Region

    End Class

End Namespace
