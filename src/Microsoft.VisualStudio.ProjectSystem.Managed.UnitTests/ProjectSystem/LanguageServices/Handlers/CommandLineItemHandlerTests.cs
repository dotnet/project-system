// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Mocks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    [ProjectSystemTrait]
    public class CommandLineItemHandlerTests
    {
        [Fact]
        public void RuleSetPassedToLanguageService()
        {
            string options = null;
            string ruleSetFile = null;

            Action<string> onSetOptions = s => options = s;
            Action<string> onSetRuleSetFile = s => ruleSetFile = s;

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForCommandLineArguments(project, onSetOptions, onSetRuleSetFile);

            var after = IProjectRuleSnapshotFactory.Create(CompilerCommandLineArgs.SchemaName)
                .AddItem(@"/ruleset:MyRules.ruleset");
            var change = IProjectChangeDescriptionFactory.CreateFromSnapshots(after: after, diff: IProjectChangeDiffFactory.Create());

            var handler = new CommandLineItemHandler(project, ICommandLineParserServiceFactory.Create());
            handler.Handle(change, context, isActiveContext: false);

            Assert.Equal(expected: @"/ruleset:MyRules.ruleset", actual: options);
            Assert.Equal(expected: @"C:\Myproject\MyRules.ruleset", actual: ruleSetFile);
        }

        [Fact]
        public void EmptyStringPassedToLanguageServiceWhenNoRuleSetPresent()
        {
            string options = null;
            string ruleSetFile = null;

            Action<string> onSetOptions = s => options = s;
            Action<string> onSetRuleSetFile = s => ruleSetFile = s;

            var project = UnconfiguredProjectFactory.Create(filePath: @"C:\Myproject\Myproject.csproj");
            var context = IWorkspaceProjectContextFactory.CreateForCommandLineArguments(project, onSetOptions, onSetRuleSetFile);

            var after = IProjectRuleSnapshotFactory.Create(CompilerCommandLineArgs.SchemaName)
                .AddItem(@"/reportanalyzer");
            var change = IProjectChangeDescriptionFactory.CreateFromSnapshots(after: after, diff: IProjectChangeDiffFactory.Create());

            var handler = new CommandLineItemHandler(project, ICommandLineParserServiceFactory.Create());
            handler.Handle(change, context, isActiveContext: false);

            Assert.Equal(expected: @"/reportanalyzer", actual: options);
            Assert.Equal(expected: string.Empty, actual: ruleSetFile);
        }
    }
}
