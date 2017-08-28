// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{

    [ProjectSystemTrait]
    public class EnumMatchToBooleanConverterTests
    {
       [Fact]
        public void EnumMatchToBooleanConverter_ConvertTests()
        {
            EnumMatchToBooleanConverter converter = new EnumMatchToBooleanConverter();

            Assert.False((bool)converter.Convert(null, typeof(MyEnum), null, CultureInfo.CurrentCulture));
            Assert.True((bool)converter.Convert(MyEnum.Value12, typeof(MyEnum), "Value12", CultureInfo.CurrentCulture));
            Assert.False((bool)converter.Convert(MyEnum.Value12, typeof(MyEnum), "Value13", CultureInfo.CurrentCulture));
        }

        [Fact]
        public void EnumMatchToBooleanConverter_ConvertBackTests()
        {
            EnumMatchToBooleanConverter converter = new EnumMatchToBooleanConverter();

            Assert.Equal(converter.ConvertBack(null, typeof(MyEnum), null, CultureInfo.CurrentCulture),null);
            Assert.Equal(MyEnum.Value12, converter.ConvertBack(true, typeof(MyEnum), "Value12", CultureInfo.CurrentCulture));
            Assert.Equal(MyEnum.Value13, converter.ConvertBack(true, typeof(MyEnum), "Value13", CultureInfo.CurrentCulture));
            Assert.Equal(MyEnum.Value12, converter.ConvertBack(true, typeof(MyEnum), "12", CultureInfo.CurrentCulture));
            Assert.Equal(MyEnum.Value13, converter.ConvertBack(true, typeof(MyEnum), "13", CultureInfo.CurrentCulture));

            Assert.Equal(converter.ConvertBack(false, typeof(MyEnum), "Value12", CultureInfo.CurrentCulture), null);
            Assert.Equal(converter.ConvertBack(false, typeof(MyEnum), "Value13", CultureInfo.CurrentCulture), null);
            Assert.Equal(converter.ConvertBack(false, typeof(MyEnum), "12", CultureInfo.CurrentCulture), null);
            Assert.Equal(converter.ConvertBack(false, typeof(MyEnum), "13", CultureInfo.CurrentCulture), null);
        }
    }

    public enum MyEnum
    {
        Value12 = 12,
        Value13 = 13
    };
}
