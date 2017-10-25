// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Telemetry;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class DependencyTreeTelemetryServiceTests
    {
        private const string TestFilePath = @"C:\Path\To\Project.csproj";
        private static readonly Guid TestGuid = Guid.NewGuid();
        private static ITelemetryServiceFactory.TelemetryParameters CalledParameters;

        /// <summary>
        /// This is a general end-to-end test of expected common flow
        /// </summary>
        [Fact]        
        public void DesignTimeWithAllChanges_TreeUpdateResolved_MultiTFM()
        {
            var handlerName = "TestHandler";
            var tfNames = new string[] { "tfm1", "tfm2" };
            var targetFrameworks = tfNames.Select(tfm => ITargetFrameworkFactory.Implement(tfm));
            var evalRules = new List<string> { "eval1", "eval2", "eval3" };
            var dtRules = new List<string> { "dt1", "dt2", "dt3" };
            var allRules = evalRules.Concat(dtRules);
            var withChanges = IProjectChangeDescriptionFactory.Implement(difference: IProjectChangeDiffFactory.Implement(anyChanges: true));
            var noChanges = IProjectChangeDescriptionFactory.Implement(difference: IProjectChangeDiffFactory.Implement(anyChanges: false));

            var telemetryService = CreateInstance();

            // Initialization
            foreach (var tf in targetFrameworks)
            {
                telemetryService.ObserveUnresolvedRules(tf, evalRules);
            }

            // Observe Evaluation
            foreach (var tf in targetFrameworks)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, IProjectChangeDescription>(StringComparers.RuleNames);
                evalRules.ForEach(rule => builder.Add(rule, withChanges));
                dtRules.ForEach(rule => builder.Add(rule, noChanges));

                telemetryService.ObserveHandlerRulesChanges(tf, handlerName, allRules, RuleHandlerType.Evaluation, builder.ToImmutable());
                telemetryService.ObserveCompleteHandlers(tf, RuleHandlerType.Evaluation);

                telemetryService.ObserveTreeUpdateCompleted();
                CheckForUnresolvedTreeUpdate();
                ResetTestCallParameters();
            }

            // Observe Design Time
            var count = 0;
            foreach (var tf in targetFrameworks)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, IProjectChangeDescription>(StringComparers.RuleNames);
                evalRules.ForEach(rule => builder.Add(rule, noChanges));
                dtRules.ForEach(rule => builder.Add(rule, withChanges));

                telemetryService.ObserveHandlerRulesChanges(tf, handlerName, allRules, RuleHandlerType.DesignTimeBuild, builder.ToImmutable());
                telemetryService.ObserveCompleteHandlers(tf, RuleHandlerType.DesignTimeBuild);

                telemetryService.ObserveTreeUpdateCompleted();

                // only resolved after the last target framework
                if (++count < targetFrameworks.Count())
                {
                    CheckForUnresolvedTreeUpdate();
                }
                else
                {
                    CheckForResolvedTreeUpdate();
                }
                ResetTestCallParameters();
            }

            // subsequent tree updates should not fire telemetry
            telemetryService.ObserveTreeUpdateCompleted();
            Assert.Null(CalledParameters.EventName);
        }

        [Fact]
        public void ObserveUnresolvedRules_TreeUpdateUnresolved()
        {
            var telemetryService = CreateInstance();

            telemetryService.ObserveUnresolvedRules(
                ITargetFrameworkFactory.Implement("tfm1"),
                new string[] { "Rule1", "Rule2" });

            telemetryService.ObserveTreeUpdateCompleted();

            CheckForUnresolvedTreeUpdate();
        }

        /// <summary>
        /// Evaluation after Design Time stops telemetry even if no Resolved yet
        /// </summary>
        [Fact]
        public void EvaluationAfterDesignTime_StopsTelemetry()
        {
            var handlerName = "TestHandler";
            var targetFramework = ITargetFrameworkFactory.Implement("tfm1");
            var evalRules = new List<string> { "eval1", "eval2", "eval3" };
            var dtRules = new List<string> { "dt1", "dt2", "dt3" };
            var allRules = evalRules.Concat(dtRules);
            var withChanges = IProjectChangeDescriptionFactory.Implement(difference: IProjectChangeDiffFactory.Implement(anyChanges: true));
            var noChanges = IProjectChangeDescriptionFactory.Implement(difference: IProjectChangeDiffFactory.Implement(anyChanges: false));

            var telemetryService = CreateInstance();

            // Initialization
            telemetryService.ObserveUnresolvedRules(targetFramework, evalRules);

            // Observe Evaluation
            var builder = ImmutableDictionary.CreateBuilder<string, IProjectChangeDescription>(StringComparers.RuleNames);
            evalRules.ForEach(rule => builder.Add(rule, withChanges));
            dtRules.ForEach(rule => builder.Add(rule, noChanges));

            telemetryService.ObserveHandlerRulesChanges(targetFramework, handlerName, allRules, RuleHandlerType.Evaluation, builder.ToImmutable());
            telemetryService.ObserveCompleteHandlers(targetFramework, RuleHandlerType.Evaluation);

            telemetryService.ObserveTreeUpdateCompleted();
            CheckForUnresolvedTreeUpdate();
            ResetTestCallParameters();

            // Observe Design Time
            builder = ImmutableDictionary.CreateBuilder<string, IProjectChangeDescription>(StringComparers.RuleNames);
            evalRules.ForEach(rule => builder.Add(rule, noChanges));
            dtRules.ForEach(rule => builder.Add(rule, rule != "dt3" ? withChanges : noChanges));

            telemetryService.ObserveHandlerRulesChanges(targetFramework, handlerName, allRules, RuleHandlerType.DesignTimeBuild, builder.ToImmutable());
            telemetryService.ObserveCompleteHandlers(targetFramework, RuleHandlerType.DesignTimeBuild);

            telemetryService.ObserveTreeUpdateCompleted();

            // one of the design time rules has no changes hence this should not report resolved
            CheckForUnresolvedTreeUpdate();
            ResetTestCallParameters();

            // Observe another evaluation
            builder = ImmutableDictionary.CreateBuilder<string, IProjectChangeDescription>(StringComparers.RuleNames);
            evalRules.ForEach(rule => builder.Add(rule, withChanges));
            dtRules.ForEach(rule => builder.Add(rule, noChanges));

            telemetryService.ObserveHandlerRulesChanges(targetFramework, handlerName, allRules, RuleHandlerType.Evaluation, builder.ToImmutable());
            telemetryService.ObserveCompleteHandlers(targetFramework, RuleHandlerType.Evaluation);

            // no more telemetry events
            telemetryService.ObserveTreeUpdateCompleted();
            Assert.Null(CalledParameters.EventName);
        }

        private void ResetTestCallParameters()
        {
            CalledParameters.EventName = null;
            CalledParameters.Properties = null;
        }

        private static void CheckForResolvedTreeUpdate()
        {
            Assert.Equal("TreeUpdated/Resolved", CalledParameters.EventName);
            CheckForProjectGuid();
        }

        private static void CheckForUnresolvedTreeUpdate()
        {
            Assert.Equal("TreeUpdated/Unresolved", CalledParameters.EventName);
            CheckForProjectGuid();
        }

        private static void CheckForProjectGuid()
        {
            Assert.Equal("Project", CalledParameters.Properties.Single().propertyName);
            Assert.Equal(TestGuid.ToString(), CalledParameters.Properties.Single().propertyValue);
        }

        static DependencyTreeTelemetryService CreateInstance()
        {
            CalledParameters = new ITelemetryServiceFactory.TelemetryParameters();

            var telemetryService = new DependencyTreeTelemetryService(
                UnconfiguredProjectFactory.Create(filePath: TestFilePath), 
                ITelemetryServiceFactory.Create(CalledParameters));

            // set id directly because ExportProvider.GetExportedValueOrDefault() is an extension
            // method and cannot be mocked.
            telemetryService.SetProjectId(TestGuid.ToString());
            return telemetryService;
        }
    }
}
