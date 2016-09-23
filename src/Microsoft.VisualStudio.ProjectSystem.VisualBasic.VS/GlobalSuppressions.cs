
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

// workaround for https://github.com/dotnet/roslyn-analyzers/issues/955
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
                "Reliability",
                "RS0006:Do not mix attributes from different versions of MEF",
                Justification = "<Pending>")]
