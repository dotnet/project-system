// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectSnapshot2Factory
    {
        public static IProjectSnapshot2 CreateEmpty()
        {
            var mock = new Mock<IProjectSnapshot2>();
            mock.Setup(s => s.AdditionalDependentFileTimes)
                .Returns(ImmutableDictionary<string, DateTime>.Empty);

            return mock.Object;
        }

        public static IProjectSnapshot2 WithAdditionalDependentFileTime(string path, DateTime fileTime)
        {
            var fileTimes = ImmutableDictionary<string, DateTime>.Empty.Add(path, fileTime);

            var mock = new Mock<IProjectSnapshot2>();
            mock.Setup(s => s.AdditionalDependentFileTimes)
                .Returns(fileTimes);

            return mock.Object;

        }
    }
}
