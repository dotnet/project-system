// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    internal abstract partial class AbstractBuildEventValueProvider
    {
        public abstract class AbstractBuildEventHelper
        {
            private const string ExecTask = "Exec";
            private const string Command = "Command";

            protected AbstractBuildEventHelper(string buildEvent,
                   string targetName,
                   Func<ProjectTargetElement, string> getTarget,
                   Action<ProjectTargetElement> setTargetDependencies)
            {
                BuildEvent = buildEvent;
                TargetName = targetName;
                GetTarget = getTarget;
                SetTargetDependencies = setTargetDependencies;
            }

            private Func<ProjectTargetElement, string> GetTarget { get; }
            private Action<ProjectTargetElement> SetTargetDependencies { get; }
            private string BuildEvent { get; }
            private string TargetName { get; }

            public async Task<string> GetPropertyAsync(ProjectRootElement projectXml, IProjectProperties defaultProperties)
            {
                // check if value already exists
                string unevaluatedPropertyValue = await defaultProperties.GetUnevaluatedPropertyValueAsync(BuildEvent).ConfigureAwait(true);
                if (unevaluatedPropertyValue != null)
                {
                    return unevaluatedPropertyValue;
                }

                // Check if build events can be found in targets
                return GetFromTargets(projectXml);
            }

            public async Task SetPropertyAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, ProjectRootElement projectXml)
            {
                // Check if project file already has props in place for this.
                if (await TrySetPropertyAsync(unevaluatedPropertyValue, defaultProperties, projectXml).ConfigureAwait(true))
                {
                    return;
                }

                if (OnlyWhitespaceCharacters(unevaluatedPropertyValue))
                {
                    (bool success, ProjectTargetElement target) = FindTargetToRemove(projectXml);
                    if (success)
                    {
                        projectXml.RemoveChild(target);
                        return;
                    }
                }

                SetParameter(projectXml, unevaluatedPropertyValue);
            }

            private string GetFromTargets(ProjectRootElement projectXml)
            {
                (bool success, ProjectTaskElement execTask) = FindExecTaskInTargets(projectXml);

                if (!success)
                {
                    return null;
                }

                if (execTask.Parameters.TryGetValue(Command, out string commandText))
                {
                    return commandText;
                }

                return null; // exec task as written in the project file is invalid, we should be resilient to this case.
            }

            private async Task<bool> TrySetPropertyAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, ProjectRootElement projectXml)
            {
                string currentValue = await defaultProperties.GetUnevaluatedPropertyValueAsync(BuildEvent).ConfigureAwait(true);
                if (currentValue == null)
                {
                    return false;
                }

                if (OnlyWhitespaceCharacters(unevaluatedPropertyValue))
                {
                    await defaultProperties.DeletePropertyAsync(BuildEvent).ConfigureAwait(true);
                    return true;
                }

                await defaultProperties.SetPropertyValueAsync(BuildEvent, unevaluatedPropertyValue).ConfigureAwait(true);
                return true;
            }

            private static bool OnlyWhitespaceCharacters(string unevaluatedPropertyValue)
                => string.IsNullOrWhiteSpace(unevaluatedPropertyValue) &&
                   !unevaluatedPropertyValue.Contains("\n");

            private (bool success, ProjectTaskElement execTask) FindExecTaskInTargets(ProjectRootElement projectXml)
            {
                ProjectTaskElement execTask = projectXml.Targets
                                    .Where(target =>
                                        StringComparer.OrdinalIgnoreCase.Compare(GetTarget(target), BuildEvent) == 0 &&
                                        StringComparer.OrdinalIgnoreCase.Compare(target.Name, TargetName) == 0)
                                    .SelectMany(target => target.Tasks)
                                    .Where(task => StringComparer.OrdinalIgnoreCase.Compare(task.Name, ExecTask) == 0)
                                    .FirstOrDefault();
                return (success: execTask != null, execTask: execTask);
            }

            private (bool success, ProjectTargetElement target) FindTargetToRemove(ProjectRootElement projectXml)
            {
                ProjectTargetElement foundTarget = projectXml.Targets
                                        .Where(target =>
                                            StringComparer.OrdinalIgnoreCase.Compare(GetTarget(target), BuildEvent) == 0 &&
                                            StringComparer.OrdinalIgnoreCase.Compare(target.Name, TargetName) == 0 &&
                                            target.Children.Count == 1 &&
                                            target.Tasks.Count == 1 &&
                                            StringComparer.OrdinalIgnoreCase.Compare(target.Tasks.First().Name, ExecTask) == 0)
                                        .FirstOrDefault();
                return (success: foundTarget != null, target: foundTarget);
            }

            private void SetParameter(ProjectRootElement projectXml, string unevaluatedPropertyValue)
            {
                (bool success, ProjectTaskElement execTask) result = FindExecTaskInTargets(projectXml);

                if (result.success == true)
                {
                    SetExecParameter(result.execTask, unevaluatedPropertyValue);
                }
                else
                {
                    ProjectTargetElement target = projectXml.AddTarget(TargetName);
                    SetTargetDependencies(target);
                    ProjectTaskElement execTask = target.AddTask(ExecTask);
                    SetExecParameter(execTask, unevaluatedPropertyValue);
                }
            }

            private static void SetExecParameter(ProjectTaskElement execTask, string unevaluatedPropertyValue)
                => execTask.SetParameter(Command, unevaluatedPropertyValue);
        }
    }
}
