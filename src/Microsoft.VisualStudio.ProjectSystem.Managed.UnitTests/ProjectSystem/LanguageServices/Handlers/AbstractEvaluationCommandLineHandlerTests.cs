// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Logging;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public partial class AbstractEvaluationCommandLineHandlerTests
    {
        [Fact]
        public void ApplyEvaluationChanges_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var difference = IProjectChangeDiffFactory.Create();
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectLoggerFactory.Create();
            
            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges((IComparable)null, difference, metadata, true, logger);
            });
        }

        [Fact]
        public void ApplyEvaluationChanges_NullAsDifference_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges(version, (IProjectChangeDiff)null, metadata, true, logger);
            });
        }

        [Fact]
        public void ApplyEvaluationChanges_NullAsMetadata_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges(version, difference, (ImmutableDictionary<string, IImmutableDictionary<string, string>>)null, true, logger);
            });
        }

        [Fact]
        public void ApplyEvaluationChanges_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();
            var metadata = ImmutableDictionary<string, IImmutableDictionary<string, string>>.Empty;
            
            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyEvaluationChanges(version, difference, metadata, true, (IProjectLogger)null);
            });
        }

        [Fact]
        public void ApplyDesignTimeChanges_NullAsVersion_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var difference = IProjectChangeDiffFactory.Create();
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyDesignTimeChanges((IComparable)null, difference, true, logger);
            });
        }

        [Fact]
        public void ApplyDesignTimeChanges_NullAsDifference_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var logger = IProjectLoggerFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyDesignTimeChanges(version, (IProjectChangeDiff)null, true, logger);
            });
        }

        [Fact]
        public void ApplyDesignTimeChanges_NullAsLogger_ThrowsArgumentNull()
        {
            var handler = CreateInstance();

            var version = 1;
            var difference = IProjectChangeDiffFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                handler.ApplyDesignTimeChanges(version, difference, true, (IProjectLogger)null);
            });
        }

        private static ConcreteAbstractEvaluationCommandLineHandler CreateInstance(string fullPath = null)
        {
            var project = UnconfiguredProjectFactory.ImplementFullPath(fullPath);

            return new ConcreteAbstractEvaluationCommandLineHandler(project);
        }
    }
}
