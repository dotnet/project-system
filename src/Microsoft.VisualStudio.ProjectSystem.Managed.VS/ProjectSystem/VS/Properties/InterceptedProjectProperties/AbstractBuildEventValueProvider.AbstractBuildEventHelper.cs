// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Construction;

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

            public string GetProperty(ProjectRootElement projectXml)
            {
                // Check if project file already has props in place for this.
                var result = TryGetFromProperty(projectXml);
                if (result.success)
                {
                    return result.property;
                }

                // Check if build events can be found in targets
                return GetFromTargets(projectXml);
            }

            public void SetProperty(string unevaluatedPropertyValue, ProjectRootElement projectXml)
            {
                // Check if project file already has props in place for this.
                if (TrySetProperty(unevaluatedPropertyValue, projectXml))
                {
                    return;
                }

                if (OnlyWhitespaceCharacters(unevaluatedPropertyValue))
                {
                    var result = FindTargetToRemove(projectXml);
                    if (result.success)
                    {
                        projectXml.RemoveChild(result.target);
                        return;
                    }
                }

                SetParameter(projectXml, unevaluatedPropertyValue);
            }

            private (bool success, string property) TryGetFromProperty(ProjectRootElement projectXml)
            {
                // Choose last or default as that matches the evaluation order for MSBuild
                var property = projectXml.Properties
                    .Where(prop => StringComparer.OrdinalIgnoreCase.Compare(prop.Name, BuildEvent) == 0)
                    .LastOrDefault();
                return (success: property != null, property?.Value);
            }

            private string GetFromTargets(ProjectRootElement projectXml)
            {
                var result = FindExecTaskInTargets(projectXml);

                if (!result.success)
                {
                    return null;
                }

                if (result.execTask.Parameters.TryGetValue(Command, out var commandText))
                {
                    return commandText;
                }

                return null; // exec task as written in the project file is invalid, we should be resilient to this case.
            }

            private bool TrySetProperty(string unevaluatedPropertyValue, ProjectRootElement projectXml)
            {
                // Choose last or default as that matches the evaluation order for MSBuild
                var property = projectXml.Properties
                    .Where(prop => StringComparer.OrdinalIgnoreCase.Compare(prop.Name, BuildEvent) == 0)
                    .LastOrDefault();
                if (property == null)
                {
                    return false;
                }

                if (OnlyWhitespaceCharacters(unevaluatedPropertyValue))
                {
                    projectXml.RemoveChild(property);
                    return true;
                }

                property.Value = unevaluatedPropertyValue;
                return true;
            }

            private static bool OnlyWhitespaceCharacters(string unevaluatedPropertyValue)
                => string.IsNullOrWhiteSpace(unevaluatedPropertyValue) &&
                   !unevaluatedPropertyValue.Contains("\n");

            private (bool success, ProjectTaskElement execTask) FindExecTaskInTargets(ProjectRootElement projectXml)
            {
                var execTask = projectXml.Targets
                                    .Where(target => StringComparer.OrdinalIgnoreCase.Compare(GetTarget(target), BuildEvent) == 0)
                                    .SelectMany(target => target.Tasks)
                                    .Where(task => StringComparer.OrdinalIgnoreCase.Compare(task.Name, ExecTask) == 0)
                                    .FirstOrDefault();
                return (success: execTask != null, execTask: execTask);
            }

            private (bool success, ProjectTargetElement target) FindTargetToRemove(ProjectRootElement projectXml)
            {
                var foundTarget = projectXml.Targets
                                        .Where(target =>
                                            StringComparer.OrdinalIgnoreCase.Compare(GetTarget(target), BuildEvent) == 0 &&
                                            target.Children.Count == 1 &&
                                            target.Tasks.Count == 1 &&
                                            StringComparer.OrdinalIgnoreCase.Compare(target.Tasks.First().Name, ExecTask) == 0)
                                        .FirstOrDefault();
                return (success: foundTarget != null, target: foundTarget);
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
                    var execTask = target.AddTask(ExecTask);
                    SetExecParameter(execTask, unevaluatedPropertyValue);
                }
            }

            private void SetExecParameter(ProjectTaskElement execTask, string unevaluatedPropertyValue)
                => execTask.SetParameter(Command, unevaluatedPropertyValue);

            private string GetTargetName(ProjectRootElement projectXml)
            {
                var targetNames = new HashSet<string>(projectXml.Targets.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
                var targetName = TargetName;
                if (targetNames.Contains(targetName))
                {
                    targetName = FindNonCollidingName(targetName, targetNames);
                }

                return targetName;

            }

            private string FindNonCollidingName(string buildEvent, HashSet<string> targetNames)
            {
                var initialValue = 0;
                var newName = string.Empty;

                do
                {
                    initialValue++;
                    newName = buildEvent + initialValue.ToString();
                } while (targetNames.Contains(newName));

                return newName;
            }
        }
    }
}
