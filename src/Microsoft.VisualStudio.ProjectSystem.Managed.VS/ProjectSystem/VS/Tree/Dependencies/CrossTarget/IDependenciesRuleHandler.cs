// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal interface IDependenciesRuleHandler
    {
        /// <summary>
        ///     Gets the rule this handler handles.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing the rule that this <see cref="IDependenciesRuleHandler"/> 
        ///     handles.
        /// </value>
        ImmutableHashSet<string> GetRuleNames(RuleHandlerType handlerType);

        /// <summary>
        ///     Handles the specified set of changes to a rule, and applies them
        ///     to the given <see cref="CrossTargetDependenciesChangesBuilder"/>.
        /// </summary>
        void Handle(
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            ITargetFramework targetFramework,
            CrossTargetDependenciesChangesBuilder changesBuilder);
    }
}
