// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Utilities
{
    /// <summary>
    /// This class contains methods that are useful for error checking and validation.
    /// </summary>
    internal static class ProjectErrorUtilities
    {
        /// <summary>
        /// Throws an ProjectException with the specified message string if some condition is false.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="message">The text message to display.</param>
        internal static void VerifyThrowProjectException(bool condition, string message)
        {
            if (!condition)
            {
                TraceUtilities.TraceError(message);
                ThrowProjectException(message);
            }
        }

        /// <summary>
        /// Throws an ProjectException with the specified message string if some condition is false.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="message">The text message to display.</param>
        /// <param name="arg0">The first string formatting argument.</param>
        internal static void VerifyThrowProjectException(bool condition, string message, object arg0)
        {
            if (!condition)
            {
                TraceUtilities.TraceError(message, arg0);
                ThrowProjectException(message, arg0);
            }
        }

        /// <summary>
        /// Throws an ProjectException with the specified message string if some condition is false.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="unformattedMessage">The text message to display.</param>
        /// <param name="args">The formatting arguments.</param>
        internal static void VerifyThrowProjectException(bool condition, string unformattedMessage, params object[] args)
        {
            if (!condition)
            {
                TraceUtilities.TraceError(unformattedMessage, args);
                throw new ProjectException(ResourceUtilities.FormatString(unformattedMessage, args));
            }
        }

        /// <summary>
        /// Throws an ProjectException with the specified message string
        /// </summary>
        /// <param name="message">The text message to display.</param>
        /// <returns>Nothing.  This method always throws.</returns>
        internal static Exception ThrowProjectException(string message)
        {
            // PERF NOTE: explicitly passing null for the arguments array
            // prevents memory allocation
            throw ThrowProjectExceptionHelper(null, message, null);
        }

        /// <summary>
        /// Throws an ProjectException with the specified message string
        /// </summary>
        /// <param name="message">The text message to display.</param>
        /// <param name="arg0">The first formatting argument to consume when formatting the message string.</param>
        /// <returns>Nothing.  This method always throws.</returns>
        internal static Exception ThrowProjectException(string message, object arg0)
        {
            throw ThrowProjectExceptionHelper(null, message, arg0);
        }
                
        /// <summary>
        /// Throws a ProjectException.
        ///
        /// PERF WARNING: calling a method that takes a variable number of arguments
        /// is expensive, because memory is allocated for the array of arguments -- do
        /// not call this method repeatedly in performance-critical scenarios
        /// </summary>
        /// <param name="innerException">Can be null.</param>
        /// <param name="unformattedMessage">The text message to display.</param>
        /// <param name="args">The formatting arguments to consume when formatting the message string.</param>
        /// <returns>Nothing.  This method always throws.</returns>
        private static Exception ThrowProjectExceptionHelper(Exception innerException, string unformattedMessage, params object[] args)
        {
            throw new ProjectException(ResourceUtilities.FormatString(unformattedMessage, args), innerException);
        }
    }
}
