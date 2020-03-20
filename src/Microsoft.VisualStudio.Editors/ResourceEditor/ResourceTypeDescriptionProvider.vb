' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Provides type information about instances of the Resource class.  This is used to fill the
    '''   properties window in Visual Studio with the property values of particular instances of the
    '''   Resource class.
    ''' </summary>
    ''' <remarks> 
    '''  Resource class is hooked up with this class using TypeDescriptionProviderAttribute. 
    '''  This class inherits TypeDescriptionProvider and only overrides GetTypeDescriptor 
    '''      to return our own ResourceTypeDescriptor.
    ''' </remarks>
    Friend NotInheritable Class ResourceTypeDescriptionProvider
        Inherits TypeDescriptionProvider

        ''' <summary>
        '''  Returns ResourceTypeDescriptor as the ICustomTypeDescriptor for the specified Resource instance.
        ''' </summary>
        ''' <param name="ObjectType">The type of the class to return the type descriptor for. In our case, Resource.</param>
        ''' <param name="Instance">Instance of the class. In our case, a Resource instance.</param>
        ''' <returns>A new ResourceTypeDescriptor for the specified Resource instance.</returns>
        Public Overrides Function GetTypeDescriptor(ObjectType As Type, Instance As Object) As ICustomTypeDescriptor
            If Instance Is Nothing Then
                Return MyBase.GetTypeDescriptor(ObjectType, Instance)
            End If
            Debug.Assert(TypeOf Instance Is Resource, "Instance is not a Resource!!!")

            Return New ResourceTypeDescriptor(CType(Instance, Resource))
        End Function

    End Class

End Namespace

