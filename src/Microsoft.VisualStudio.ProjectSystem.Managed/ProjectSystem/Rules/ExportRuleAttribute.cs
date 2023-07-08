// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.Rules
{
    /// <summary>
    ///     Exports a XAML-based embedded rule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal class ExportRuleAttribute : ExportPropertyXamlRuleDefinitionAttribute
    {
        // TODO: If reflection is insufficient, this will also work.
        //private const string AssemblyFullName = $"{ThisAssembly.AssemblyName}, Version = {ThisAssembly.AssemblyVersion}, Culture = neutral, PublicKeyToken = {ThisAssembly.PublicKeyToken}";

        /// <summary>
        ///     Initializes the <see cref="ExportRuleAttribute"/> class with the specified rule name and context.
        /// </summary>
        /// <param name="ruleName">
        ///     The name of the rule without '.xaml', for example, 'ConfigurationGeneral'.
        /// </param>
        /// <param name="contexts">
        ///     One or more of <see cref="PropertyPageContexts"/>.
        /// </param>
        public ExportRuleAttribute(string ruleName, params string[] contexts)
            : base(Assembly.GetExecutingAssembly().FullName, $"XamlRuleToCode:{ruleName}.xaml", string.Join(";", contexts))
        {
            RuleName = ruleName;
        }

        /// <summary>
        ///     Gets the name of the rule without '.xaml', for example, 'ConfigurationGeneral'.
        /// </summary>
        public string RuleName
        {
            get;
        }
    }
}
