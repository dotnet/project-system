// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Test.Apex.VisualStudio.Shell.ToolWindows;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class AssertExtensions
    {
        public static void AreEqual(this Assert assert, ImageMoniker expected, ExportableImageMoniker? actual)
        {
            if (actual == null)
            {
                throw new AssertFailedException($"ImageMoniker did not match.{Environment.NewLine}Expected: {S(expected)}{Environment.NewLine}Actual: null");
            }

            var actualMoniker = ToImageMoniker(actual.Value);

            if (expected.Id != actualMoniker.Id || expected.Guid != actualMoniker.Guid)
            {
                throw new AssertFailedException($"ImageMoniker did not match.{Environment.NewLine}Expected: {S(expected)}{Environment.NewLine}Actual: {S(actualMoniker)}");
            }

            static string S(ImageMoniker a) => ImageMonikerDebuggerDisplay.FromImageMoniker(a);
        }

        public static ImageMoniker ToImageMoniker(this ExportableImageMoniker actual)
        {
            return new ImageMoniker { Id = actual.Id, Guid = actual.Guid };
        }

        public static bool AreEqual(ImageMoniker expected, ExportableImageMoniker? actual)
        {
            if (actual == null)
            {
                return false;
            }

            if (expected.Id != actual.Value.Id || expected.Guid != actual.Value.Guid)
            {
                return false;
            }

            return true;
        }
    }
}
