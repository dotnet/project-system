// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.Logging;
using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IEvaluationHandlerFactory
    {
        public static IProjectEvaluationHandler ImplementProjectEvaluationRule(string evaluationRule)
        {
            var mock = new Mock<IProjectEvaluationHandler>();

            mock.SetupGet(h => h.ProjectEvaluationRule)
                .Returns(evaluationRule);

            return mock.Object;
        }

        public static IProjectEvaluationHandler ImplementHandle(string evaluationRule, Action<IComparable, IProjectChangeDescription, bool, IProjectLogger> action)
        {
            var mock = new Mock<IProjectEvaluationHandler>();

            mock.SetupGet(h => h.ProjectEvaluationRule)
                .Returns(evaluationRule);

            mock.Setup(h => h.Handle(It.IsAny<IComparable>(), It.IsAny<IProjectChangeDescription>(), It.IsAny<bool>(), It.IsAny<IProjectLogger>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
