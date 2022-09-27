// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

internal class ImplicitUsing
{
    public string Include { get; }
    public string? Alias { get; }
    public bool IsStatic { get; }
    public bool IsReadOnly { get; }

    public ImplicitUsing(string include, string? alias, bool isStatic, bool isReadOnly)
    {
        Include = include;
        Alias = alias;
        IsStatic = isStatic;
        IsReadOnly = isReadOnly;
    }

    protected bool Equals(ImplicitUsing other)
    {
        return Include == other.Include && Alias == other.Alias && IsStatic == other.IsStatic && IsReadOnly == other.IsReadOnly;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ImplicitUsing)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Include.GetHashCode();
            hashCode = (hashCode * 397) ^ (Alias is not null ? Alias.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ IsStatic.GetHashCode();
            hashCode = (hashCode * 397) ^ IsReadOnly.GetHashCode();
            return hashCode;
        }
    }
}
