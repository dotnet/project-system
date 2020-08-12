// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    internal static class RuleServices
    {
        public static IEnumerable<MemberInfo> GetAllExportedMembers()
        {
            foreach (var member in typeof(RuleExporter).GetMembers())
            {
                if (member.DeclaringType == typeof(RuleExporter))
                    yield return member;
            }
        }

        /// <summary>
        ///     Returns the list of embedded rules
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllEmbeddedRules()
        {
            foreach (var member in GetAllExportedMembers())
            {
                var attribute = member.GetCustomAttribute<ExportRuleAttribute>();
                if (attribute != null)
                    yield return attribute.RuleName;
            }
        }
    }
}
