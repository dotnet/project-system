// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

internal sealed class RestoreStateComparisonBuilder
{
    private StringBuilder? _sb;

    /// <summary>
    /// Tracks the names of hierarchical scopes within which log messages should be written.
    /// </summary>
    private readonly List<string> _scopes = [];

    private int _loggedScopeDepth = 0;

    public void PushScope(string title)
    {
        _scopes.Add(title);
    }

    public void PopScope()
    {
        _scopes.RemoveAt(_scopes.Count - 1);
        if (_scopes.Count < _loggedScopeDepth)
        {
            _loggedScopeDepth = _scopes.Count;
        }
    }

    public void CompareString(string before, string after, string? name = null)
    {
        // Use the same comparison approach as RestoreHasher.
        // All strings use ordinal comparison in the hash (via UTF8 bytes).

        if (!StringComparer.Ordinal.Equals(before, after))
        {
            if (name is not null)
                PushScope(name);
            Log($"Before: {before}");
            Log($"After: {after}");
            if (name is not null)
                PopScope();
        }
    }

    public void CompareArray<T>(ImmutableArray<T> before, ImmutableArray<T> after, string? name = null) where T : class, IRestoreState<T>
    {
        if (name is not null)
            PushScope(name);

        if (before.Length != after.Length)
        {
            Log($"The number of items changed from {before.Length} to {after.Length}.");
        }
        else
        {
            foreach ((T a, T b) in before.Zip(after, static (a, b) => (a, b)))
            {
                a.DescribeChanges(this, b);
            }
        }

        if (name is not null)
            PopScope();
    }

    internal void CompareDictionary(IImmutableDictionary<string, string> before, IImmutableDictionary<string, string> after, string? name = null)
    {
        if (name is not null)
            PushScope(name);

        SetDiff<string> diff = new(before.Keys, after.Keys, StringComparer.Ordinal);
            
        if (diff.HasChange)
        {
            foreach (string added in diff.Added)
            {
                Log($"{added} added");
            }

            foreach (string removed in diff.Removed)
            {
                Log($"{removed} removed");
            }
        }

        foreach ((string beforeKey, string beforeValue) in before)
        {
            if (!after.TryGetValue(beforeKey, out string afterValue))
            {
                continue;
            }

            CompareString(beforeValue, afterValue, beforeKey);
        }

        if (name is not null)
            PopScope();
    }

    private void Log(string line)
    {
        _sb ??= new();

        // Ensure scope logged
        if (_loggedScopeDepth < _scopes.Count)
        {
            // Need to log at least one scope
            for (int i = _loggedScopeDepth; i < _scopes.Count; i++)
            {
                string scope = _scopes[i];

                Indent(_sb, i);
                _sb.AppendLine(scope);
            }

            _loggedScopeDepth = _scopes.Count;
        }

        Indent(_sb, _scopes.Count);

        _sb.AppendLine(line);

        static void Indent(StringBuilder sb, int indent)
        {
            for (int i = 0; i < indent; i++)
                sb.Append("    ");
        }
    }

    public override string ToString() => _sb?.ToString() ?? "";
}
