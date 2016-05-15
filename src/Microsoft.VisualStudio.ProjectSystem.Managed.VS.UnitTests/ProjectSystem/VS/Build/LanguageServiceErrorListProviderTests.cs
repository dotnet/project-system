// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using Microsoft.Build.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    [ProjectSystemTrait]
    public class LanguageServiceErrorListProviderTests
    {
        [Fact]
        public void Constructor_NullAsUnconfiguedProject_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("unconfiguredProject", () => {
                new LanguageServiceErrorListProvider((UnconfiguredProject)null);
            });
        }

        [Fact]
        public void SuspendRefresh_DoesNotThrow()
        {
            var provider = CreateInstance();
            provider.SuspendRefresh();
        }

        [Fact]
        public void ResumeRefresh_DoesNotThrow()
        {
            var provider = CreateInstance();
            provider.ResumeRefresh();
        }

        [Fact]
        public async void ClearAllAsync_WhenNoProjectsWithIntellisense_DoesNotThrow()
        {
            var provider = CreateInstance();

            await provider.ClearAllAsync();
        }

        [Fact]
        public async void ClearMessageFromTargetAsync_WhenNoProjectsWithIntellisense_DoesNotThrow()
        {
            var provider = CreateInstance();

            await provider.ClearMessageFromTargetAsync("targetName");
        }

        [Fact]
        public async void AddMessageAsync_NullAsTask_ThrowsArgumentNull()
        {
            var provider = CreateInstance();

            await Assert.ThrowsAsync<ArgumentNullException>("task", () => {
                return provider.AddMessageAsync((TargetGeneratedTask)null);
            });
        }

        [Fact]
        public async void AddMessageAsync_WhenNoProjectsWithIntellisense_ReturnsNotHandled()
        {
            var provider = CreateInstance();

            var task = new TargetGeneratedTask();
            task.BuildEventArgs = new BuildErrorEventArgs(null, "Code", "File", 1, 1, 1, 1, "Message", "HelpKeyword", "Sender");

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.NotHandled);
        }

        [Fact]
        public async void AddMessageAsync_NullAsTaskBuildEventArgs_ReturnsNotHandled()
        {
            var provider = CreateInstance();

            var task = new TargetGeneratedTask();
            task.BuildEventArgs = null;

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.NotHandled);
        }

        [Fact]
        public async void AddMessageAsync_UnrecognizedArgsAsTaskBuildEventArgs_ReturnsNotHandled()
        {
            var provider = CreateInstance();

            var task = new TargetGeneratedTask();
            task.BuildEventArgs = new LazyFormattedBuildEventArgs("Message", "HelpKeyword", "SenderName");

            var result = await provider.AddMessageAsync(task);

            Assert.Equal(result, AddMessageResult.NotHandled);
        }

        private static LanguageServiceErrorListProvider CreateInstance()
        {
            return CreateInstance(null);
        }

        private static LanguageServiceErrorListProvider CreateInstance(IProjectWithIntellisense project)
        {
            var provider = new LanguageServiceErrorListProvider(IUnconfiguredProjectFactory.Create("CSharp"));

            if (project != null)
                provider.ProjectsWithIntellisense.Add(project, "CSharp");

            return provider;
        }
    }
}
