// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    internal static class RuleServices
    {
        /// <summary>
        ///     Returns the list of embedded rules
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllEmbeddedRules()
        {
            foreach (var member in GetAllExportedMembers())
            {
                var attribute = member.GetCustomAttribute<ExportRuleAttribute>();
                if (attribute is not null)
                    yield return attribute.RuleName;
            }
        }

        public static IEnumerable<Type> GetAllExportedTypes()
        {
            Type parentType = typeof(RuleExporter);

            foreach (var type in parentType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
            {
                yield return type;
            }

            yield return parentType;
        }

        public static IEnumerable<MemberInfo> GetAllExportedMembers()
        {
            foreach (Type type in GetAllExportedTypes())
            {
                foreach (MemberInfo member in GetDeclaredMembers(type))
                {
                    yield return member;
                }
            }
        }

        public static IEnumerable<MemberInfo> GetDeclaredMembers(Type type)
        {
            foreach (var member in type.GetMembers())
            {
                if (member.DeclaringType == type)
                    yield return member;
            }
        }
    }
}
