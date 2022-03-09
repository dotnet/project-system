' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.OptionPages
    ''' <summary>
    ''' Holds the data backing the Tools | Options | Projects and Solutions | SDK-Style Projects page.
    ''' </summary>
    Friend Class SDKStyleProjectOptionsData
        Implements INotifyPropertyChanged

        Public Shared ReadOnly Property MainInstance As SDKStyleProjectOptionsData = New SDKStyleProjectOptionsData

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Private _fastUpToDateCheckEnabled As Boolean = True
        Private _fastUpToDateCheckLogLevel As LogLevel = LogLevel.None
        Private _nestingIgnoreSolutionAndProjectProfiles As Boolean
        Private _preferSingleTargetBuildsOnLaunch As Boolean = True

        Public Function Clone() As SDKStyleProjectOptionsData
            Dim clonedData = New SDKStyleProjectOptionsData
            clonedData.CopyFrom(Me)
            Return clonedData
        End Function

        Public Sub CopyFrom(source As SDKStyleProjectOptionsData)
            FastUpToDateCheckEnabled = source.FastUpToDateCheckEnabled
            FastUpToDateCheckLogLevel = source.FastUpToDateCheckLogLevel
            NestingIgnoreSolutionAndProjectProfiles = source.NestingIgnoreSolutionAndProjectProfiles
            PreferSingleTargetBuildsOnLaunch = source.PreferSingleTargetBuildsOnLaunch
        End Sub

        <SharedSettings("ManagedProjectSystem\FastUpToDateCheckEnabled", False)>
        Public Property FastUpToDateCheckEnabled As Boolean
            Get
                Return _fastUpToDateCheckEnabled
            End Get
            Set
                If value = _fastUpToDateCheckEnabled Then
                    Return
                End If

                _fastUpToDateCheckEnabled = value
                SendPropertyChangedNotification()
            End Set
        End Property

        <SharedSettings("Cps.NestingIgnoreSolutionAndProjectProfiles", False)>
        Public Property NestingIgnoreSolutionAndProjectProfiles As Boolean
            Get
                Return _nestingIgnoreSolutionAndProjectProfiles
            End Get
            Set
                If Value = _nestingIgnoreSolutionAndProjectProfiles Then
                    Return
                End If

                _nestingIgnoreSolutionAndProjectProfiles = Value
                SendPropertyChangedNotification()
            End Set
        End Property

        <SharedSettings("ManagedProjectSystem\FastUpToDateLogLevel", False)>
        Public Property FastUpToDateCheckLogLevel As LogLevel
            Get
                Return _fastUpToDateCheckLogLevel
            End Get
            Set
                If value = _fastUpToDateCheckLogLevel Then
                    Return
                End If

                _fastUpToDateCheckLogLevel = value
                SendPropertyChangedNotification()
            End Set
        End Property

        <SharedSettings("ManagedProjectSystem\PreferSingleTargetBuilds", False)>
        Public Property PreferSingleTargetBuildsOnLaunch As Boolean
            Get
                Return _preferSingleTargetBuildsOnLaunch
            End Get
            Set
                If Value = _preferSingleTargetBuildsOnLaunch Then
                    Return
                End If

                _preferSingleTargetBuildsOnLaunch = Value
                SendPropertyChangedNotification()
            End Set
        End Property

        Private Sub SendPropertyChangedNotification(<CallerMemberName> Optional callingMember As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(callingMember))
        End Sub
    End Class
End Namespace

