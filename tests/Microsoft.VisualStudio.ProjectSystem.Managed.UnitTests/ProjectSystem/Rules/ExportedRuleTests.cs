// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    public sealed class RuleExporterTests
    {
        [Theory]
        [MemberData(nameof(GetAllExportedMembers))]
        public void ExportedRulesMustBeStaticFields(MemberInfo member)
        {
            Assert.True(member is FieldInfo field && field.IsStatic, $"'{GetTypeQualifiedName(member)}' must be a static field.");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembers))]
        public void ExportedRulesMustHaveAppliesTo(MemberInfo member)
        {
            var attribute = member.GetCustomAttribute<AppliesToAttribute>();

            Assert.True(attribute != null, $"'{GetTypeQualifiedName(member)}' must be marked with AppliesTo");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembers))]
        public void ExportedRulesMustExist(MemberInfo member)
        {
            var attribute = member.GetCustomAttribute<ExportPropertyXamlRuleDefinitionAttribute>();

            Assert.NotNull(attribute);

            var assembly = Assembly.Load(attribute.XamlResourceAssemblyName);

            using Stream stream = assembly.GetManifestResourceStream(attribute.XamlResourceStreamName);

            Assert.True(stream != null, $"The rule '{attribute.XamlResourceStreamName}' indicated by '{GetTypeQualifiedName(member)}' does not exist in assembly '{attribute.XamlResourceAssemblyName}'.");
        }

        private static string GetTypeQualifiedName(MemberInfo member)
        {
            return $"{member.DeclaringType.Name}.{member.Name}";
        }

        public static IEnumerable<object[]> GetAllExportedMembers()
        {
            foreach (var member in typeof(RuleExporter).GetMembers())
            {
                if (member.DeclaringType == typeof(RuleExporter))
                    yield return new[] { member };
            }
        }
    }
}
