' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class)>
    Friend NotInheritable Class ProvideRefactorNotifyAttribute
        Inherits RegistrationAttribute

        Private ReadOnly _factoryType As Type
        Private ReadOnly _extension As String
        Private ReadOnly _projectGuid As String

        Public Sub New(factoryType As Type, extension As String, projectGuid As String)
            _factoryType = factoryType
            _extension = extension
            _projectGuid = projectGuid
        End Sub

        Public Overrides Sub Register(context As RegistrationContext)

            Using projectKey As Key = context.CreateKey($"Projects\{{{_projectGuid}}}\FileExtensions\{_extension}\RefactorNotify")
                projectKey.SetValue(String.Empty, _factoryType.GUID.ToString("B"))
            End Using

        End Sub

        Public Overrides Sub Unregister(context As RegistrationContext)
            Throw New NotImplementedException()
        End Sub
    End Class

End Namespace
