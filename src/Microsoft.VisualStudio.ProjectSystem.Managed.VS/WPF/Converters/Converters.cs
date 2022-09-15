// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal sealed class MultiValueBoolToBool_And : IMultiValueConverter
    {
        private static readonly object s_false = false;
        private static readonly object s_true = true;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (object v in values)
            {
                // Any false, the result is false
                if (s_false.Equals(v))
                {
                    return s_false;
                }
            }

            return s_true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack should NOT be invoked");
        }
    }

    internal sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    // Copied from CPS Converters.cs
    internal static partial class Converters
    {
        private const string DimensionSeparator = " & ";
        
        public static ImmutableArray<string> ImmutableStringArray => ImmutableArray<string>.Empty;

        public static IMultiValueConverter DimensionNames { get; } = new LambdaMultiConverter<ImmutableDictionary<string, string>, ImmutableArray<string>, string>(GetDimensionNames);

        public static IValueConverter BoolToVisibilityConverter { get; } = new LambdaConverter<bool, Visibility>(b => b ? Visibility.Visible : Visibility.Collapsed);

        public static IValueConverter IsEnteredUserAliasStringEnabled { get; } = new LambdaConverter<bool, bool>(isStatic => !isStatic);
        
        public static IMultiValueConverter IsListViewAliasStringEnabled { get; } = new LambdaMultiConverter<bool, bool, bool>((isReadOnly, isStatic) => !isReadOnly && !isStatic);

        public static IMultiValueConverter IsIsStaticCheckboxEnabled { get; } = new LambdaMultiConverter<string, bool, bool>((alias, isReadOnly) => !isReadOnly && alias.Length == 0);
        
        public static IValueConverter IsStaticCheckboxText { get; } = new LambdaConverter<string, string>(alias => alias.Length == 0 ? "Yes" : "No");

        public static IValueConverter InvertBoolean { get; } = new LambdaConverter<bool, bool>(b => !b);
        
        private static string GetDimensionNames(ImmutableDictionary<string, string> map, ImmutableArray<string> dimensions)
        {
            if (map.IsEmpty || dimensions.IsEmpty)
            {
                return string.Empty;
            }

            if (map.Count == 1)
            {
                return map.First().Value;
            }

            var sb = new StringBuilder();

            foreach (string? dimension in dimensions)
            {
                if (!map.TryGetValue(dimension, out string? value))
                {
                    continue;
                }

                if (sb.Length != 0)
                {
                    sb.Append(DimensionSeparator);
                }

                sb.Append(value);
            }

            return sb.ToString();
        }
    }
}
