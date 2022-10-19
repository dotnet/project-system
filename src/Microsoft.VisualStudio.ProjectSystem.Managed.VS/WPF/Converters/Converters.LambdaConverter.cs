// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Windows.Data;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    // Copied from CPS Converters.cs
    internal static partial class Converters
    {
        /// <summary>
        /// Expresses a one or two-way value converter using one or two lambda expressions, respectively.
        /// </summary>
        /// <typeparam name="TFrom">The source type, to convert from.</typeparam>
        /// <typeparam name="TTo">The destination type, to convert to.</typeparam>
        private sealed class LambdaConverter<TFrom, TTo> : IValueConverter
        {
            private readonly Func<TFrom, TTo> _convert;
            private readonly Func<TTo, TFrom>? _convertBack;
            private readonly bool _convertFromNull;

            public LambdaConverter(Func<TFrom, TTo> convert, Func<TTo, TFrom>? convertBack = null, bool convertFromNull = true)
            {
                _convert = convert;
                _convertBack = convertBack;
                _convertFromNull = convertFromNull;
            }

            public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
            {
                try
                {
                    if (value is TFrom from)
                    {
                        return _convert(from);
                    }

                    if (value is null && _convertFromNull)
                    {
                        return _convert(default!);
                    }

                    return value;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Callback threw an exception when converting from {typeof(TFrom)} to {typeof(TTo)}.", ex);
                }
            }

            public object? ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
            {
                try
                {
                    if (_convertBack is not null && value is TTo to)
                    {
                        return _convertBack(to);
                    }

                    return value;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Callback threw an exception when converting back to {typeof(TTo)} from {typeof(TFrom)}.", ex);
                }
            }
        }
    }
}
