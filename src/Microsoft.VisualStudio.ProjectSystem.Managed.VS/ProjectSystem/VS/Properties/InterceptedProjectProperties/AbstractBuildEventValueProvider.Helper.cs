// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using System.Linq;
using Microsoft.Build.Construction;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties.InterceptedProjectProperties
{
    internal abstract partial class AbstractBuildEventValueProvider
    {
        public abstract class Helper
        {
            private const string _execTaskName = "Exec";
            private const string _commandString = "Command";

            protected Helper(string buildEventString,
                   string targetNameString,
                   Func<ProjectTargetElement, string> getTargetString,
                   Action<ProjectTargetElement> setTargetDependencies)
            {
                BuildEventString = buildEventString;
                TargetNameString = targetNameString;
                GetTargetString = getTargetString;
                SetTargetDependencies = setTargetDependencies;
            }

            private Func<ProjectTargetElement, string> GetTargetString { get; }
            private Action<ProjectTargetElement> SetTargetDependencies { get; }
            private string BuildEventString { get; }
            private string TargetNameString { get; }

            public string GetProperty(ProjectRootElement projectXml)
            {
                var result = FindExecTaskInTargets(projectXml);

                if (result.success == false)
                {
                    return null;
                }

                if (result.execTask.Parameters.TryGetValue(_commandString, out var commandText))
                {
                    return commandText;
                }

                return null; //unreachable
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

            private void SetExecParameter(ProjectTaskElement execTask, string unevaluatedPropertyValue)
                => execTask.SetParameter(_commandString, unevaluatedPropertyValue);

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
        }
    }
}
