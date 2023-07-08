// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using static Microsoft.VisualStudio.ProjectSystem.VS.TempPE.DesignTimeInputsCompiler;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    public class CompilationQueueTests
    {
        [Fact]
        public void Push_Adds()
        {
            var queue = new CompilationQueue();

            queue.Push(new QueueItem("FileName", ImmutableHashSet<string>.Empty, "", false));

            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void Pop_Removes()
        {
            var queue = new CompilationQueue();

            queue.Push(new QueueItem("FileName", ImmutableHashSet<string>.Empty, "", false));

            queue.Pop();

            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void Pop_WhenEmpty_ReturnsNull()
        {
            var queue = new CompilationQueue();

            Assert.Null(queue.Pop());
        }

        [Fact]
        public void Push_UpdatesFileWriteTime()
        {
            var queue = new CompilationQueue();

            queue.Push(new QueueItem("FileName", ImmutableHashSet<string>.Empty, "", false));

            queue.Push(new QueueItem("FileName", ImmutableHashSet<string>.Empty, "", true));

            Assert.Equal(1, queue.Count);
            Assert.True(queue.Pop()?.IgnoreFileWriteTime);
        }

        [Fact]
        public void RemoveSpecific_Removes()
        {
            var queue = new CompilationQueue();

            queue.Push(new QueueItem("FileName", ImmutableHashSet<string>.Empty, "", false));

            queue.RemoveSpecific("FileName");

            Assert.Equal(0, queue.Count);
        }

        [Fact]
        public void Update_Adds()
        {
            var queue = new CompilationQueue();

            var range = ImmutableArray.CreateRange(new DesignTimeInputFileChange[]
            {
                new DesignTimeInputFileChange("FileName1", false),
                new DesignTimeInputFileChange("FileName2", false)
            });

            queue.Update(range, ImmutableHashSet.CreateRange(new string[] { "FileName1", "FileName2" }), ImmutableHashSet<string>.Empty, "");

            Assert.Equal(2, queue.Count);
        }

        [Fact]
        public void Update_WIthDuplicates_Ignored()
        {
            var queue = new CompilationQueue();

            var range = ImmutableArray.CreateRange(new DesignTimeInputFileChange[]
            {
                new DesignTimeInputFileChange("FileName1", false),
                new DesignTimeInputFileChange("FileName1", false)
            });

            queue.Update(range, ImmutableHashSet.CreateRange(new string[] { "FileName1" }), ImmutableHashSet<string>.Empty, "");

            Assert.Equal(1, queue.Count);
        }

        [Fact]
        public void Update_UpdatesFileWriteTime()
        {
            var queue = new CompilationQueue();

            var range = ImmutableArray.CreateRange(new DesignTimeInputFileChange[]
            {
                new DesignTimeInputFileChange("FileName1", false),
                new DesignTimeInputFileChange("FileName1", true)
            });

            queue.Update(range, ImmutableHashSet.CreateRange(new string[] { "FileName1" }), ImmutableHashSet<string>.Empty, "");

            Assert.Equal(1, queue.Count);
            Assert.True(queue.Pop()?.IgnoreFileWriteTime);
        }

        [Fact]
        public void Update_RemovesItems()
        {
            var queue = new CompilationQueue();

            var range = ImmutableArray.CreateRange(new DesignTimeInputFileChange[]
            {
                new DesignTimeInputFileChange("FileName1", false),
                new DesignTimeInputFileChange("FileName2", false)
            });

            queue.Update(range, ImmutableHashSet.CreateRange(new string[] { "FileName1" }), ImmutableHashSet<string>.Empty, "");

            Assert.Equal(1, queue.Count);
            Assert.Equal("FileName1", queue.Pop()?.FileName);
        }
    }
}
