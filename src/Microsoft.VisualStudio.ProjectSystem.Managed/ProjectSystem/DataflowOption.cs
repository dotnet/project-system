// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs (we are wrapping them)

using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides properties and methods containing common dataflow link and block options.
    /// </summary>
    internal static class DataflowOption
    {
        /// <summary>
        ///     Returns a new instance of <see cref="DataflowLinkOptions"/> with
        ///     <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        public static DataflowLinkOptions PropagateCompletion
        {
            get
            {
                return new DataflowLinkOptions()
                {
                    PropagateCompletion = true  // Make sure source block completion and faults flow onto the target block to avoid hangs.
                };
            }
        }

        /// <summary>
        ///     Returns a new instance of <see cref="StandardRuleDataflowLinkOptions"/> with
        ///     <see cref="StandardRuleDataflowLinkOptions.RuleNames"/> set to <paramref name="ruleNames"/>
        ///     and <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="ruleNames"/> is <see langword="null"/>.
        /// </exception>
        public static StandardRuleDataflowLinkOptions WithRuleNames(IEnumerable<string> ruleNames)
        {
            Requires.NotNull(ruleNames, nameof(ruleNames));

            return WithRuleNames(ImmutableHashSet.CreateRange(ruleNames));
        }

        /// <summary>
        ///     Returns a new instance of <see cref="StandardRuleDataflowLinkOptions"/> with
        ///     <see cref="StandardRuleDataflowLinkOptions.RuleNames"/> set to <paramref name="ruleNames"/>
        ///     and <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="ruleNames"/> is <see langword="null"/>.
        /// </exception>
        public static StandardRuleDataflowLinkOptions WithRuleNames(params string[] ruleNames)
        {
            Requires.NotNull(ruleNames, nameof(ruleNames));

            return WithRuleNames(ImmutableHashSet.Create(ruleNames));
        }

        /// <summary>
        ///     Returns a new instance of <see cref="StandardRuleDataflowLinkOptions"/> with
        ///     <see cref="StandardRuleDataflowLinkOptions.RuleNames"/> set to <paramref name="ruleNames"/>
        ///     and <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="ruleNames"/> is <see langword="null"/>.
        /// </exception>
        public static StandardRuleDataflowLinkOptions WithRuleNames(IImmutableSet<string> ruleNames)
        {
            Requires.NotNull(ruleNames, nameof(ruleNames));

            return new StandardRuleDataflowLinkOptions()
            {
                RuleNames = ruleNames,
                PropagateCompletion = true,
            };
        }
    }
}
