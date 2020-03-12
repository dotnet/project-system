' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace System.ComponentModel

    ''' <summary>
    '''     A <see cref="TypeDescriptionProvider"/> that allows you swap out an abstract class with a concrete 
    '''     implementation, so that derived types can be opened And designed in the editor.
    ''' </summary>
    Friend Class AbstractControlTypeDescriptionProvider(Of TAbstract, TDerived)
        Inherits TypeDescriptionProvider

        ' Based on Brian Pepin's post: http://www.pocketsilicon.com/post/Using-Visual-Studio-Whidbey-to-Design-Abstract-Forms

        Public Sub New()

            MyBase.New(TypeDescriptor.GetProvider(GetType(TAbstract)))

        End Sub

        Public Overrides Function GetReflectionType(objectType As Type, instance As Object) As Type

            ' If the designer Is asking for the abstract control,
            ' return the "concrete" version of it instead
            If objectType = GetType(TAbstract) Then
                Return GetType(TDerived)
            End If

            Return MyBase.GetReflectionType(objectType, instance)

        End Function

        Public Overrides Function CreateInstance(provider As IServiceProvider, objectType As Type, argTypes As Type(), args As Object()) As Object

            ' If the designer Is asking to create the abstract 
            ' control, instantiate the "concrete" version of it instead
            If objectType = GetType(TAbstract) Then
                objectType = GetType(TDerived)
            End If

            Return MyBase.CreateInstance(provider, objectType, argTypes, args)

        End Function

    End Class

End Namespace

