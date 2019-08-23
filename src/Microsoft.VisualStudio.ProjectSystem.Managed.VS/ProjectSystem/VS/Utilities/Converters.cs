// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
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
}
