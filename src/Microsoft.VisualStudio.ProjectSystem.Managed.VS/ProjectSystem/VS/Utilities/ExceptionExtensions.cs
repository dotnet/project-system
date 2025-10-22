// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

internal static class ExceptionExtensions
{
    /// <summary>
    /// Gets whether this exception is of a type that is deemed to be catchable.
    /// </summary>
    /// <remarks>
    /// Certain types of exception should not be caught by catch blocks, as they represent states
    /// from which program is not able to recover, such as a stack overflow, running out of memory,
    /// the thread being aborted, or an attempt to read memory for which access is disallowed.
    /// This helper is intended for use in exception filter expressions on catch blocks that wish to
    /// catch all kinds of exceptions other than these uncatchable exception types.
    /// </remarks>
    public static bool IsCatchable(this Exception e)
    {
        return e is not (StackOverflowException or OutOfMemoryException or ThreadAbortException or AccessViolationException);
    }
}
