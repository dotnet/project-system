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
            private const string _execTask = "Exec";
            private const string _command = "Command";

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
                var result = FindExecTaskInTargets(projectXml);

                if (!result.success)
                {
                    return null;
                }

                if (result.execTask.Parameters.TryGetValue(_command, out var commandText))
                {
                    return commandText;
                }

                return null; // exec task as written in the project file is invalid, we should be resilient to this case.
            }

            public void SetProperty(string unevaluatedPropertyValue, ProjectRootElement projectXml)
            {
                if (string.IsNullOrWhiteSpace(unevaluatedPropertyValue) &&
                    !unevaluatedPropertyValue.Contains("\n"))
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

            private (bool success, ProjectTaskElement execTask) FindExecTaskInTargets(ProjectRootElement projectXml)
            {
                var execTask = projectXml.Targets
                                    .Where(target => StringComparer.OrdinalIgnoreCase.Compare(GetTarget(target), BuildEvent) == 0)
                                    .SelectMany(target => target.Tasks)
                                    .Where(task => StringComparer.OrdinalIgnoreCase.Compare(task.Name, _execTask) == 0)
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
                                            StringComparer.OrdinalIgnoreCase.Compare(target.Tasks.First().Name, _execTask) == 0)
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
                    var execTask = target.AddTask(_execTask);
                    SetExecParameter(execTask, unevaluatedPropertyValue);
                }
            }

            private void SetExecParameter(ProjectTaskElement execTask, string unevaluatedPropertyValue)
                => execTask.SetParameter(_command, unevaluatedPropertyValue);

            private string GetTargetName(ProjectRootElement projectXml)
            {
                var targetNames = new HashSet<string>(projectXml.Targets.Select(t => t.Name));
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
