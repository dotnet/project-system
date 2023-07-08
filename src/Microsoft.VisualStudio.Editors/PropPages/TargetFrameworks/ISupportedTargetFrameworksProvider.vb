' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.Versioning

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Interface ISupportedTargetFrameworksProvider

        Function GetSupportedTargetFrameworks(framework As FrameworkName) As IReadOnlyList(Of TargetFrameworkMoniker)

    End Interface

End Namespace
