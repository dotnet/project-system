' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Shell.Design


Namespace Microsoft.VisualStudio.Editors.PropPageDesigner


    ''' <summary>
    ''' The only purpose of this class is to allow us to create a window pane
    '''   specific to the property page designer (PropPageDesignerWindowPane)
    '''   instead of the one that the base one provides.
    ''' This allows us to have more control of the WindowPane.
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class DeferrableWindowPaneProviderService
        Inherits AppDesDesignerFramework.DeferrableWindowPaneProviderServiceBase

        ''' <summary>
        ''' Creates a new DeferrableWindowPaneProviderService.  This service is used by the shell
        '''   to create a PropPageDesignerWindowPane when needed (i.e., its creation is deferred).
        ''' </summary>
        ''' <param name="provider"></param>
        ''' <remarks></remarks>
        Public Sub New(provider As IServiceProvider)
            MyBase.New(provider, Nothing)
        End Sub


        ''' <summary>
        ''' We override this so that we create a window pane specific to the 
        '''   property page designer.
        ''' </summary>
        ''' <param name="surface"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function CreateWindowPane(surface As DesignSurface) As DesignerWindowPane
            Return New PropPageDesignerWindowPane(surface)
        End Function

    End Class

End Namespace
