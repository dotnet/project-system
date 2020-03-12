// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    public enum ProjectTreeFormatError
    {
        IdExpected_EncounteredOnlyWhiteSpace,
        IdExpected_EncounteredDelimiter,
        IdExpected_EncounteredEndOfString,
        DelimiterExpected,
        DelimiterExpected_EncounteredEndOfString,
        EndOfStringExpected,
        UnrecognizedPropertyName,
        UnrecognizedPropertyValue,
        IndentTooManyLevels,
        MultipleRoots,
        IntegerExpected,
        GuidExpected,
    }
}
