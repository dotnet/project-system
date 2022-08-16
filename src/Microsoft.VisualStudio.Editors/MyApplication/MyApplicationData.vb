' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.MyApplication

    <ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
    Friend NotInheritable Class MyApplicationData

        Public Sub New()
            'Set defaults that don't match empty constructor
            EnableVisualStyles = True

            'Default authentication mode to "Windows"
            AuthenticationMode = ApplicationServices.AuthenticationMode.Windows

            _saveMySettingsOnExit = True
        End Sub

        Private _mySubMain As Boolean 'True indicates My.MyApplication will be StartupObject
        Private _mainFormNoRootNS As String 'Form for My.MyApplication to instantiate for main window (without the root namespace)
        Private _singleInstance As Boolean
        Private _shutdownMode As Integer
        Private _enableVisualStyles As Boolean
        Private _authenticationMode As Integer
        Private _splashScreenNoRootNS As String 'Splash screen to use (without the root namespace)
        Private _saveMySettingsOnExit As Boolean 'Whether to save My.Settings on shutdown
        Private _highDpiMode As Integer

        Private _dirty As Boolean

        Public Property IsDirty As Boolean
            Get
                Return _dirty
            End Get
            Set
                _dirty = value
            End Set
        End Property

        Public Property MySubMain As Boolean 'True indicates My.MyApplication will be StartupObject
            Get
                Return _mySubMain
            End Get
            Set
                _mySubMain = value
                IsDirty = True
            End Set
        End Property

        Public Property MainFormNoRootNS As String 'Form for My.MyApplication to instantiate for main window (not including the root namespace)
            Get
                Return _mainFormNoRootNS
            End Get
            Set
                _mainFormNoRootNS = value
                IsDirty = True
            End Set
        End Property

        Public Property SingleInstance As Boolean
            Get
                Return _singleInstance
            End Get
            Set
                _singleInstance = value
                IsDirty = True
            End Set
        End Property

        Public Property ShutdownMode As Integer
            Get
                Return _shutdownMode
            End Get
            Set
                _shutdownMode = value
                IsDirty = True
            End Set
        End Property

        Public Property EnableVisualStyles As Boolean
            Get
                Return _enableVisualStyles
            End Get
            Set
                _enableVisualStyles = value
                IsDirty = True
            End Set
        End Property

        Public Property AuthenticationMode As Integer
            Get
                Return _authenticationMode
            End Get
            Set
                _authenticationMode = value
                IsDirty = True
            End Set
        End Property

        Public Property SplashScreenNoRootNS As String
            Get
                Return _splashScreenNoRootNS
            End Get
            Set
                _splashScreenNoRootNS = value
                IsDirty = True
            End Set
        End Property

        Public Property SaveMySettingsOnExit As Boolean
            Get
                Return _saveMySettingsOnExit
            End Get
            Set
                _saveMySettingsOnExit = value
                IsDirty = True
            End Set
        End Property

        Public Property HighDpiMode As Integer
            Get
                Return _highDpiMode
            End Get
            Set
                _highDpiMode = Value
                IsDirty = True
            End Set
        End Property

    End Class

End Namespace
