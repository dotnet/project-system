// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.LogModel.Builder
{
    public sealed class ModelBuilder
    {
        private static readonly Regex UsingTaskRegex = new Regex("Using \"(?<task>.+)\" task from (assembly|the task factory) \"(?<assembly>.+)\"\\.", RegexOptions.Compiled);

        private bool _done;
        private BuildInfo _buildInfo;
        private readonly ConcurrentBag<Exception> _exceptions = new ConcurrentBag<Exception>();
        private Dictionary<int, EvaluationInfo> _evaluationInfos;
        private readonly ConcurrentDictionary<int, ProjectInfo> _projectInfos = new ConcurrentDictionary<int, ProjectInfo>();
        private readonly ConcurrentDictionary<string, string> _assemblies =
            new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _strings = new ConcurrentDictionary<string, string>();

        private readonly object _syncLock = new object();

        public ModelBuilder(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += OnAnyEvent;
        }

        private void HandleException(Exception ex)
        {
            lock (_syncLock)
            {
                _exceptions.Add(ex);
            }
        }

        private void OnAnyEvent(object sender, BuildEventArgs args)
        {
            try
            {
                lock (_syncLock)
                {
                    if (args == null || _done)
                    {
                        throw new LoggerException(Resources.BadState);
                    }

                    switch (args)
                    {
                        case BuildStartedEventArgs buildStartedEventArgs:
                            OnBuildStarted(buildStartedEventArgs);
                            break;

                        case BuildFinishedEventArgs buildFinishedEventArgs:
                            OnBuildFinished(buildFinishedEventArgs);
                            break;

                        case ProjectStartedEventArgs projectStartedEventArgs:
                            OnProjectStarted(projectStartedEventArgs);
                            break;

                        case ProjectFinishedEventArgs projectFinishedEventArgs:
                            OnProjectFinished(projectFinishedEventArgs);
                            break;

                        case TargetStartedEventArgs targetStartedEventArgs:
                            OnTargetStarted(targetStartedEventArgs);
                            break;

                        case TargetFinishedEventArgs targetFinishedEventArgs:
                            OnTargetFinished(targetFinishedEventArgs);
                            break;

                        case TaskStartedEventArgs taskStartedEventArgs:
                            OnTaskStarted(taskStartedEventArgs);
                            break;

                        case TaskFinishedEventArgs taskFinishedEventArgs:
                            OnTaskFinished(taskFinishedEventArgs);
                            break;

                            // This needs to be before BuildMessageEventArgs since it is a subclass
                        case TaskCommandLineEventArgs taskCommandLineEventArgs:
                            OnTaskCommandLine(taskCommandLineEventArgs);
                            break;

                        // This needs to be before BuildMessageEventArgs since it is a subclass
                        case ProjectImportedEventArgs projectImportedEventArgs:
                            OnBuildMessage(sender, projectImportedEventArgs);
                            break;

                        case BuildMessageEventArgs buildMessageEventArgs:
                            OnBuildMessage(sender, buildMessageEventArgs);
                            break;

                        case CustomBuildEventArgs customBuildEventArgs:
                            OnCustomBuildEvent(sender, customBuildEventArgs);
                            break;

                        case ProjectEvaluationStartedEventArgs projectEvaluationStartedEventArgs:
                            OnProjectEvaluationStarted(sender, projectEvaluationStartedEventArgs);
                            break;

                        case ProjectEvaluationFinishedEventArgs projectEvaluationFinishedEventArgs:
                            OnProjectEvaluationFinished(sender, projectEvaluationFinishedEventArgs);
                            break;

                        case BuildErrorEventArgs buildErrorEventArgs:
                            OnBuildError(sender, buildErrorEventArgs);
                            break;

                        case BuildWarningEventArgs buildWarningEventArgs:
                            OnBuildWarning(sender, buildWarningEventArgs);
                            break;

                        default:
                            throw new LoggerException(Resources.UnexpectedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private string Intern(string text)
        {
            if (text == null)
            {
                return null;
            }

            if (text.Length == 0)
            {
                return string.Empty;
            }

            if (_strings.TryGetValue(text.Replace("\r\n", "\n").Replace("\r", "\n"), out var existing))
            {
                return existing;
            }

            _strings[text] = text;
            return text;
        }

        private ProjectInfo FindProjectContext(BuildEventArgs args)
        {
            if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
            {
                throw new LoggerException(Resources.CannotFindProject);
            }

            return projectInfo;
        }

        private TargetInfo FindTargetContext(BuildEventArgs args) => 
            !_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo)
                ? throw new LoggerException(Resources.CannotFindProject)
                : projectInfo.GetTarget(args.BuildEventContext.TargetId);

        private TaskInfo FindTaskContext(BuildEventArgs args)
        {
            if (!_projectInfos.TryGetValue(args.BuildEventContext.ProjectContextId, out var projectInfo))
            {
                throw new LoggerException(Resources.CannotFindProject);
            }

            var targetInfo = projectInfo.GetTarget(args.BuildEventContext.TargetId);

            if (!targetInfo.TaskInfos.TryGetValue(args.BuildEventContext.TaskId, out var taskInfo))
            {
                throw new LoggerException(Resources.CannotFindTask);
            }

            return taskInfo;
        }

        private EvaluationInfo FindEvaluationContext(BuildEventArgs args)
        {
            if (_evaluationInfos == null ||
                !_evaluationInfos.TryGetValue(args.BuildEventContext.EvaluationId, out var evaluationInfo))
            {
                evaluationInfo = new EvaluationInfo();

                if (_evaluationInfos == null)
                {
                    _evaluationInfos = new Dictionary<int, EvaluationInfo>();
                }

                if (_evaluationInfos.ContainsKey(args.BuildEventContext.EvaluationId))
                {
                    throw new LoggerException(Resources.DoubleEvaluation);
                }

                _evaluationInfos[args.BuildEventContext.EvaluationId] = evaluationInfo;
            }

            return evaluationInfo;
        }

        private void OnBuildStarted(BuildStartedEventArgs args)
        {
            if (args.BuildEventContext != null)
            {
                throw new LoggerException(Resources.BadState);
            }

            _buildInfo = new BuildInfo(args.Timestamp, args.BuildEnvironment.ToImmutableDictionary());
            AddMessage(_buildInfo, args);
        }

        private void OnBuildFinished(BuildFinishedEventArgs args)
        {
            if (args.BuildEventContext != null)
            {
                throw new LoggerException(Resources.BadState);
            }

            _buildInfo.EndBuild(args.Timestamp, args.Succeeded);
            AddMessage(_buildInfo, args);
        }

        private static void CheckProjectEventContext(BuildEventArgs args)
        {
            if (args.BuildEventContext.TargetId != -1 ||
                args.BuildEventContext.TaskId != -1 ||
                args.BuildEventContext.EvaluationId != -1)
            {
                throw new LoggerException(Resources.BadState);
            }
        }

        private ItemInfo CreateItemInfo(ITaskItem item) =>
            new ItemInfo(
                Intern(item.ItemSpec),
                item
                    .CloneCustomMetadata()
                    .Cast<KeyValuePair<string, string>>()
                    .Select(metadata => new KeyValuePair<string, string>(Intern(Convert.ToString(metadata.Key)),
                        Intern(Convert.ToString(metadata.Value)))));

        private void OnProjectStarted(ProjectStartedEventArgs args)
        {
            CheckProjectEventContext(args);

            if (args.ParentProjectBuildEventContext.TargetId != -1 ||
                args.ParentProjectBuildEventContext.TaskId != -1 ||
                args.ParentProjectBuildEventContext.EvaluationId != -1)
            {
                throw new LoggerException(Resources.BadState);
            }

            if (_projectInfos.ContainsKey(args.BuildEventContext.ProjectContextId))
            {
                throw new LoggerException(Resources.DoubleCreationOfProject);
            }

            var projectInfo = new ProjectInfo(
                args.BuildEventContext.ProjectContextId,
                args.BuildEventContext.NodeId,
                args.ParentProjectBuildEventContext.ProjectContextId,
                args.Timestamp,
                string.IsNullOrEmpty(args.TargetNames)
                    ? ImmutableHashSet<string>.Empty
                    : args.TargetNames.Split(';').Select(Intern).ToImmutableHashSet(),
                args.ToolsVersion,
                Intern(Path.GetFileName(args.ProjectFile)),
                Intern(args.ProjectFile),
                EmptyIfNull(args.GlobalProperties?
                    .Select(d => new KeyValuePair<string, string>(Intern(d.Key), Intern(d.Value)))
                    .ToImmutableDictionary()),
                EmptyIfNull(args.Properties?
                    .Cast<DictionaryEntry>()
                    .OrderBy(d => d.Key)
                    .Select(d => new KeyValuePair<string, string>(Intern(Convert.ToString(d.Key)), Intern(Convert.ToString(d.Value))))
                    .ToImmutableDictionary()),
                EmptyIfNull(args.Items?.Cast<DictionaryEntry>()
                    .GroupBy(
                        kvp => (string)kvp.Key, 
                        kvp => (ITaskItem)kvp.Value)
                            .Select(
                                group => new ItemGroupInfo(
                                    Intern(group.Key),
                                    group.Select(CreateItemInfo).ToImmutableList()))
                    .ToImmutableList()));

            AddMessage(projectInfo, args);

            _projectInfos[projectInfo.Id] = projectInfo;
        }

        private void OnProjectFinished(ProjectFinishedEventArgs args)
        {
            CheckProjectEventContext(args);

            var projectInfo = FindProjectContext(args);
            projectInfo.EndProject(args.Timestamp, args.Succeeded);
            AddMessage(projectInfo, args);

            foreach (var target in projectInfo.TargetsToBuild)
            {
                var executedTargets = projectInfo.ExecutedTargets.Where(targetInfo =>
                    targetInfo.Name.Equals(target, StringComparison.OrdinalIgnoreCase) &&
                    targetInfo.ParentTarget == null).ToList();

                if (!executedTargets.Any())
                {
                    throw new LoggerException(Resources.CannotFindTarget);
                }

                foreach (var executedTarget in executedTargets)
                {
                    executedTarget.SetIsRequestedTarget();
                }
            }
        }

        private static void CheckTargetEventContext(BuildEventArgs args)
        {
            if (args.BuildEventContext.TaskId != -1 ||
                args.BuildEventContext.EvaluationId != -1)
            {
                throw new LoggerException(Resources.BadState);
            }
        }

        private void OnTargetStarted(TargetStartedEventArgs args)
        {
            CheckTargetEventContext(args);

            var projectInfo = FindProjectContext(args);

            var targetInfo = new TargetInfo(
                args.BuildEventContext.TargetId,
                args.BuildEventContext.NodeId,
                Intern(args.TargetName),
                Intern(args.TargetFile),
                args.ParentTarget,
                args.Timestamp);
            AddMessage(targetInfo, args);

            projectInfo.AddTarget(args.BuildEventContext.TargetId, targetInfo);
            projectInfo.AddExecutedTarget(targetInfo.Name, targetInfo);
        }

        private void OnTargetFinished(TargetFinishedEventArgs args)
        {
            CheckTargetEventContext(args);

            var targetInfo = FindTargetContext(args);
            targetInfo.EndTarget(
                args.Timestamp, 
                args.Succeeded,
                EmptyIfNull(args.TargetOutputs?.Cast<ITaskItem>().Select(CreateItemInfo).ToImmutableList()));
            AddMessage(targetInfo, args);
        }

        private static void CheckTaskEventContext(BuildEventArgs args)
        {
            if (args.BuildEventContext.EvaluationId != -1)
            {
                throw new LoggerException(Resources.BadState);
            }
        }

        private void OnTaskStarted(TaskStartedEventArgs args)
        {
            CheckTaskEventContext(args);

            var targetInfo = FindTargetContext(args);

            var taskInfo = new TaskInfo(
                args.BuildEventContext.TaskId,
                args.BuildEventContext.NodeId,
                Intern(args.TaskName),
                args.Timestamp,
                _assemblies.TryGetValue(Intern(args.TaskName), out var assembly)
                    ? Intern(assembly)
                    : string.Empty,
                Intern(args.TaskFile));
            AddMessage(taskInfo, args);

            targetInfo.AddTask(taskInfo.Id, taskInfo);
        }

        private void OnTaskFinished(TaskFinishedEventArgs args)
        {
            CheckTaskEventContext(args);

            var taskInfo = FindTaskContext(args);
            taskInfo.FinishTask(args.Timestamp, args.Succeeded);
            AddMessage(taskInfo, args);
        }

        private void OnTaskCommandLine(TaskCommandLineEventArgs taskCommandLineEventArgs) =>
            FindTaskContext(taskCommandLineEventArgs).SetCommandLineArguments(Intern(taskCommandLineEventArgs.CommandLine));

        private static string ParseQuotedSubstring(string text)
        {
            var firstQuote = text.IndexOf('"');
            if (firstQuote == -1)
            {
                return text;
            }

            var secondQuote = text.IndexOf('"', firstQuote + 1);
            if (secondQuote == -1)
            {
                return text;
            }

            if (secondQuote - firstQuote < 2)
            {
                return text;
            }

            return text.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }

        private static KeyValuePair<string, string> ParseNameValue(string nameEqualsValue, int trimFromStart = 0)
        {
            var equals = nameEqualsValue.IndexOf('=');
            if (equals == -1)
            {
                return new KeyValuePair<string, string>(nameEqualsValue, "");
            }

            var name = nameEqualsValue.Substring(trimFromStart, equals - trimFromStart);
            var value = nameEqualsValue.Substring(equals + 1);
            return new KeyValuePair<string, string>(name, value);
        }

        private static int GetNumberOfLeadingSpaces(string line)
        {
            var result = 0;
            while (result < line.Length && line[result] == ' ')
            {
                result++;
            }

            return result;
        }

        private KeyValuePair<string, string> ParseProperty(string message, string prefix)
        {
            var nameValue = ParseNameValue(message, trimFromStart: prefix.Length);
            var propertyInfo = new KeyValuePair<string, string>(Intern(nameValue.Key), Intern(nameValue.Value));
            return propertyInfo;
        }

        private static string[] SplitMessage(string message) =>
            message.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        private ItemGroupInfo ParseItemGroupInfo(string message, string prefix) =>
            ParseItemGroupInfo(SplitMessage(message), prefix);

        private ItemGroupInfo ParseItemGroupInfo(string[] lines, string prefix)
        {
            // If no items were produced, we only get one line.
            if (lines[0].Length > prefix.Length)
            {
                var nameValue = ParseNameValue(lines[0].Substring(prefix.Length));

                var name = Intern(nameValue.Key);
                var items = ImmutableList<ItemInfo>.Empty;

                if (!string.IsNullOrEmpty(nameValue.Value))
                {
                    items = items.Add(new ItemInfo(nameValue.Value));
                }

                if (lines.Length > 1)
                {
                    throw new LoggerException(Resources.UnexpectedMessage);
                }

                return new ItemGroupInfo(name, items);
            }

            string currentGroupName = null;
            var currentItems = ImmutableList<ItemInfo>.Empty;
            string currentItemName = null;
            var currentMetadata = new List<KeyValuePair<string, string>>();
            string currentMetadataKey = null;
            string currentMetadataValue = null;

            foreach (var line in lines)
            {
                var numberOfLeadingSpaces = GetNumberOfLeadingSpaces(line);
                switch (numberOfLeadingSpaces)
                {
                    case 4:
                        if (!line.EndsWith("=", StringComparison.Ordinal))
                        {
                            throw new LoggerException(Resources.ExpectedItemGroupName);
                        }

                        if (currentGroupName != null)
                        {
                            throw new LoggerException(Resources.UnexpectedMessage);
                        }

                        currentGroupName = Intern(line.Substring(4, line.Length - 5));
                        break;

                    case 8:
                        if (currentGroupName == null)
                        {
                            throw new LoggerException(Resources.CannotFindItem);
                        }

                        if (currentMetadataKey != null)
                        {
                            currentMetadata.Add(new KeyValuePair<string, string>(currentMetadataKey, currentMetadataValue));
                        }

                        if (currentItemName != null)
                        {
                            currentItems = currentItems.Add(new ItemInfo(currentItemName, currentMetadata));
                        }

                        currentItemName = Intern(line.Substring(8));
                        currentMetadata.Clear();
                        currentMetadataKey = null;
                        currentMetadataValue = null;
                        break;

                    case 16:
                        var currentLine = line.Substring(16);

                        if (currentItemName == null)
                        {
                            throw new LoggerException(Resources.CannotFindItem);
                        }

                        if (!currentLine.Contains("="))
                        {
                            if (currentMetadataKey == null)
                            {
                                throw new LoggerException(Resources.BadMetadataContinuation);
                            }

                            currentMetadataValue = Intern((currentMetadataValue ?? "") + line);
                        }
                        else
                        {
                            if (currentMetadataKey != null)
                            {
                                currentMetadata.Add(new KeyValuePair<string, string>(currentMetadataKey, currentMetadataValue));
                            }
                            var nameValue = ParseNameValue(currentLine);
                            currentMetadataKey = Intern(nameValue.Key);
                            currentMetadataValue = Intern(nameValue.Value);
                        }
                        break;

                    default:
                        if (numberOfLeadingSpaces == 0 && line == prefix)
                        {
                            continue;
                        }

                        if (currentMetadataKey != null)
                        {
                            currentMetadataValue = Intern((currentMetadataValue ?? "") + line);
                        }
                        else if (currentItemName != null)
                        {
                            currentItemName = Intern(currentItemName + line);
                        }
                        else
                        {
                            throw new LoggerException(Resources.BadMetadataContinuation);
                        }
                        break;
                }
            }

            if (currentMetadataKey != null)
            {
                currentMetadata.Add(new KeyValuePair<string, string>(currentMetadataKey, currentMetadataValue));
            }

            if (currentItemName != null)
            {
                currentItems = currentItems.Add(new ItemInfo(currentItemName, currentMetadata));
            }

            return new ItemGroupInfo(currentGroupName, currentItems);
        }

        private void ProcessProjectMessage(BuildEventArgs args)
        {
            var message = args.Message;
            var projectInfo = FindProjectContext(args);

            if (message.StartsWith("Target") && message.Contains("skipped"))
            {
                var targetName = Intern(ParseQuotedSubstring(message));
                if (targetName == null)
                {
                    throw new LoggerException(Resources.UnexpectedMessage);
                }

                var targetInfo = new TargetInfo(targetName, args.Timestamp);
                AddMessage(targetInfo, args);
                projectInfo.AddExecutedTarget(targetName, targetInfo);
                return;
            }

            AddMessage(projectInfo, args);
        }

        private void ProcessTargetMessage(BuildEventArgs args)
        {
            const string itemGroupIncludeMessagePrefix = @"Added Item(s): ";
            const string itemGroupRemoveMessagePrefix = @"Removed Item(s): ";
            const string propertyGroupMessagePrefix = @"Set Property: ";

            var message = args.Message;
            var targetInfo = FindTargetContext(args);

            if (message.StartsWith("Using"))
            {
                // A task from assembly message (parses out the task name and assembly path).
                var match = UsingTaskRegex.Match(args.Message);
                if (match.Success)
                {
                    var taskName = Intern(match.Groups["task"].Value);
                    var assembly = Intern(match.Groups["assembly"].Value);
                    _assemblies.GetOrAdd(taskName, t => assembly);
                }
            }

            if (message.StartsWith(itemGroupIncludeMessagePrefix))
            {
                var itemGroupInfo = ParseItemGroupInfo(args.Message, itemGroupIncludeMessagePrefix);
                var itemActionInfo = new ItemActionInfo(true, itemGroupInfo, args.Timestamp);
                AddMessage(itemActionInfo, args);
                targetInfo.AddItemAction(itemActionInfo);
                return;
            }

            if (message.StartsWith(itemGroupRemoveMessagePrefix))
            {
                var itemGroupInfo = ParseItemGroupInfo(args.Message, itemGroupRemoveMessagePrefix);
                var itemActionInfo = new ItemActionInfo(false, itemGroupInfo, args.Timestamp);
                AddMessage(itemActionInfo, args);
                targetInfo.AddItemAction(itemActionInfo);
                return;
            }

            if (message.StartsWith(propertyGroupMessagePrefix))
            {
                message = args.Message.Substring(propertyGroupMessagePrefix.Length);

                var kvp = ParseNameValue(message);
                var propertySetInfo = new PropertySetInfo(kvp.Key, kvp.Value, args.Timestamp);
                AddMessage(propertySetInfo, args);
                targetInfo.AddPropertySet(propertySetInfo);
                return;
            }

            if (message.StartsWith("Task") && message.Contains("skipped"))
            {
                var taskName = Intern(ParseQuotedSubstring(message));
                if (taskName == null)
                {
                    throw new LoggerException(Resources.UnexpectedMessage);
                }

                var taskInfo = new TaskInfo(taskName, args.Timestamp);
                AddMessage(taskInfo, args);
                targetInfo.AddExecutedTask(taskInfo);
                return;
            }

            AddMessage(targetInfo, args);
        }

        private void ProcessTaskMessage(BuildEventArgs args)
        {
            const string taskParameterMessagePrefix = @"Task Parameter:";
            const string outputItemsMessagePrefix = @"Output Item(s): ";
            const string outputPropertyMessagePrefix = @"Output Property: ";

            var taskInfo = FindTaskContext(args);
            var message = args.Message;

            if (message.StartsWith(outputItemsMessagePrefix))
            {
                taskInfo.AddOutputItems(ParseItemGroupInfo(message, outputItemsMessagePrefix));
                AddMessage(taskInfo, args);
                return;
            }

            if (message.StartsWith(outputPropertyMessagePrefix))
            {
                var property = ParseProperty(message, outputPropertyMessagePrefix);
                taskInfo.AddOutputProperty(property.Key, property.Value);
                AddMessage(taskInfo, args);
                return;
            }

            if (message.StartsWith(taskParameterMessagePrefix))
            {
                if (message.IndexOf('\n') != taskParameterMessagePrefix.Length)
                {
                    var property = ParseProperty(message, taskParameterMessagePrefix);
                    taskInfo.AddParameterProperty(property.Key, property.Value);
                }
                else
                {
                    taskInfo.AddParameterItems(ParseItemGroupInfo(message, taskParameterMessagePrefix));
                }

                AddMessage(taskInfo, args);
                return;
            }

            AddMessage(taskInfo, args);
        }

        private void ProcessEvaluationMessage(BuildEventArgs args)
        {
            if (args.BuildEventContext.ProjectContextId != BuildEventContext.InvalidProjectContextId ||
                args.BuildEventContext.TargetId != BuildEventContext.InvalidTargetId ||
                args.BuildEventContext.TaskId != BuildEventContext.InvalidTaskId)
            {
                throw new LoggerException(Resources.UnexpectedMessage);
            }

            var evaluationInfo = FindEvaluationContext(args);

            evaluationInfo.AddMessage(args.Message, args.Timestamp);
        }

        private void ProcessMessage(BuildEventArgs args)
        {
            if (string.IsNullOrEmpty(args?.Message))
            {
                return;
            }

            if (args.BuildEventContext != null)
            {
                if (args.BuildEventContext.EvaluationId != BuildEventContext.InvalidEvaluationId)
                {
                    ProcessEvaluationMessage(args);
                    return;
                }

                if (args.BuildEventContext.TaskId == 0 &&
                    args.BuildEventContext.TargetId == 0 &&
                    args.BuildEventContext.ProjectContextId == 0)
                {
                    // Build summary message
                    AddMessage(_buildInfo, args);
                    return;
                }

                if (args.BuildEventContext.TaskId != BuildEventContext.InvalidTaskId)
                {
                    ProcessTaskMessage(args);
                    return;
                }

                if (args.BuildEventContext.TargetId != BuildEventContext.InvalidTargetId)
                {
                    ProcessTargetMessage(args);
                    return;
                }

                if (args.BuildEventContext.ProjectContextId != BuildEventContext.InvalidProjectContextId)
                {
                    ProcessProjectMessage(args);
                    return;
                }
            }

            AddMessage(_buildInfo, args);
        }

        private void AddMessage(BaseInfo info, BuildEventArgs args) => 
            info.AddMessage(Intern(args.Message), args.Timestamp);

        private void OnBuildMessage(object sender, BuildMessageEventArgs args) => 
            ProcessMessage(args);

        private void OnCustomBuildEvent(object sender, CustomBuildEventArgs args) => 
            ProcessMessage(new BuildMessageEventArgs(
                Intern(args.Message),
                Intern(args.HelpKeyword),
                Intern(args.SenderName),
                MessageImportance.Low));

        private void OnProjectEvaluationStarted(object sender, ProjectEvaluationStartedEventArgs args)
        {
            var evaluationInfo = FindEvaluationContext(args);
            var evaluatedProject = new EvaluatedProjectInfo(args.ProjectFile, args.Timestamp);
            AddMessage(evaluatedProject, args);
            evaluationInfo.StartEvaluatingProject(evaluatedProject);
        }

        private void OnProjectEvaluationFinished(object sender, ProjectEvaluationFinishedEventArgs args)
        {
            var evaluationInfo = FindEvaluationContext(args);
            var evaluatedProjectInfo = evaluationInfo.EndEvaluatingProject(args.ProjectFile);
            evaluatedProjectInfo.EndEvaluatedProject(args.Timestamp);
            AddMessage(evaluatedProjectInfo, args);
        }

        private BaseInfo FindMessageContext(BuildEventArgs buildEventArgs)
        {
            if (buildEventArgs.BuildEventContext.TaskId != BuildEventContext.InvalidTaskId)
            {
                return FindTaskContext(buildEventArgs);
            }

            if (buildEventArgs.BuildEventContext.TargetId != BuildEventContext.InvalidTargetId)
            {
                return FindTargetContext(buildEventArgs);
            }

            if (buildEventArgs.BuildEventContext.ProjectContextId != BuildEventContext.InvalidProjectContextId)
            {
                return FindProjectContext(buildEventArgs);
            }

            return _buildInfo;
        }

        private void OnBuildWarning(object sender, BuildWarningEventArgs args)
        {
            var info = FindMessageContext(args);

            var warning = new DiagnosticInfo(
                false, 
                Intern(args.Message), 
                args.Timestamp, 
                Intern(args.Code),
                args.ColumnNumber, 
                args.EndColumnNumber, 
                args.LineNumber, 
                args.EndLineNumber, 
                Intern(args.File),
                Intern(args.ProjectFile), 
                Intern(args.Subcategory));

            info.AddMessage(warning);
        }

        private void OnBuildError(object sender, BuildErrorEventArgs args)
        {
            var info = FindMessageContext(args);

            var error = new DiagnosticInfo(
                true, 
                Intern(args.Message), 
                args.Timestamp, 
                Intern(args.Code),
                args.ColumnNumber, 
                args.EndColumnNumber, 
                args.LineNumber, 
                args.EndLineNumber, 
                Intern(args.File),
                Intern(args.ProjectFile), 
                Intern(args.Subcategory));

            info.AddMessage(error);
        }

        private static string OrderItems(Item item) => item.Name;

        private static DateTime OrderTasks(Task task) => task.StartTime;

        private static string OrderItemGroups(ItemGroup itemGroup) => itemGroup.Name;

        private static DateTime OrderItemActions(ItemAction itemAction) => itemAction.Time;

        private static DateTime OrderPropertySets(PropertySet propertySet) => propertySet.Time;

        private static DateTime OrderNodes<T>(T node) where T : Node => node.StartTime;

        private static DateTime OrderMessages(Message message) => message.Timestamp;

        private static ImmutableDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> values) =>
            values == null
                ? ImmutableDictionary<TKey, TValue>.Empty 
                : values.ToImmutableDictionary();

        private static ImmutableList<T> EmptyIfNull<T>(IEnumerable<T> values) =>
            values == null
                ? ImmutableList<T>.Empty
                : values.ToImmutableList();

        private static Item ConstructItem(ItemInfo itemInfo) =>
            new Item(
                itemInfo.Name,
                itemInfo.Metadata.ToImmutableDictionary());

        private static ItemGroup ConstructItemGroup(ItemGroupInfo itemGroupInfo) =>
            new ItemGroup(
                itemGroupInfo.Name,
                EmptyIfNull(itemGroupInfo.Items?.Select(ConstructItem).OrderBy(OrderItems).ToImmutableList()));

        private static ItemAction ConstructItemAction(ItemActionInfo itemActionInfo) =>
            new ItemAction(
                itemActionInfo.IsAddition,
                ConstructItemGroup(itemActionInfo.ItemGroup),
                itemActionInfo.Time);

        private static PropertySet ConstructPropertySet(PropertySetInfo propertySetInfo) =>
            new PropertySet(
                propertySetInfo.Name,
                propertySetInfo.Value,
                propertySetInfo.Time);

        private static Message ConstructMessage(MessageInfo messageInfo) => 
            messageInfo is DiagnosticInfo diagnosticInfo
                ? new Diagnostic(
                    diagnosticInfo.IsError,
                    diagnosticInfo.Text,
                    diagnosticInfo.Timestamp,
                    diagnosticInfo.Code,
                    diagnosticInfo.ColumnNumber,
                    diagnosticInfo.EndColumnNumber,
                    diagnosticInfo.LineNumber,
                    diagnosticInfo.EndLineNumber,
                    diagnosticInfo.File,
                    diagnosticInfo.ProjectFile,
                    diagnosticInfo.Subcategory)
                : new Message(messageInfo.Timestamp, messageInfo.Text);

        private static Task ConstructTask(TaskInfo taskInfo) =>
            new Task(
                taskInfo.NodeId,
                taskInfo.Name,
                taskInfo.FromAssembly,
                taskInfo.CommandLineArguments,
                taskInfo.SourceFilePath,
                EmptyIfNull(taskInfo.ChildProjectInfos?.Select(ConstructProject).OrderBy(OrderNodes).ToImmutableList()),
                EmptyIfNull(taskInfo.ParameterItems?.Select(ConstructItemGroup).ToImmutableList()),
                EmptyIfNull(taskInfo.ParameterProperties?.ToImmutableDictionary()),
                EmptyIfNull(taskInfo.OutputItems?.Select(ConstructItemGroup).ToImmutableList()),
                EmptyIfNull(taskInfo.OutputProperties?.ToImmutableDictionary()),
                taskInfo.StartTime,
                taskInfo.EndTime,
                taskInfo.Messages.Select(ConstructMessage).OrderBy(OrderMessages).ToImmutableList(),
                taskInfo.Result
            );

        private static Target ConstructTarget(TargetInfo targetInfo) =>
            new Target(
                targetInfo.NodeId,
                targetInfo.Name,
                targetInfo.IsRequestedTarget,
                targetInfo.SourceFilePath,
                targetInfo.ParentTarget,
                EmptyIfNull(targetInfo.ItemActionInfos?.Select(ConstructItemAction).OrderBy(OrderItemActions).ToImmutableList()),
                EmptyIfNull(targetInfo.PropertySetInfos?.Select(ConstructPropertySet).OrderBy(OrderPropertySets).ToImmutableList()),
                EmptyIfNull(targetInfo.OutputItems?.Select(ConstructItem).OrderBy(OrderItems).ToImmutableList()),
                EmptyIfNull(targetInfo.TaskInfos?.Values.Select(ConstructTask).OrderBy(OrderTasks).ToImmutableList()),
                targetInfo.StartTime,
                targetInfo.EndTime,
                targetInfo.Messages.Select(ConstructMessage).OrderBy(OrderMessages).ToImmutableList(),
                targetInfo.Result
            );

        private static EvaluatedProject ConstructEvaluatedProject(EvaluatedProjectInfo evaluatedProjectInfo) =>
            new EvaluatedProject(
                evaluatedProjectInfo.Name,
                evaluatedProjectInfo.StartTime,
                evaluatedProjectInfo.EndTime,
                evaluatedProjectInfo.Messages.Select(ConstructMessage).OrderBy(OrderMessages).ToImmutableList()
            );

        private static Evaluation ConstructEvaluation(EvaluationInfo evaluationInfo) =>
            new Evaluation(
                EmptyIfNull(evaluationInfo.Messages?.Select(ConstructMessage).OrderBy(OrderMessages).ToImmutableList()),
                EmptyIfNull(evaluationInfo.EvaluatedProjects?.Select(ConstructEvaluatedProject).OrderBy(OrderNodes).ToImmutableList())
            );

        private static Project ConstructProject(ProjectInfo projectInfo) =>
            new Project(
                projectInfo.NodeId,
                projectInfo.Name,
                projectInfo.ProjectFile,
                projectInfo.GlobalProperties,
                projectInfo.Properties,
                projectInfo.ItemGroups?.Select(ConstructItemGroup).OrderBy(OrderItemGroups).ToImmutableList() ?? ImmutableList<ItemGroup>.Empty,
                projectInfo.ExecutedTargets.Select(ConstructTarget).ToImmutableList(),
                projectInfo.ToolsVersion,
                EmptyIfNull(projectInfo.Messages?.Select(ConstructMessage).OrderBy(OrderMessages).ToImmutableList()),
                projectInfo.StartTime,
                projectInfo.EndTime,
                projectInfo.Result);

        private Build ConstructBuild() =>
            new Build(ConstructProject(_projectInfos.Values.Single(p => p.ParentProject == BuildEventContext.InvalidProjectContextId)),
                _buildInfo.Environment,
                EmptyIfNull(_buildInfo.Messages?.Select(ConstructMessage).OrderBy(OrderMessages).ToImmutableList()), 
                _buildInfo.StartTime, 
                _buildInfo.EndTime, 
                _buildInfo.Result);

        private void ConnectBuildTasks()
        {
            foreach (var projectInfo in _projectInfos.Values.Where(projectInfo => projectInfo.ParentProject != BuildEventContext.InvalidProjectContextId))
            {
                var parentProjectInfo = _projectInfos[projectInfo.ParentProject];
                var parentDirectoryName = Path.GetDirectoryName(parentProjectInfo.ProjectFile) ?? "";
                TaskInfo parentTask = null;

                var tasks = parentProjectInfo.ExecutedTargets
                    .Where(target => target.TaskInfos != null)
                    .SelectMany(target => target.TaskInfos.Values).ToArray();

                if (Path.GetExtension(projectInfo.ProjectFile) == ".tmp_proj")
                {
                    parentTask = tasks.SingleOrDefault(task => task.Name == "GenerateTemporaryTargetAssembly");
                }

                if (parentTask != null)
                {
                    parentTask.AddChildProject(projectInfo);
                    return;
                }

                var msBuildTasks = tasks.Where(task => task.Name == "MSBuild");

                foreach (var taskInfo in msBuildTasks)
                {
                    var targets = taskInfo.GetTaskParameter("Targets");

                    if (taskInfo
                        .GetTaskParameter("Projects")
                        .Select(project =>
                            Path.IsPathRooted(project)
                                ? project
                                : Path.GetFullPath(
                                    Path.Combine(parentDirectoryName, project)))
                        .Any(project =>
                            project == projectInfo.ProjectFile &&
                            projectInfo.TargetsToBuild.SetEquals(targets)))
                    {
                        parentTask = taskInfo;
                        break;
                    }
                }

                if (parentTask == null)
                {
                    throw new LoggerException(Resources.CannotFindTask);
                }

                parentTask.AddChildProject(projectInfo);
            }
        }

        public Log Finish()
        {
            Build build = null;
            var evaluations = ImmutableList<Evaluation>.Empty;

            try
            {
                if (_done)
                {
                    throw new LoggerException(Resources.BadState);
                }

                _done = true;

                if (_evaluationInfos != null)
                {
                    evaluations = _evaluationInfos.Values.Select(ConstructEvaluation).ToImmutableList();
                }

                if (_buildInfo != null)
                {
                    ConnectBuildTasks();
                    build = ConstructBuild();
                }
            }
            catch (Exception ex)
            {
                _exceptions.Add(ex);
            }

            if (_exceptions.Any())
            {
                throw new AggregateException(_exceptions);
            }

            return new Log(build, evaluations);
        }
    }
}