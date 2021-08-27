' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A special subclass of PropertyControlData that changes the behavior in the following manner:
    '''   a) If simplified configurations mode is on ("Show Advanced Build Configurations" property from
    '''     Tools.Options.Projects and Solutions" is off, and the project still has only the original
    '''     Debug and Release configurations, with no configurations having been changed, added or deleted),
    '''     then the property will only display its value from a single configuration (either Debug or
    '''     Release, as specified in the constructor), and changing that property will only affect that
    '''     single configuration.  Even if the property's value is different in the two configurations, it
    '''     will not show an indeterminate value.
    '''   b) If simplified configurations mode is not on, then we revert to default behavior (including showing
    '''     indeterminate state if multiple objects are selected and their values differ).
    ''' 
    ''' This subclass is used for certain properties that have a "natural" value in debug that differs from
    '''     the default used in Release.  If we did not use this subclass for these properties, the default
    '''     display would be indeterminate for these properties in all SKUs that use simplified configurations
    '''     (e.g., VB, C# Express, etc.).
    ''' 
    ''' </summary>
    Friend Class SingleConfigPropertyControlData
        Inherits PropertyControlData

        Private Const ReleaseConfigName As String = "Release"
        Private Const DebugConfigName As String = "Debug"

        Public Enum Configs
            Debug
            Release
        End Enum

        Private ReadOnly _specificConfigName As String

        ' Constructor.  Same as PropertyControlData, except for Config.
        '   Argument Config: The configuration to show the property's value from, and to affect when saving, if the project is
        '     in simplified configurations mode.

        Public Sub New(Config As Configs, id As Integer, name As String, control As Control, flags As ControlDataFlags)
            Me.New(Config, id, name, control, Nothing, Nothing, flags)
        End Sub

        Public Sub New(Config As Configs, id As Integer, name As String, control As Control)
            Me.New(Config, id, name, control, Nothing, Nothing, ControlDataFlags.None)
        End Sub

        Public Sub New(Config As Configs, id As Integer, name As String, control As Control, setter As SetDelegate, getter As GetDelegate, flags As ControlDataFlags)
            Me.New(Config, id, name, control, setter, getter, flags, Nothing)
        End Sub

        Public Sub New(Config As Configs, id As Integer, name As String, control As Control, AssocControls As Control())
            Me.New(Config, id, name, control, Nothing, Nothing, ControlDataFlags.None, AssocControls)
        End Sub

        Public Sub New(Config As Configs, id As Integer, name As String, control As Control, setter As SetDelegate, getter As GetDelegate)
            Me.New(Config, id, name, control, setter, getter, ControlDataFlags.None, Nothing)
        End Sub

        Public Sub New(Config As Configs, id As Integer, name As String, control As Control, setter As SetDelegate, getter As GetDelegate, flags As ControlDataFlags, AssocControls As Control())
            MyBase.New(id, name, control, setter, getter, flags, AssocControls)
            Select Case Config
                Case Configs.Debug
                    _specificConfigName = DebugConfigName
                Case Configs.Release
                    _specificConfigName = ReleaseConfigName
                Case Else
                    Debug.Fail("Unrecognized Configs enum value")
                    _specificConfigName = ""
            End Select

            Debug.Assert(Not IsUserPersisted, "SingleConfigPropertyControlData - Can't be used for user-persisted properties")
            Debug.Assert(Not IsCommonProperty, "SingleConfigPropertyControlData - Can't be used for common properties")
        End Sub

        ''' <summary>
        ''' Returns the raw set of objects in use by this property page.  This will generally be the set of objects
        '''   passed in to the page through SetObjects.  However, it may be modified by subclasses to contain a superset
        '''   or subset for special purposes.
        ''' </summary>
        Public Overrides ReadOnly Property RawPropertiesObjects As Object()
            Get
                Dim AllObjects As Object() = MyBase.RawPropertiesObjects
                Dim SpecificConfigIndex As Integer = IndexOfSpecificConfig(AllObjects)
                If SpecificConfigIndex >= 0 Then
                    'Specific single configuration found and should be used - return an array with only that entry
                    '  This means that the property page will display and set only the values from this specific configuration
                    '  for this property.
                    Return New Object() {AllObjects(SpecificConfigIndex)}
                Else
                    'Use default behavior (all configurations)
                    Return AllObjects
                End If
            End Get
        End Property

        ''' <summary>
        ''' Returns the extended objects created from the raw set of objects in use by this property page.  This will generally be 
        '''   based on the set of objects passed in to the page through SetObjects.  However, it may be modified by subclasses to 
        '''   contain a superset or subset for special purposes.
        ''' </summary>
        Public Overrides ReadOnly Property ExtendedPropertiesObjects As Object()
            Get
                'We must pass the raw setobjects array to IndexOfSpecificConfig - it won't work with the extended objects
                '  because they will not be IVsCfg objects - but we return an array based on the extended objects.
                '  These two arrays correspond to each other and must be the same size.
                Debug.Assert(MyBase.RawPropertiesObjects.Length = MyBase.ExtendedPropertiesObjects.Length)

                Dim SpecificConfigIndex As Integer = IndexOfSpecificConfig(MyBase.RawPropertiesObjects) 'Note: using MyBase implementation, not the overridden implementation that might return only a subset
                If SpecificConfigIndex >= 0 Then
                    'Specific single configuration found and should be used - return an array with only that entry.
                    '  This means that the property page will display and set only the values from this specific configuration
                    '  for this property.
                    Return New Object() {MyBase.ExtendedPropertiesObjects(SpecificConfigIndex)}
                Else
                    'Use default behavior (all configurations)
                    Return MyBase.ExtendedPropertiesObjects
                End If
            End Get
        End Property

        ''' <summary>
        ''' Given a set of objects (raw or extended), determine if we should be displaying the values
        '''   from all of them (the normal, default behavior for properties), or from only a single
        '''   one.  If we should be displaying/changing the values from only one of these objects,
        '''   return the index of that object in the array, otherwise return -1.
        ''' </summary>
        ''' <param name="Objects">The array of raw or extended objects (configurations) to inspect.</param>
        ''' <returns>The index into the array of the specific single configuration to use, else -1 to use all of them (default behavior).</returns>
        ''' <remarks>
        ''' If we are not in simplified configuration mode for this project, this function will return -1 to indicate all values of the property
        '''   should be considered.
        ''' </remarks>
        Private Function IndexOfSpecificConfig(Objects() As Object) As Integer
            Debug.Assert(_specificConfigName IsNot Nothing)
            If _specificConfigName = "" Then
                Return -1
            End If

            If Objects Is Nothing OrElse Objects.Length <> 2 Then 'perf optimization
                'We can't be in advanced configuration mode unless the number of configuration is exactly
                '  two (because the project will start returning False for CanHideConfigurationsForThisProject.
                Debug.Assert(Not SimplifiedConfigsMode())
                Return -1
            End If

            If Not SimplifiedConfigsMode() Then
                'We only want the single-config behavior when we're in simplified configs mode
                Return -1
            End If

            'Look for a configuration whose name matches that of the configuration we are special-casing
            Dim Index As Integer = 0
            For Each Obj As Object In Objects
                Debug.Assert(Obj IsNot Nothing, "Why was Nothing passed in as a config object?")
                Dim Config As IVsCfg = TryCast(Obj, IVsCfg)
                Debug.Assert(Config IsNot Nothing, "Object was not IVsCfg")
                If Config IsNot Nothing Then
                    Dim ConfigName As String = Nothing
                    Dim PlatformName As String = Nothing
                    ShellUtil.GetConfigAndPlatformFromIVsCfg(Config, ConfigName, PlatformName)
                    If _specificConfigName IsNot Nothing AndAlso _specificConfigName.Equals(ConfigName, StringComparison.CurrentCultureIgnoreCase) Then
                        'Found it - return the index to it
                        Return Index
                    End If
                End If
                Index += 1
            Next

            Return -1
        End Function

        ''' <summary>
        ''' Returns true iff this project is in simplified configuration mode.
        ''' </summary>
        Private Function SimplifiedConfigsMode() As Boolean
            Return ShellUtil.GetIsSimplifiedConfigMode(PropPage.ProjectHierarchy)
        End Function

    End Class

End Namespace
