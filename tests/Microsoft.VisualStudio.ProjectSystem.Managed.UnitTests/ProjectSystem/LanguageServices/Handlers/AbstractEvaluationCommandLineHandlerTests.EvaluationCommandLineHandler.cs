// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem.Logging;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public partial class AbstractEvaluationCommandLineHandlerTests
    {
        private class EvaluationCommandLineHandler : AbstractEvaluationCommandLineHandler
        {
            public EvaluationCommandLineHandler(UnconfiguredProject project)
                : base(project)
            {
                Files = new Dictionary<string, IImmutableDictionary<string, string>>();
            }

            public ICollection<string> FileNames
            {
                get { return Files.Keys; }
            }

            public Dictionary<string, IImmutableDictionary<string, string>> Files
            {
                get;
            }

            protected override void AddToContext(string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IProjectLogger logger)
            {
                Files.Add(fullPath, metadata);
            }

            protected override void RemoveFromContext(string fullPath, IProjectLogger logger)
            {
                Files.Remove(fullPath);
            }
        }
    }
}
