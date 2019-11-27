' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' Handles running the single-file generator for a particular <see cref="EnvDTE.ProjectItem"/>.
    ''' This implementation of <see cref="ISingleFileGenerator"/> handles the "local" scenario,
    ''' where the Resource Designer is running in the same process as the project system.
    ''' </summary>
    Friend Class LocalSingleFileGenerator
        Implements ISingleFileGenerator

        Private ReadOnly _serviceProvider As IServiceProvider

        Public Sub New(serviceProvider As IServiceProvider)
            _serviceProvider = serviceProvider
        End Sub

        ''' <summary>
        ''' Runs the single-file generator.
        ''' </summary>
        ''' <remarks>
        ''' We retrieve the <see cref="EnvDTE.ProjectItem"/> from the <see cref="IServiceProvider"/>
        ''' provided in the constructor. We depend on the IServiceProvider being updated if/when the
        ''' project item associated with the designer changes.
        ''' </remarks>
        Public Sub Run() Implements ISingleFileGenerator.Run
            Dim projItem = TryCast(_serviceProvider.GetService(GetType(EnvDTE.ProjectItem)), EnvDTE.ProjectItem)

            If projItem IsNot Nothing Then
                Dim vsProj As VSLangProj.VSProjectItem = TryCast(projItem.Object, VSLangProj.VSProjectItem)
                If vsProj IsNot Nothing Then
                    vsProj.RunCustomTool()
                End If
            End If
        End Sub

    End Class

End Namespace

