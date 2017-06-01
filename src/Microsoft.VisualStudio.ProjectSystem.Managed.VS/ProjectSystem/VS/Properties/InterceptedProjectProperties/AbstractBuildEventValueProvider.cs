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
        protected abstract void SetTargetString(ProjectTargetElement target, string targetName);
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
                    return string.Empty;
                }

                if (result.execTask.Parameters.TryGetValue(_commandString, out var commandText))
                {
                    return UnReplaceMSBuildReservedCharacters(commandText);
                }

                return string.Empty;
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

            return string.Empty;
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
                // TODO: What if there is already a target named "PreBuild" or "PostBuild"?
                var prebuildTarget = projectXml.AddTarget(TargetNameString);
                SetTargetString(prebuildTarget, BuildEventString);
                var execTask = prebuildTarget.AddTask(_execTaskName);
                SetExecParameter(execTask, unevaluatedPropertyValue);
            }
        }

        private void SetExecParameter(ProjectTaskElement execTask, string unevaluatedPropertyValue)
        {
            execTask.SetParameter(_commandString, this.ReplaceMSBuildReservedCharacters(unevaluatedPropertyValue));
        }

        private string ReplaceMSBuildReservedCharacters(string value)
            => value
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

        private string UnReplaceMSBuildReservedCharacters(string value)
            => value
                .Replace("&quot;", "\"")
                .Replace("&apos;", "'")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">");

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
