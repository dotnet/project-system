// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable disable

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    // Copied from CPS Converters.cs

    internal static partial class Converters
    {
        /// <summary>
        /// Expresses a one or two-way value converter using one or two lambda expressions, respectively.
        /// </summary>
        /// <typeparam name="TFrom1">The first source type, to convert from.</typeparam>
        /// <typeparam name="TFrom2">The second source type, to convert from.</typeparam>
        /// <typeparam name="TTo">The destination type, to convert to.</typeparam>
        private sealed class LambdaMultiConverter<TFrom1, TFrom2, TTo> : IMultiValueConverter
        {
            private readonly Func<TFrom1, TFrom2, TTo> _convert;
            private readonly Func<TTo, (TFrom1, TFrom2)> _convertBack;

            public LambdaMultiConverter(Func<TFrom1, TFrom2, TTo> convert, Func<TTo, (TFrom1, TFrom2)> convertBack = null)
            {
                _convert = convert;
                _convertBack = convertBack;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length == 2 && TryConvert(values[0], out TFrom1 t1) && TryConvert(values[1], out TFrom2 t2))
                {
                    return _convert(t1, t2);
                }

                return DependencyProperty.UnsetValue;

                static bool TryConvert<T>(object o, out T t)
                {
                    if (o is T tt)
                    {
                        t = tt;
                        return true;
                    }

                    t = default;
                    return o is null;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                if (_convertBack is not null && value is TTo to)
                {
                    var values = _convertBack(to);
                    return new object[] { values.Item1, values.Item2 };
                }

                return new[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
            }
        }

        /// <summary>
        /// Expresses a one or two-way value converter using one or two lambda expressions, respectively.
        /// </summary>
        /// <typeparam name="TFrom1">The first source type, to convert from.</typeparam>
        /// <typeparam name="TFrom2">The second source type, to convert from.</typeparam>
        /// <typeparam name="TFrom3">The third source type, to convert from.</typeparam>
        /// <typeparam name="TTo">The destination type, to convert to.</typeparam>
        private sealed class LambdaMultiConverter<TFrom1, TFrom2, TFrom3, TTo> : IMultiValueConverter
        {
            private readonly Func<TFrom1, TFrom2, TFrom3, TTo> _convert;
            private readonly Func<TTo, (TFrom1, TFrom2, TFrom3)> _convertBack;

            public LambdaMultiConverter(Func<TFrom1, TFrom2, TFrom3, TTo> convert, Func<TTo, (TFrom1, TFrom2, TFrom3)> convertBack = null)
            {
                _convert = convert;
                _convertBack = convertBack;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length == 3 && TryConvert(values[0], out TFrom1 t1) && TryConvert(values[1], out TFrom2 t2) && TryConvert(values[2], out TFrom3 t3))
                {
                    return _convert(t1, t2, t3);
                }

                return DependencyProperty.UnsetValue;

                static bool TryConvert<T>(object o, out T t)
                {
                    if (o is T tt)
                    {
                        t = tt;
                        return true;
                    }

                    t = default;
                    return o is null;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                if (_convertBack is not null && value is TTo to)
                {
                    var values = _convertBack(to);
                    return new object[] { values.Item1, values.Item2, values.Item3 };
                }

                return new[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
            }
        }

        /// <summary>
        /// Expresses a one or two-way value converter using one or two lambda expressions, respectively.
        /// </summary>
        /// <typeparam name="TFrom1">The first source type, to convert from.</typeparam>
        /// <typeparam name="TFrom2">The second source type, to convert from.</typeparam>
        /// <typeparam name="TFrom3">The third source type, to convert from.</typeparam>
        /// <typeparam name="TFrom4">The fourth source type, to convert from.</typeparam>
        /// <typeparam name="TTo">The destination type, to convert to.</typeparam>
        private sealed class LambdaMultiConverter<TFrom1, TFrom2, TFrom3, TFrom4, TTo> : IMultiValueConverter
        {
            private readonly Func<TFrom1, TFrom2, TFrom3, TFrom4, TTo> _convert;
            private readonly Func<TTo, (TFrom1, TFrom2, TFrom3, TFrom4)> _convertBack;

            public LambdaMultiConverter(Func<TFrom1, TFrom2, TFrom3, TFrom4, TTo> convert, Func<TTo, (TFrom1, TFrom2, TFrom3, TFrom4)> convertBack = null)
            {
                _convert = convert;
                _convertBack = convertBack;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length == 4 && TryConvert(values[0], out TFrom1 t1) && TryConvert(values[1], out TFrom2 t2) && TryConvert(values[2], out TFrom3 t3) && TryConvert(values[3], out TFrom4 t4))
                {
                    return _convert(t1, t2, t3, t4);
                }

                return DependencyProperty.UnsetValue;

                static bool TryConvert<T>(object o, out T t)
                {
                    if (o is T tt)
                    {
                        t = tt;
                        return true;
                    }

                    t = default;
                    return o is null;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                if (_convertBack is not null && value is TTo to)
                {
                    var values = _convertBack(to);
                    return new object[] { values.Item1, values.Item2, values.Item3, values.Item4 };
                }

                return new[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue };
            }
        }
    }
}
