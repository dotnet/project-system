// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    internal partial class DesignTimeInputsCompiler
    {
        internal class CompilationQueue
        {
            private ImmutableDictionary<string, QueueItem> _queue = ImmutableDictionary<string, QueueItem>.Empty.WithComparers(StringComparers.Paths);

            public int Count => _queue.Count;

            public QueueItem? Pop()
            {
                QueueItem? result = null;
                ImmutableInterlocked.Update(ref _queue, queue =>
                {
                    ImmutableDictionary<string, QueueItem>.Enumerator enumerator = queue.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        result = enumerator.Current.Value;
                        return queue.Remove(result.FileName);
                    }
                    return queue;
                });

                return result;
            }

            public void Push(QueueItem item)
            {
                // Add an item to our queue, or if it exists, ensure that if anyone wanted us to ignore the file write time, we don't lose that info
                ImmutableInterlocked.AddOrUpdate(ref _queue, item.FileName, item, (fileName, value) => new QueueItem(fileName, item.SharedInputs, item.TempPEOutputPath, item.IgnoreFileWriteTime | value.IgnoreFileWriteTime));
            }

            /// <summary>
            /// Updates the queue items by either adding or updating from addOrUpdateItems, whilst ensuring that no items are tracked that aren't in the master list
            /// </summary>
            /// <param name="addOrUpdateItems">The items to add or update in the queue</param>
            /// <param name="masterListOfItems">The master list of items which will always be a superset of the queue contents</param>
            /// <param name="sharedInputs">The shared inputs that are to be updated in each item</param>
            /// <param name="tempPEOutputPath">The output path that is to be updated in each item</param>
            public void Update(ImmutableArray<DesignTimeInputFileChange> addOrUpdateItems, ImmutableHashSet<string> masterListOfItems, ImmutableHashSet<string> sharedInputs, string tempPEOutputPath)
            {
                ImmutableInterlocked.Update(ref _queue, (queue, args) =>
                {
                    foreach (DesignTimeInputFileChange item in addOrUpdateItems)
                    {
                        if (queue.TryGetValue(item.File, out QueueItem? existing))
                        {
                            // If the item exists, ensure that if anyone wanted us to ignore the file write time, we don't lose that info
                            // If the item doesn't need to be tracked, we'll clean it up later (we have to loop through everything then anyway)
                            queue = queue.SetItem(item.File, new QueueItem(item.File, args.sharedInputs, args.tempPEOutputPath, existing.IgnoreFileWriteTime | item.IgnoreFileWriteTime));
                        }
                        // Minor optimization - no point adding only to remove later
                        else if (masterListOfItems.Contains(item.File))
                        {
                            queue = queue.Add(item.File, new QueueItem(item.File, args.sharedInputs, args.tempPEOutputPath, item.IgnoreFileWriteTime));
                        }
                    }

                    // now go through our queue and make sure we aren't tracking items that aren't in the master list
                    foreach ((string? key, _) in queue)
                    {
                        if (!masterListOfItems.Contains(key))
                        {
                            queue = queue.Remove(key);
                        }
                    }

                    return queue;
                }, (sharedInputs, tempPEOutputPath));
            }

            public void RemoveSpecific(string fileName)
            {
                ImmutableInterlocked.TryRemove(ref _queue, fileName, out _);
            }
        }
    }
}
