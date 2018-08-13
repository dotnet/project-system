// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.ProjectSystem.Logging;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    internal static class IEvaluationHandlerFactory
    {
        public static IEvaluationHandler ImplementEvaluationRule(string evaluationRule)
        {
            var mock = new Mock<IEvaluationHandler>();

            mock.SetupGet(h => h.EvaluationRule)
                .Returns(evaluationRule);

            return mock.Object;
        }

        public static IEvaluationHandler ImplementHandle(string evaluationRule, Action<IComparable, IProjectChangeDescription, bool, IProjectLogger> action)
        {
            var mock = new Mock<IEvaluationHandler>();

            mock.SetupGet(h => h.EvaluationRule)
                .Returns(evaluationRule);

            mock.Setup(h => h.Handle(It.IsAny<IComparable>(), It.IsAny<IProjectChangeDescription>(), It.IsAny<bool>(), It.IsAny<IProjectLogger>()))
                .Callback(action);

            return mock.Object;
        }
    }
}
