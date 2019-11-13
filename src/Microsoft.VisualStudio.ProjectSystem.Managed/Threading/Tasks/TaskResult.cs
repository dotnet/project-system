// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading
{
    /// <summary>
    ///     Provides sentinel Tasks that represent commonly returned values.
    /// </summary>
    internal static class TaskResult
    {
        /// <summary>
        ///     Represents a <see cref="Task{TResult}"/> that's completed successfully with the result of <see langword="false"/>.
        /// </summary>
        public static Task<bool> False => TplExtensions.FalseTask;

        /// <summary>
        ///     Represents a <see cref="Task{TResult}"/> that's completed successfully with the result of <see langword="true"/>.
        /// </summary>
        public static Task<bool> True => TplExtensions.TrueTask;
    }
}
