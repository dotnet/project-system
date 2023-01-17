// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IPropertyFactory
    {
        public static IEnumProperty CreateEnum(IEnumValue[]? admissibleValues = null)
        {
            var mock = new Mock<IEnumProperty>();

            if (admissibleValues is not null)
            {
                mock.Setup(m => m.GetAdmissibleValuesAsync()).ReturnsAsync(new ReadOnlyCollection<IEnumValue>(admissibleValues));
            }

            return mock.Object;
        }

        public static IProperty Create(
            string? name = null,
            IDataSource? dataSource = null,
            Action<object?>? setValue = null)
        {
            var mock = new Mock<IProperty>();

            if (name is not null)
            {
                mock.SetupGet(m => m.Name).Returns(name);
            }

            if (dataSource is not null)
            {
                mock.SetupGet(m => m.DataSource).Returns(dataSource);
            }

            if (setValue is not null)
            {
                mock.Setup(m => m.SetValueAsync(It.IsAny<object?>()))
                    .Returns((object? o) => { setValue(o); return Task.CompletedTask; }); 
            }

            return mock.Object;
        }
    }

    internal static class IDataSourceFactory
    {
        public static IDataSource Create(bool? hasConfigurationCondition = false)
        {
            var mock = new Mock<IDataSource>();

            if (hasConfigurationCondition is not null)
            {
                mock.SetupGet(m => m.HasConfigurationCondition).Returns(hasConfigurationCondition.Value);
            }

            return mock.Object;
        }
    }
}
