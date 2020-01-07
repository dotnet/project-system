' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.DesignerFramework
    ''' <summary>
    ''' Handles running the single-file generator for a particular file path.
    ''' This implementation of <see cref="ISingleFileGenerator"/> handles the "cloud"
    ''' scenario, where the Resource Designer is running on a client system but the
    ''' project system is on a server elsewhere.
    ''' </summary>
    Friend Class CloudEnvironmentSingleFileGenerator
        Implements ISingleFileGenerator

        Private ReadOnly _serviceProvider As IServiceProvider
        Private ReadOnly _loader As BaseDesignerLoader

        Public Sub New(serviceProvider As IServiceProvider, loader As BaseDesignerLoader)
            _serviceProvider = serviceProvider
            _loader = loader
        End Sub

        Public Sub Run() Implements ISingleFileGenerator.Run
            ThreadHelper.JoinableTaskFactory.Run(
                Async Function()
                    Dim sfgService As RpcContracts.SingleFileGenerators.ISingleFileGenerator = Nothing

                    Try
                        Dim serviceContainer = _serviceProvider.GetService(Of SVsBrokeredServiceContainer, IBrokeredServiceContainer)
                        Dim serviceBroker = serviceContainer.GetFullAccessServiceBroker()

                        sfgService = Await serviceBroker.GetProxyAsync(Of RpcContracts.SingleFileGenerators.ISingleFileGenerator)(VisualStudioServices.VS2019_5.SingleFileGenerator)

                        ' We know the local (client) path of the file, but we need to convert that to the path on
                        ' the server for the single file generator service.
                        Dim localPath As String = _loader.Moniker
                        Dim serverPath As String = RemotePathService.TranslateToSharedPathIfNecessary(localPath, isDirectory:=False)

                        Await sfgService.GenerateCodeAsync(serverPath, String.Empty, CancellationToken.None)
                    Finally
                        TryCast(sfgService, IDisposable)?.Dispose()
                    End Try

                End Function)
        End Sub
    End Class
End Namespace

