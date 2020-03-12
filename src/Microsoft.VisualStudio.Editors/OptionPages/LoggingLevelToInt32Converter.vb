' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Globalization
Imports System.Windows.Data

Namespace Microsoft.VisualStudio.Editors.OptionPages
    Friend NotInheritable Class LoggingLevelToInt32Converter
        Implements IValueConverter

        Public Shared ReadOnly Property Instance As LoggingLevelToInt32Converter = New LoggingLevelToInt32Converter()

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Debug.Assert(value IsNot Nothing)
            Debug.Assert(TypeOf value Is LogLevel)
            Debug.Assert([Enum].IsDefined(GetType(LogLevel), value))
            Debug.Assert(targetType = GetType(Integer))

            Return CInt(CType(value, LogLevel))
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Debug.Assert(value IsNot Nothing)
            Debug.Assert(TypeOf value Is Integer)
            Debug.Assert(CInt(value) >= 0 AndAlso CInt(value) < [Enum].GetValues(GetType(LogLevel)).Length)
            Debug.Assert(targetType = GetType(LogLevel))

            Return CType(CInt(value), LogLevel)
        End Function
    End Class
End Namespace
