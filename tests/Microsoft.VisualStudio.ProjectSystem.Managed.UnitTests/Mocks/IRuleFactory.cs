// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IRuleFactory
    {
        public static IRule Create(
            string? name = null,
            string? displayName = null,
            IEnumerable<IProperty>? properties = null,
            string? pageTemplate = null,
            Dictionary<string, object>? metadata = null)
        {
            var schema = new Rule
            {
                Name = name,
                DisplayName = displayName,
                Metadata = metadata,
                PageTemplate = pageTemplate
            };

            var rule = new Mock<IRule>();

            if (properties != null)
            {
                rule.Setup(o => o.GetProperty(It.IsAny<string>()))
                    .Returns((string propertyName) =>
                    {
                        return properties.FirstOrDefault(p => p.Name == propertyName);
                    });
            }

            rule.Setup(o => o.Schema)
                .Returns(schema);

            return rule.Object;
        }

        public static IRule Create(Rule schema)
        {
            var rule = new Mock<IRule>();

            rule.Setup(o => o.Schema)
                .Returns(schema);

            return rule.Object;
        }
    }
}
