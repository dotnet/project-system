' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.Serialization

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' An exception that we throw internally in some situations when the project is unloaded
    '''   because of a programmatic action that we take (e.g., checking out the project file,
    '''   setting the target framework property).
    ''' </summary>
    ''' <remarks>
    ''' This exception should not be allowed to bubble up to the user.
    ''' </remarks>
    <Serializable>
    Public Class ProjectReloadedException
        Inherits Exception

        Public Sub New()
            MyBase.New(My.Resources.Designer.PPG_ProjectReloadedSomePropertiesMayNotHaveBeenSet)
        End Sub

        ''' <summary>
        ''' Deserialization constructor.  Required for serialization/remotability support
        '''   (not that we expect this to be needed).
        ''' </summary>
        ''' <param name="Info"></param>
        ''' <param name="Context"></param>
        ''' <remarks>
        '''See .NET Framework Developer's Guide, "Custom Serialization" for more information
        ''' </remarks>
        Protected Sub New(Info As SerializationInfo, Context As StreamingContext)
            MyBase.New(Info, Context)
        End Sub

    End Class

End Namespace
