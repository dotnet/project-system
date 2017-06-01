// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    internal abstract class AbstractBuildEventValueProvider : InterceptingPropertyValueProviderBase
    {
        private const string _execTaskName = "Exec";
        private const string _commandString = "Command";

        protected readonly IProjectLockService _projectLockService;
        protected readonly UnconfiguredProject _unconfiguredProject;

        protected AbstractBuildEventValueProvider(
            IProjectLockService projectLockService,
            UnconfiguredProject unconfiguredProject)
        {
            _projectLockService = projectLockService;
            _unconfiguredProject = unconfiguredProject;
        }

        protected abstract string GetTargetString(ProjectTargetElement target);
        protected abstract void SetTargetDependencies(ProjectTargetElement target);
        protected abstract string BuildEventString { get; }
        protected abstract string TargetNameString { get; }

        public override async Task<string> OnGetEvaluatedPropertyValueAsync(
            string evaluatedPropertyValue,
            IProjectProperties defaultProperties)
        {
            using (var access = await _projectLockService.ReadLockAsync())
            {
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);
                var result = FindExecTaskInTargets(projectXml);

                if (result.success == false)
                {
                    return null;
                }

                if (result.execTask.Parameters.TryGetValue(_commandString, out var commandText))
                {
                    return commandText;
                }

                return null;
            }
        }

        public override async Task<string> OnSetPropertyValueAsync(
            string unevaluatedPropertyValue,
            IProjectProperties defaultProperties,
            IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            using (var access = await _projectLockService.WriteLockAsync())
            {
                var projectXml = await access.GetProjectXmlAsync(_unconfiguredProject.FullPath).ConfigureAwait(true);

                if (string.IsNullOrWhiteSpace(unevaluatedPropertyValue))
                {
                    var result = FindTargetToRemove(projectXml);
                    if (result.success)
                    {
                        projectXml.RemoveChild(result.target);
                    }
                }
                else
                {
                    SetParameter(projectXml, unevaluatedPropertyValue);
                }
            }

            return null;
        }

        private void SetParameter(ProjectRootElement projectXml, string unevaluatedPropertyValue)
        {
            var result = FindExecTaskInTargets(projectXml);

            if (result.success == true)
            {
                SetExecParameter(result.execTask, unevaluatedPropertyValue);
            }
            else
            {
                var targetName = GetTargetName(projectXml);
                var target = projectXml.AddTarget(targetName);
                SetTargetDependencies(target);
                var execTask = target.AddTask(_execTaskName);
                SetExecParameter(execTask, unevaluatedPropertyValue);
            }
        }

        private string GetTargetName(ProjectRootElement projectXml)
        {
            var targetNames = projectXml.Targets.Select(t => t.Name).ToArray();
            var targetName = TargetNameString;
            if (targetNames.Contains(targetName))
            {
                targetName = FindNonCollidingName(targetName, targetNames);
            }

            return targetName;
        }

        private string FindNonCollidingName(string buildEventString, string[] targetNames)
        {
            var initialValue = 1;
            var newName = buildEventString + initialValue.ToString();
            while (targetNames.Contains(newName))
            {
                initialValue++;
                newName = buildEventString + initialValue.ToString();
            }

            return newName;
        }

        private void SetExecParameter(ProjectTaskElement execTask, string unevaluatedPropertyValue)
        {
            execTask.SetParameter(_commandString, unevaluatedPropertyValue);
        }

        private (bool success, ProjectTaskElement execTask) FindExecTaskInTargets(ProjectRootElement projectXml)
        {
            var execTask = projectXml.Targets
                                .Where(target => GetTargetString(target) == BuildEventString)
                                .SelectMany(target => target.Tasks)
                                .Where(task => task.Name == _execTaskName)
                                .FirstOrDefault();
            return (success: execTask != null, execTask: execTask);
        }

        private (bool success, ProjectTargetElement target) FindTargetToRemove(ProjectRootElement projectXml)
        {
            var targetArray = projectXml.Targets
                                    .Where(target =>
                                        GetTargetString(target) == BuildEventString &&
                                        target.Children.Count == 1 &&
                                        target.Tasks.Count == 1 &&
                                        target.Tasks.First().Name == _execTaskName);
            return (success: targetArray.Count() == 1, target: targetArray.SingleOrDefault());
        }
    }
}
