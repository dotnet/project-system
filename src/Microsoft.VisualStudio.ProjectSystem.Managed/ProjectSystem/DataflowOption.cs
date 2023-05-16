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
            get => new()
            {
                // Make sure source block completion and faults flow onto the target block to avoid hangs.
                PropagateCompletion = true
            };
        }

        /// <summary>
        ///     Returns a new instance of <see cref="StandardRuleDataflowLinkOptions"/> with
        ///     <see cref="StandardRuleDataflowLinkOptions.RuleNames"/> set to <paramref name="ruleName"/>
        ///     and <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="ruleName"/> is <see langword="null"/>.
        /// </exception>
        public static StandardRuleDataflowLinkOptions WithRuleNames(string ruleName)
        {
            Requires.NotNull(ruleName);

            return WithRuleNames(ImmutableHashSet.Create(ruleName));
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
            Requires.NotNull(ruleNames);

            // This class sets PropagateCompletion by default, so we don't have to set it here again.
            return new()
            {
                RuleNames = ruleNames
            };
        }

        /// <summary>
        ///     Returns a new instance of <see cref="JointRuleDataflowLinkOptions"/> with
        ///     <see cref="JointRuleDataflowLinkOptions.EvaluationRuleNames"/> set to <paramref name="evaluationRuleNames"/>,
        ///     <see cref="JointRuleDataflowLinkOptions.BuildRuleNames"/> set to <paramref name="buildRuleNames"/>
        ///     and <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        public static JointRuleDataflowLinkOptions WithJointRuleNames(ImmutableHashSet<string> evaluationRuleNames, ImmutableHashSet<string> buildRuleNames)
        {
            // This class sets PropagateCompletion by default, so we don't have to set it here again.
            return new()
            {
                EvaluationRuleNames = evaluationRuleNames,
                BuildRuleNames = buildRuleNames
            };
        }
    }
}
