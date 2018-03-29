// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    [Export(typeof(IProjectChangeHintReceiver))]
    [Export(typeof(OrderAddItemHintReceiver))]
    [ProjectChangeHintKind(ProjectChangeFileSystemEntityHint.AddedFileAsString)]
    [AppliesTo(ProjectCapability.SortByDisplayOrder)]
    internal class OrderAddItemHintReceiver : IProjectChangeHintReceiver
    {
        private readonly IProjectAccessor _accessor;

        private ImmutableHashSet<string> _previousIncludes = ImmutableHashSet<string>.Empty;
        private OrderAddItemHintReceiverAction _action = OrderAddItemHintReceiverAction.NoOp;
        private IProjectTree _target = null;

        [ImportingConstructor]
        public OrderAddItemHintReceiver(IProjectAccessor accessor)
        {
            Requires.NotNull(accessor, nameof(accessor));

            _accessor = accessor;
        }

        public async Task HintedAsync(IImmutableDictionary<Guid, IImmutableSet<IProjectChangeHint>> hints)
        {
            if (CanMove() && !_previousIncludes.IsEmpty && hints.ContainsKey(ProjectChangeFileSystemEntityHint.AddedFile))
            {
                var hint = hints[ProjectChangeFileSystemEntityHint.AddedFile].First();
                var configuredProject = hint.UnconfiguredProject.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;

                await _accessor.OpenProjectForWriteAsync(configuredProject, project =>
                {
                    // We do a sanity re-evaluation to absolutely ensure changes were met.
                    project.ReevaluateIfNecessary();
                    var addedElements = GetAddedElements(_previousIncludes, project);

                    switch (_action)
                    {
                        case OrderAddItemHintReceiverAction.MoveToTop:
                            OrderingHelper.TryMoveElementsToTop(project, addedElements, _target);
                            break;
                        case OrderAddItemHintReceiverAction.MoveAbove:
                            OrderingHelper.TryMoveElementsAbove(project, addedElements, _target);
                            break;
                        case OrderAddItemHintReceiverAction.MoveBelow:
                            OrderingHelper.TryMoveElementsBelow(project, addedElements, _target);
                            break;
                        default:
                            break;
                    }
                }).ConfigureAwait(false);
            }

            // Reset everything because we are done.
            // We need to make sure these are all reset so we can listen for changes again.
            Reset();
        }

        public async Task HintingAsync(IProjectChangeHint hint)
        {
            // This will only be called once even if you are adding multiple files from say, e.g. add existing item dialog
            // However we check to see if we captured the previous includes for sanity to ensure it only gets set once.
            if (CanMove() && _previousIncludes.IsEmpty)
            {
                var configuredProject = hint.UnconfiguredProject.Services.ActiveConfiguredProjectProvider.ActiveConfiguredProject;

                _previousIncludes = await _accessor.OpenProjectForReadAsync(configuredProject, project =>
                    project.AllEvaluatedItems.Select(x => x.EvaluatedInclude).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// When the task runs, if the receiver picks up that we will be adding an item, it will capture the MSBuild project's includes.
        /// If any items were added as a result of the task running, the hint receiver will perform the specified action on those items.
        /// </summary>
        public async Task Capture(OrderAddItemHintReceiverAction action, IProjectTree target, Func<Task> task)
        {
            Requires.NotNull(target, nameof(target));
            Requires.NotNull(task, nameof(task));

            _action = action;
            _target = target;
            await task().ConfigureAwait(false);
            Reset();
        }

        private void Reset()
        {
            _action = OrderAddItemHintReceiverAction.NoOp;
            _target = null;
            _previousIncludes = ImmutableHashSet<string>.Empty;
        }

        private bool CanMove()
        {
            return _action != OrderAddItemHintReceiverAction.NoOp && _target != null;
        }

        private static ImmutableArray<ProjectItemElement> GetAddedElements(ImmutableHashSet<string> previousIncludes, Project project)
        {
            return project.AllEvaluatedItems
                .Where(x => !previousIncludes.Contains(x.EvaluatedInclude, StringComparer.OrdinalIgnoreCase))
                .Select(x => x.Xml)
                .ToImmutableArray();
        }
    }
}
