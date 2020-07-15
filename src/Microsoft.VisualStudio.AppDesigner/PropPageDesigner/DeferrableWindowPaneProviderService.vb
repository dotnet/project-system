' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design

Imports Microsoft.VisualStudio.Shell.Design

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' The only purpose of this class is to allow us to create a window pane
    '''   specific to the property page designer (PropPageDesignerWindowPane)
    '''   instead of the one that the base one provides.
    ''' This allows us to have more control of the WindowPane.
    ''' </summary>
    Public NotInheritable Class DeferrableWindowPaneProviderService
        Inherits AppDesDesignerFramework.DeferrableWindowPaneProviderServiceBase

        ''' <summary>
        ''' Creates a new DeferrableWindowPaneProviderService.  This service is used by the shell
        '''   to create a PropPageDesignerWindowPane when needed (i.e., its creation is deferred).
        ''' </summary>
        ''' <param name="provider"></param>
        Public Sub New(provider As IServiceProvider)
            MyBase.New(provider, Nothing)
        End Sub

        ''' <summary>
        ''' We override this so that we create a window pane specific to the 
        '''   property page designer.
        ''' </summary>
        ''' <param name="surface"></param>
        Public Overrides Function CreateWindowPane(surface As DesignSurface) As DesignerWindowPane
            Return New PropPageDesignerWindowPane(surface)
        End Function

    End Class

End Namespace
