' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Settings

Namespace Microsoft.VisualStudio.Editors.OptionPages
    Public NotInheritable Class GeneralOptions
        <Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")>
        Private Class SVsSettingsPersistenceManager
        End Class

        Private Const FastUpToDateEnabledSettingKey As String = "ManagedProjectSystem\FastUpToDateCheckEnabled"
        Private Const FastUpToDateLogLevelSettingKey As String = "ManagedProjectSystem\FastUpToDateLogLevel"

        Private ReadOnly _settingsManager As ISettingsManager

        Public Property FastUpToDateCheckEnabled As Boolean
            Get
                Return If(_settingsManager?.GetValueOrDefault(FastUpToDateEnabledSettingKey, False), False)
            End Get
            Set
                _settingsManager.SetValueAsync(FastUpToDateEnabledSettingKey, Value, isMachineLocal:=False)
            End Set
        End Property

        Public Property FastUpToDateLogLevel As LogLevel
            Get
                Return If(_settingsManager?.GetValueOrDefault(FastUpToDateLogLevelSettingKey, LogLevel.None), LogLevel.None)
            End Get
            Set
                _settingsManager.SetValueAsync(FastUpToDateLogLevelSettingKey, Value, isMachineLocal:=False)
            End Set
        End Property

        Public Sub New(serviceProvider As IServiceProvider)
            _settingsManager = CType(serviceProvider.GetService(GetType(SVsSettingsPersistenceManager)), ISettingsManager)
        End Sub
    End Class
End Namespace
