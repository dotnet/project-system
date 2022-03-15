// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Represents the managed project types in Visual Studio.
    /// </summary>
    /// <remarks>
    ///     A project type points to a factory that implements <see cref="IVsProjectFactory"/> and is responsible for creating an in-memory
    ///     representation (implementing <see cref="IVsHierarchy"/>, <see cref="IVsProject"/>) that features through out Visual Studio
    ///     interact with.
    /// </remarks>
    internal static class ProjectType
    {
        /// <summary>
        ///     A <see cref="string"/> representing the Visual Basic project type based on the Common Project System (CPS).
        /// </summary>
        public const string VisualBasic = "778DAE3C-4631-46EA-AA77-85C1314464D9";

        /// <summary>
        ///     A <see cref="string"/> representing the legacy Visual Basic project type based on the native project system in msvbprj.dll.
        /// </summary>
        public const string LegacyVisualBasic = "F184B08F-C81C-45F6-A57F-5ABD9991F28F";

        /// <summary>
        ///     A <see cref="Guid"/> representing the legacy Visual Basic project type based on the native project system in msvbprj.dll.
        /// </summary>
        public static readonly Guid LegacyVisualBasicGuid = new(LegacyVisualBasic);

        /// <summary>
        ///     A <see cref="string"/> representing the C# project type based on the Common Project System (CPS).
        /// </summary>
        public const string CSharp = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";

        /// <summary>
        ///     A <see cref="Guid"/> representing the C# project type based on the Common Project System (CPS).
        /// </summary>
        public static readonly Guid CSharpGuid = new(CSharp);

        /// <summary>
        ///     A <see cref="string"/> representing the legacy C# project type based on the native project system in csproj.dll.
        /// </summary>
        public const string LegacyCSharp = "FAE04EC0-301F-11d3-BF4B-00C04F79EFBC";

        /// <summary>
        ///     A <see cref="Guid"/> representing the legacy C# project type based on the native project system in csproj.dll.
        /// </summary>
        public static readonly Guid LegacyCSharpGuid = new(LegacyCSharp);

        /// <summary>
        ///     A <see cref="string"/> representing the F# project type based on the Common Project System (CPS).
        /// </summary>
        public const string FSharp = "6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705";

        /// <summary>
        ///     A <see cref="Guid"/> representing the F# project type based on the Common Project System (CPS).
        /// </summary>
        public static readonly Guid FSharpGuid = new(FSharp);

        /// <summary>
        ///     A <see cref="string"/> representing the legacy F# project type based on the Managed Package Framework (MPF).
        /// </summary>
        public const string LegacyFSharp = "F2A71F9B-5D33-465A-A702-920D77279786";

        /// <summary>
        ///     A <see cref="Guid"/> representing the legacy F# project type based on the Managed Package Framework (MPF).
        /// </summary>
        public static readonly Guid LegacyFSharpGuid = new(LegacyFSharp);

        /// <summary>
        ///     A <see cref="string"/> representing the deprecated C# (xproj) project type based on the Common Project System (CPS).
        /// </summary>
        public const string LegacyXProj = "8BB2217D-0F2D-49D1-97BC-3654ED321F3B";
    }
}
