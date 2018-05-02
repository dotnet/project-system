// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Input.Commands.Ordering
{
    /// <summary>
    /// Helper methods to interact with a project tree that have items with a valid display order.
    /// </summary>
    internal static class OrderingHelper
    {
        /// <summary>
        /// Performs a move on any items that were added based on the previous includes.
        /// </summary>
        public static Task Move(ConfiguredProject configuredProject, IProjectAccessor accessor, ImmutableHashSet<string> previousIncludes, IProjectTree target, OrderingMoveAction action)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(accessor, nameof(accessor));
            Requires.NotNull(previousIncludes, nameof(previousIncludes));
            Requires.NotNull(target, nameof(target));

            return accessor.OpenProjectForWriteAsync(configuredProject, project =>
            {
                // We do a sanity re-evaluation to absolutely ensure changes were met.
                project.ReevaluateIfNecessary();
                ImmutableArray<ProjectItemElement> addedElements = GetAddedItemElements(previousIncludes, project);

                switch (action)
                {
                    case OrderingMoveAction.MoveToTop:
                        TryMoveElementsToTop(project, addedElements, target);
                        break;
                    case OrderingMoveAction.MoveAbove:
                        TryMoveElementsAbove(project, addedElements, target);
                        break;
                    case OrderingMoveAction.MoveBelow:
                        TryMoveElementsBelow(project, addedElements, target);
                        break;
                    default:
                        break;
                }
            });
        }

        /// <summary>
        /// Get all evaluated includes from a project as an immutable hash set. This includes items that aren't for ordering as well.
        /// </summary>
        public static Task<ImmutableHashSet<string>> GetAllEvaluatedIncludes(ConfiguredProject configuredProject, IProjectAccessor accessor)
        {
            Requires.NotNull(configuredProject, nameof(configuredProject));
            Requires.NotNull(accessor, nameof(accessor));

            return accessor.OpenProjectForReadAsync(configuredProject, project =>
                project.AllEvaluatedItems.Select(x => x.EvaluatedInclude).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks to see if the project tree has a valid display order.
        /// </summary>
        public static bool HasValidDisplayOrder(IProjectTree projectTree)
        {
            return IsValidDisplayOrder(GetDisplayOrder(projectTree));
        }

        /// <summary>
        /// Gets the display order for a project tree.
        /// </summary>
        public static int GetDisplayOrder(IProjectTree projectTree)
        {
            if (projectTree is IProjectTree2 projectTree2)
            {
                return projectTree2.DisplayOrder;
            }
            // It's safe to return zero here. Project trees that do not have a display order are always assumed zero.
            return 0;
        }

        /// <summary>
        /// Checks if the given project tree can move up over one of its siblings.
        /// </summary>
        public static bool CanMoveUp(IProjectTree projectTree)
        {
            Requires.NotNull(projectTree, nameof(projectTree));

            return GetSiblingByMoveAction(projectTree, MoveAction.Above) != null;
        }

        /// <summary>
        /// Move the project tree up over one of its siblings.
        /// </summary>
        public static bool TryMoveUp(Project project, IProjectTree projectTree)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(projectTree, nameof(projectTree));

            return TryMove(project, projectTree, MoveAction.Above);
        }

        /// <summary>
        /// Checks if the given project tree can move down over one of its siblings.
        /// </summary>
        public static bool CanMoveDown(IProjectTree projectTree)
        {
            Requires.NotNull(projectTree, nameof(projectTree));

            return GetSiblingByMoveAction(projectTree, MoveAction.Below) != null;
        }

        /// <summary>
        /// Move the project tree down over one of its siblings.
        /// </summary>
        public static bool TryMoveDown(Project project, IProjectTree projectTree)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(projectTree, nameof(projectTree));

            return TryMove(project, projectTree, MoveAction.Below);
        }

        /// <summary>
        /// Move the respective item elements above the target.
        /// </summary>
        public static bool TryMoveElementsAbove(Project project, ImmutableArray<ProjectItemElement> elements, IProjectTree target)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(target, nameof(target));

            ProjectItemElement referenceElement = TryGetReferenceElement(project, target, ImmutableArray<string>.Empty, MoveAction.Above);
            return TryMoveElements(elements, referenceElement, MoveAction.Above);
        }

        /// <summary>
        /// Move the respective item elements below the target.
        /// </summary>
        public static bool TryMoveElementsBelow(Project project, ImmutableArray<ProjectItemElement> elements, IProjectTree target)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(target, nameof(target));

            ProjectItemElement referenceElement = TryGetReferenceElement(project, target, ImmutableArray<string>.Empty, MoveAction.Below);
            return TryMoveElements(elements, referenceElement, MoveAction.Below);
        }

        /// <summary>
        /// Move the respective item elements to the top of the target's children.
        /// </summary>
        public static bool TryMoveElementsToTop(Project project, ImmutableArray<ProjectItemElement> elements, IProjectTree target)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(target, nameof(target));

            // Get the target's first child. We use that child as our reference to move.
            IProjectTree targetChild = GetChildren(target).FirstOrDefault();

            // If we didn't find a child and our target is an empty folder and not the project root, let's walk up the tree to find a new target child.
            // Empty folders do not have a valid display order currently in CPS. If they ever do, we have to make changes to this.
            if (targetChild == null && target.IsFolder && !target.Flags.Contains(ProjectTreeFlags.ProjectRoot))
            {
                IProjectTree referenceTarget = target;
                while (targetChild == null && !referenceTarget.Flags.Contains(ProjectTreeFlags.ProjectRoot))
                {
                    referenceTarget = referenceTarget.Parent;
                    targetChild = GetChildren(referenceTarget).FirstOrDefault();
                }
            }

            if (targetChild == null)
            {
                // The project is empty, we don't need to move anything.
                return false;
            }

            // Make sure we exclude the moving elements when trying to find a reference element; this prevents us from choosing a reference element that is part of the moving elements.
            ProjectItemElement referenceElement = TryGetReferenceElement(project, targetChild, elements.Select(x => x.Include).ToImmutableArray(), MoveAction.Above);

            // If we couldn't find a reference element, we can't move the elements and we don't need to.
            if (referenceElement == null)
            {
                return false;
            }

            return TryMoveElements(elements, referenceElement, MoveAction.Above);
        }

        /// <summary>
        /// Get project item elements based on the project tree.
        /// Project tree can be a folder or item.
        /// </summary>
        public static ImmutableArray<ProjectItemElement> GetItemElements(Project project, IProjectTree projectTree, ImmutableArray<string> excludeIncludes)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(projectTree, nameof(projectTree));

            var includes = GetEvaluatedIncludes(projectTree).Except(excludeIncludes, StringComparer.OrdinalIgnoreCase).ToImmutableArray();
            return GetItemElements(project, includes);
        }

        /// <summary>
        /// Determines if we are moving up or down files or folders.
        /// </summary>
        private enum MoveAction { Above = 0, Below = 1 }

        private static ImmutableArray<ProjectItemElement> GetItemElements(Project project, ImmutableArray<string> includes)
        {
            ImmutableArray<ProjectItemElement>.Builder elements = ImmutableArray.CreateBuilder<ProjectItemElement>();

            foreach (string include in includes)
            {
                // GetItemsByEvaluatedInclude is efficient and uses a MultiDictionary underneath.
                //     It uses this: new MultiDictionary<string, ProjectItem>(StringComparer.OrdinalIgnoreCase);
                ProjectItem item = project.GetItemsByEvaluatedInclude(include).FirstOrDefault();

                // We only care about adding one item associated with the evaluated include.
                if (item?.Xml is ProjectItemElement element)
                {
                    elements.Add(element);
                }
            }

            return elements.ToImmutable();
        }

        /// <summary>
        /// Gets a read-only collection with the evaluated includes associated with a project tree.
        /// Evaluated includes will be in order by their display order.
        /// </summary>
        private static IEnumerable<string> GetEvaluatedIncludes(IProjectTree projectTree)
        {
            var treeQueue = new Queue<IProjectTree>();

            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var includes = new SortedList<int, string>();

            treeQueue.Enqueue(projectTree);

            // The queue is how we process each project tree.
            while (treeQueue.Count > 0)
            {
                IProjectTree tree = treeQueue.Dequeue();

                if (tree is IProjectItemTree2 tree2 && IsValidDisplayOrder(tree2.DisplayOrder))
                {
                    // Technically it is possible to have more than one of the same item names.
                    // We only want to add one of them.
                    // Sanity check
                    if (hashSet.Add(tree2.Item.ItemName))
                    {
                        includes.Add(tree2.DisplayOrder, tree2.Item.ItemName);
                    }
                }

                if (tree.IsFolder || tree.Flags.HasFlag(ProjectTreeFlags.Common.ProjectRoot))
                {
                    foreach (IProjectTree childTree in tree.Children)
                    {
                        treeQueue.Enqueue(childTree);
                    }
                }
            }

            return includes.Select(x => x.Value);
        }

        /// <summary>
        /// Checks to see if the display order is valid.
        /// </summary>
        private static bool IsValidDisplayOrder(int displayOrder)
        {
            return displayOrder > 0 && displayOrder != int.MaxValue;
        }

        /// <summary>
        /// Gets a collection a project tree's children. 
        /// The children will only have a valid display order, and the collection will be in order by their display order.
        /// </summary>
        private static ImmutableArray<IProjectTree> GetChildren(IProjectTree projectTree)
        {
            return projectTree.Children.Where(x => HasValidDisplayOrder(x)).OrderBy(x => GetDisplayOrder(x)).ToImmutableArray();
        }

        /// <summary>
        /// Gets a sibling based on the given project tree. Can return null.
        /// </summary>
        /// <param name="projectTree">the given project tree</param>
        /// <param name="returnSibling">passes the index of the given project tree from the given ordered sequence, expecting to return a sibling</param>
        /// <returns>a sibling</returns>
        private static IProjectTree2 GetSiblingByDisplayOrder(IProjectTree projectTree, Func<int, ImmutableArray<IProjectTree>, IProjectTree2> returnSibling)
        {
            IProjectTree parent = projectTree.Parent;
            int displayOrder = GetDisplayOrder(projectTree);
            if (!IsValidDisplayOrder(displayOrder) || parent == null)
            {
                return null;
            }

            ImmutableArray<IProjectTree> orderedChildren = GetChildren(parent);

            for (int i = 0; i < orderedChildren.Length; ++i)
            {
                IProjectTree sibling = orderedChildren[i];
                if (GetDisplayOrder(sibling) == displayOrder)
                {
                    return returnSibling(i, orderedChildren);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the previous sibling of the given project tree, if there is any. Can return null.
        /// </summary>
        private static IProjectTree2 GetPreviousSibling(IProjectTree projectTree)
        {
            return GetSiblingByDisplayOrder(projectTree, (i, orderedChildren) =>
            {
                if (i == 0)
                {
                    return null;
                }

                return orderedChildren[i - 1] as IProjectTree2;
            });
        }

        /// <summary>
        /// Gets the next sibling of the given project tree, if there is any. Can return null.
        /// </summary>
        private static IProjectTree2 GetNextSibling(IProjectTree projectTree)
        {
            return GetSiblingByDisplayOrder(projectTree, (i, orderedChildren) =>
            {
                if (i == (orderedChildren.Length - 1))
                {
                    return null;
                }

                return orderedChildren[i + 1] as IProjectTree2;
            });
        }

        /// <summary>
        /// Gets a sibling of the given project tree based on the move action. Can return null.
        /// </summary>
        private static IProjectTree GetSiblingByMoveAction(IProjectTree projectTree, MoveAction moveAction)
        {
            switch (moveAction)
            {
                case MoveAction.Above:
                    return GetPreviousSibling(projectTree);

                case MoveAction.Below:
                    return GetNextSibling(projectTree);
            }

            return null;
        }

        /// <summary>
        /// Gets a reference element based on the given project tree and move action. Can return null.
        /// The reference element is the element for which moved items will be above or below it.
        /// </summary>
        private static ProjectItemElement TryGetReferenceElement(Project project, IProjectTree projectTree, ImmutableArray<string> excludeIncludes, MoveAction moveAction)
        {
            switch (moveAction)
            {
                case MoveAction.Above:
                    return GetItemElements(project, projectTree, excludeIncludes).FirstOrDefault();

                case MoveAction.Below:
                    return GetItemElements(project, projectTree, excludeIncludes).LastOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Moves child elements based on the reference element and move action.
        /// </summary>
        /// <param name="referenceElement">element for which moved items will be above or below it</param>
        /// <returns>true or false; 'true' if all elements were successfully moved. 'false' if just one element was not moved successfully.</returns>
        private static bool TryMoveElements(ImmutableArray<ProjectItemElement> elements, ProjectItemElement referenceElement, MoveAction moveAction)
        {
            Requires.NotNull(referenceElement, nameof(referenceElement));

            ProjectElementContainer parent = referenceElement.Parent;
            if (parent == null || !elements.Any())
            {
                return false;
            }

            // Sanity check
            bool didAllElementsMove = true;

            switch (moveAction)
            {
                case MoveAction.Above:
                    foreach (ProjectItemElement element in elements)
                    {
                        ProjectElementContainer elementParent = element.Parent;
                        if (elementParent != null)
                        {
                            elementParent.RemoveChild(element);
                            parent.InsertBeforeChild(element, referenceElement);
                        }
                        else
                        {
                            didAllElementsMove = false;
                        }
                    }
                    break;

                case MoveAction.Below:
                    // Iterate in reverse order when we are wanting to move elements down.
                    // If we didn't do this, the end result would be the moved elements are reversed.
                    for (int i = elements.Length - 1; i >= 0; --i)
                    {
                        ProjectItemElement element = elements[i];

                        ProjectElementContainer elementParent = element.Parent;
                        if (elementParent != null)
                        {
                            elementParent.RemoveChild(element);
                            parent.InsertAfterChild(element, referenceElement);
                        }
                        else
                        {
                            didAllElementsMove = false;
                        }
                    }
                    break;
            }

            return didAllElementsMove;
        }

        /// <summary>
        /// Move project elements based on the given project tree, reference project tree and move action. 
        /// Will modify the project if successful, but not save; only dirty.
        /// </summary>
        private static bool TryMove(Project project, IProjectTree projectTree, IProjectTree referenceProjectTree, MoveAction moveAction)
        {
            if (!HasValidDisplayOrder(projectTree) || !HasValidDisplayOrder(referenceProjectTree))
            {
                return false;
            }

            if (projectTree == referenceProjectTree)
            {
                return false;
            }

            if (referenceProjectTree != null)
            {
                // The reference element is the element for which moved items will be above or below it.
                ProjectItemElement referenceElement = TryGetReferenceElement(project, referenceProjectTree, ImmutableArray<string>.Empty, moveAction);

                if (referenceElement != null)
                {
                    ImmutableArray<ProjectItemElement> elements = GetItemElements(project, projectTree, ImmutableArray<string>.Empty);
                    return TryMoveElements(elements, referenceElement, moveAction);
                }
            }

            return false;
        }

        /// <summary>
        /// Move project elements based on the given project tree and move action. 
        /// Will modify the project if successful, but not save; only dirty.
        /// </summary>
        private static bool TryMove(Project project, IProjectTree projectTree, MoveAction moveAction)
        {
            // Determine what sibling we want to look at based on if we are moving up or down.
            IProjectTree sibling = GetSiblingByMoveAction(projectTree, moveAction);
            return TryMove(project, projectTree, sibling, moveAction);
        }

        private static ImmutableArray<ProjectItemElement> GetAddedItemElements(ImmutableHashSet<string> previousIncludes, Project project)
        {
            return project.AllEvaluatedItems
                // We are excluding folder elements until CPS allows empty folders to be part of the order; when they do, we can omit checking the item type for "Folder".
                // Related changes will also need to happen in TryMoveElementsToTop when CPS allows empty folders in ordering.
                .Where(x => !previousIncludes.Contains(x.EvaluatedInclude, StringComparer.OrdinalIgnoreCase) && !x.ItemType.Equals("Folder", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Xml)
                .ToImmutableArray();
        }
    }
}
