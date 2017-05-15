' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Settings

Namespace Microsoft.VisualStudio.Editors.OptionPages
    Public NotInheritable Class GeneralOptions
        <Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")>
        Private Class SVsSettingsPersistenceManager
        End Class

        Private Const FastUpToDateSettingKey As String = "NETCoreProjectSystem\FastUpToDateCheck"
        Private Const VerboseLoggingKey As String = "NETCoreProjectSystem\VerboseLogging"

        Private ReadOnly _settingsManager As ISettingsManager

        Public Property FastUpToDateCheck As Boolean
            Get
                Return If(_settingsManager?.GetValueOrDefault(FastUpToDateSettingKey, False), False)
            End Get
            Set
                _settingsManager.SetValueAsync(FastUpToDateSettingKey, Value, isMachineLocal:=False)
            End Set
        End Property

        Public Property VerboseLogging As Boolean
            Get
                Return If(_settingsManager?.GetValueOrDefault(VerboseLoggingKey, False), False)
            End Get
            Set
                _settingsManager.SetValueAsync(VerboseLoggingKey, Value, isMachineLocal:=False)
            End Set
        End Property

        Public Sub New(serviceProvider As IServiceProvider)
            _settingsManager = CType(serviceProvider.GetService(GetType(SVsSettingsPersistenceManager)), ISettingsManager)
        End Sub
    End Class
End Namespace
