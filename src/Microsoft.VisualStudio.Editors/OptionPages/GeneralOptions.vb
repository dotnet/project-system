' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Settings
Imports Microsoft.VisualStudio.Shell.Settings

Public NotInheritable Class GeneralOptions
    Private ReadOnly _settingsManager As SettingsManager

    Public Property FastUpToDateCheck As Boolean
        Get
            Dim settingsStore = _settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings)
            Return settingsStore.GetBoolean("General", "NETCoreFastUpToDateCheck")
        End Get
        Set(value As Boolean)
            Dim settingsStore = _settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings)
            settingsStore.SetBoolean("General", "NETCoreFastUpToDateCheck", value)
        End Set
    End Property

    Public Sub New(serviceProvider As IServiceProvider)
        _settingsManager = New ShellSettingsManager(serviceProvider)
    End Sub
End Class
