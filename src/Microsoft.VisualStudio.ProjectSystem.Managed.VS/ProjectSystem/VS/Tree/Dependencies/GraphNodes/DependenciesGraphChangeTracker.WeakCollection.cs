// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.GraphNodes
{
    internal sealed partial class DependenciesGraphChangeTracker
    {
        /// <summary>
        /// Maintains a collection of objects via <see cref="WeakReference{T}"/>, allowing elements to be
        /// garbage collected.
        /// </summary>
        /// <remarks>
        /// Note this implementation exists intentionally, despite <see cref="PlatformUI.WeakCollection{T}"/>.
        /// It allocates less memory during use, and combines pruning with enumeration. Neither type is
        /// free-threaded.
        /// </remarks>
        /// <typeparam name="T">Type of objects tracked within this collection.</typeparam>
        private sealed class WeakCollection<T> where T : class
        {
            private readonly LinkedList<WeakReference<T>> _references = new LinkedList<WeakReference<T>>();

            public void Add(T item)
            {
                _references.AddLast(new WeakReference<T>(item));
            }

            public bool Contains(T item)
            {
                foreach (T member in this)
                {
                    if (Equals(member, item))
                    {
                        return true;
                    }
                }

                return false;
            }

            public Enumerator GetEnumerator() => new Enumerator(_references);

            /// <summary>
            /// A struct enumerator for items within the weak collection, allowing enumeration without
            /// allocation. Dead references are cleaned up during enumeration.
            /// </summary>
            public struct Enumerator
            {
                private readonly LinkedList<WeakReference<T>> _list;
                private LinkedListNode<WeakReference<T>> _next;

                internal Enumerator(LinkedList<WeakReference<T>> list)
                {
                    _list = list;
                    _next = list.First;
                    Current = null!;
                }

                public T Current { get; private set; }

                public bool MoveNext()
                {
                    while (_next != null)
                    {
                        if (_next.Value.TryGetTarget(out T target))
                        {
                            // Reference is alive: yield it
                            Current = target;
                            _next = _next.Next;
                            return true;
                        }
                        else
                        {
                            // Reference has been collected: remove it and continue
                            LinkedListNode<WeakReference<T>> remove = _next;
                            _next = _next.Next;
                            _list.Remove(remove);
                        }
                    }

                    Current = null!;
                    return false;
                }
            }
        }
    }
}
