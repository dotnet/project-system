// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    public static class CollectionsExtensions
    {
        public static bool AreEqual<T>(IList<T> left, IList<T> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            return !left.Except(right).Any();
        }
    }
}
