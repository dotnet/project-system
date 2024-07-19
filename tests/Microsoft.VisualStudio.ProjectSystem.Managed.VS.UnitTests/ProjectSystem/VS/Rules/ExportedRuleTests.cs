﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Rules;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rules
{
    public sealed class RuleExporterTests : XamlRuleTestBase
    {
        [Theory(Skip = "Waiting on all rules to be embedded")]
        [MemberData(nameof(GetAllRules))]
        public void AllRulesMustBeExported(string name, string fullPath, Type assemblyExporterType)
        {
            name = name.Replace(".", "");

            MemberInfo member = typeof(VSRuleExporter).GetField(name) ?? typeof(RuleExporter).GetField(name);

            Assert.True(member is not null, $"Rule '{fullPath}' has not been exported by {nameof(RuleExporter)} or {nameof(VSRuleExporter)}. Export this rule from a member called \"{name}\".");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembers))]
        public void ExportedRulesMustBeStaticFields(MemberInfo member)
        {
            Assert.True(member is FieldInfo { IsStatic: true }, $"'{GetTypeQualifiedName(member)}' must be a static field.");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembers))]
        public void ExportedRulesMustBeMarkedWithAppliesTo(MemberInfo member)
        {
            var attribute = member.GetCustomAttribute<AppliesToAttribute>();

            Assert.True(attribute is not null, $"'{GetTypeQualifiedName(member)}' must be marked with [AppliesTo]");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembers))]
        public void ExportedRulesMustBeMarkedWithOrder(MemberInfo member)
        {
            var attribute = member.GetCustomAttribute<OrderAttribute>();

            Assert.True(attribute?.OrderPrecedence == Order.Default, $"'{GetTypeQualifiedName(member)}' must be marked with [Order(Order.Default)]");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembers))]
        public void ExportedFieldsMustEndInRule(MemberInfo member)
        {
            Assert.True(member.Name.EndsWith("Rule"), $"'{GetTypeQualifiedName(member)}' must be end in 'Rule' so that the '[ExportRule(nameof(RuleName))]' expression refers to the rule itself");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembersWithDeclaringType))]
        public void MembersInSameTypeMustBeMarkedWithSameAppliesTo(Type declaringType, IEnumerable<MemberInfo> members)
        {
            string? appliesTo = null;

            foreach (MemberInfo member in members)
            {
                var attribute = member.GetCustomAttribute<AppliesToAttribute>();
                if (attribute is null)
                    continue; // Another test will catch this

                if (appliesTo is null)
                {
                    appliesTo = attribute.AppliesTo;
                }
                else
                {
                    Assert.True(appliesTo == attribute.AppliesTo, $"{declaringType}'s member must be all have the same value for [AppliesTo]");
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembersWithAttribute))]
        public void BrowseObjectsMustBeInBrowseObjectContext(MemberInfo member, ExportPropertyXamlRuleDefinitionAttribute attribute)
        {
            if (!member.Name.Contains("BrowseObject"))
                return;

            foreach (string context in attribute.Context.Split(';'))
            {
                if (context == PropertyPageContexts.BrowseObject)
                    return;
            }

            Assert.Fail($"'{GetTypeQualifiedName(member)}' must live in the PropertyPageContexts.BrowseObject context.");
        }

        [Theory]
        [MemberData(nameof(GetAllExportedMembersWithAttribute))]
        public void ExportedRulesMustExist(MemberInfo member, ExportPropertyXamlRuleDefinitionAttribute attribute)
        {
            Assert.NotNull(attribute);

            // HERE BE DRAGONS
            // Note the following are *not* equivalent:
            //   Assembly.Load(assemblyNameString)
            //   Assembly.Load(new AssemblyName(assemblyNameString))
            // The first will accept certain malformed assembly names that the second does not,
            // and will successfully load the assembly where the second throws an exception.
            // CPS uses the second form when loading assemblies to extract embedded XAML, and
            // so we must do the same in this test.
            var assemblyName = new AssemblyName(attribute.XamlResourceAssemblyName);
            var assembly = Assembly.Load(assemblyName);

            using Stream stream = assembly.GetManifestResourceStream(attribute.XamlResourceStreamName);

            Assert.True(stream is not null, $"The rule '{attribute.XamlResourceStreamName}' indicated by '{GetTypeQualifiedName(member)}' does not exist in assembly '{attribute.XamlResourceAssemblyName}'.");
        }

        private static string GetTypeQualifiedName(MemberInfo member)
        {
            return $"{member.DeclaringType.Name}.{member.Name}";
        }

        public static IEnumerable<object[]> GetAllExportedMembers()
        {
            return RuleServices.GetAllExportedMembers().Select(member => new[] { member });
        }

        public static IEnumerable<object[]> GetAllExportedMembersWithAttribute()
        {
            foreach (MemberInfo member in RuleServices.GetAllExportedMembers())
            {
                Attribute attribute = member.GetCustomAttribute<ExportPropertyXamlRuleDefinitionAttribute>();

                yield return new object[] { member, attribute };
            }
        }

        public static IEnumerable<object[]> GetAllExportedMembersWithDeclaringType()
        {
            foreach (Type type in RuleServices.GetRuleExporterTypes())
            {
                yield return new object[] { type, RuleServices.GetDeclaredMembers(type) };
            }
        }

        public static IEnumerable<object[]> GetAllRules()
        {
            return Project(GetRules(suffix: string.Empty, recursive: true));
        }
    }
}
