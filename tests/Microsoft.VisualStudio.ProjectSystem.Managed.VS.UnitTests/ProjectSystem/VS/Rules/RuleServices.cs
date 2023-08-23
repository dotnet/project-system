// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem.Rules;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Rules
{
    internal static class RuleServices
    {
        /// <summary>
        ///     Returns the list of embedded rules
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllEmbeddedRules()
        {
            foreach (MemberInfo member in GetAllExportedMembers())
            {
                foreach (var attribute in member.GetCustomAttributes<ExportRuleAttribute>())
                {
                    if (attribute is not null)
                    {
                        yield return attribute.RuleName;
                    }
                }

                foreach (var attribute in member.GetCustomAttributes<ExportVSRuleAttribute>())
                {
                    if (attribute is not null)
                    {
                        yield return attribute.RuleName;
                    }
                }
            }
        }

        public static IEnumerable<Type> GetRuleExporterTypes()
        {
            Type parentType1 = typeof(RuleExporter);

            foreach (Type type in parentType1.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                yield return type;
            }

            yield return parentType1;

            Type parentType2 = typeof(VSRuleExporter);

            foreach (Type type in parentType2.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                yield return type;
            }

            yield return parentType2;
        }

        public static IEnumerable<MemberInfo> GetAllExportedMembers()
        {
            foreach (Type type in GetRuleExporterTypes())
            {
                foreach (MemberInfo member in GetDeclaredMembers(type))
                {
                    yield return member;
                }
            }
        }

        public static IEnumerable<MemberInfo> GetDeclaredMembers(Type type)
        {
            foreach (MemberInfo member in type.GetMembers())
            {
                if (member.DeclaringType == type)
                    yield return member;
            }
        }
    }
}
