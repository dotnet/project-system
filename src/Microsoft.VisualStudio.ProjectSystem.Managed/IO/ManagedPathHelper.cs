// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio.IO
{
    /// <summary>
    /// Helpful Path operations that are repeating in several places.
    /// We need to ask to make these API public in CPS's PathHelper.
    /// </summary>
    internal static class ManagedPathHelper
    {
        /// <summary>
        /// Tests a path to see if it is absolute or not. More reliable than <see cref="System.IO.Path.IsPathRooted"/>.
        /// </summary>
        /// <param name="path"></param>
        public static bool IsRooted(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));
            // We don't use System.IO.Path.IsPathRooted because it doesn't support
            // URIs, and because it returns true for paths like "\dir\file", which is
            // relative to whatever drive we're talking about.
            return Uri.TryCreate(path, UriKind.Absolute, out _);
        }

        /// <summary>
        /// Makes the specified path absolute if possible, otherwise return an empty string.
        /// </summary>
        /// <param name="basePath">The path used as the root if <paramref name="path"/> is relative.</param>
        /// <param name="path">An absolute or relative path.</param>
        /// <returns>An absolute path, or the empty string if <paramref name="path"/> invalid.</returns>
        public static string TryMakeRooted(string basePath, string path)
        {
            Requires.NotNullOrEmpty(basePath, nameof(basePath));
            Requires.NotNullOrEmpty(path, nameof(path));

            try
            {
                return PathHelper.MakeRooted(basePath, path);
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }
    }
}
