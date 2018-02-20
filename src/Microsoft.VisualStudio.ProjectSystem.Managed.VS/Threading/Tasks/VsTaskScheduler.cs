// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Threading.Tasks
{
    /// <summary>
    ///     Visual Studio implementation of <see cref="ITaskScheduler"/>;
    /// </summary>
    [Export(typeof(ITaskScheduler))]
    internal class VsTaskScheduler : ITaskScheduler
    {
        [ImportingConstructor]
        public VsTaskScheduler()
        {
        }

        public JoinableTask<T> RunAsync<T>(TaskSchedulerPriority priority, Func<System.Threading.Tasks.Task<T>> asyncMethod)
        {
            // Only support UI background at the moment, we can add more when needed
            if (priority != TaskSchedulerPriority.UIThreadBackgroundPriority)
                throw new ArgumentOutOfRangeException(nameof(priority), priority, null);

            return ThreadHelper.JoinableTaskFactory.RunAsync(VsTaskRunContext.UIThreadBackgroundPriority, asyncMethod);
        }
    }
}
