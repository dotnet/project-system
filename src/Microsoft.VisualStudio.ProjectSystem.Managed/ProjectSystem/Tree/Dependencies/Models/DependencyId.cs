// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models
{
    internal readonly struct DependencyId
    {
        public string ProviderId { get; }
        public string ModelId { get; }

        public DependencyId(string providerId, string modelId)
        {
            Requires.NotNull(providerId, nameof(providerId));
            Requires.NotNull(modelId, nameof(modelId));

            ProviderId = providerId;
            ModelId = modelId;
        }

        public bool Equals(DependencyId other)
        {
            return StringComparers.DependencyProviderTypes.Equals(ProviderId, other.ProviderId) &&
                   StringComparers.DependencyTreeIds.Equals(ModelId, other.ModelId);
        }

        public override bool Equals(object? obj) => obj is DependencyId other && Equals(other);

        public override int GetHashCode()
        {
            return unchecked(StringComparers.DependencyProviderTypes.GetHashCode(ProviderId) * 397) ^ StringComparers.DependencyTreeIds.GetHashCode(ModelId);
        }

        public static bool operator ==(DependencyId left, DependencyId right) => left.Equals(right);
        public static bool operator !=(DependencyId left, DependencyId right) => !left.Equals(right);

        public override string ToString() => $"({ProviderId}) {ModelId}";
    }
}
