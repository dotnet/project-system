// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

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
            Action<object?>? setValue = null)
        {
            var mock = new Mock<IProperty>();

            if (name is not null)
            {
                mock.SetupGet(m => m.Name).Returns(name);
            }

            if (setValue is not null)
            {
                mock.Setup(m => m.SetValueAsync(It.IsAny<object?>()))
                    .Returns((object? o) => { setValue(o); return Task.CompletedTask; }); 
            }

            return mock.Object;
        }
    }
}
