// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// This class implements a dictionary bassed stack that always pops a dictionary
    /// with requested keys and minimal values from their respective collections in the 
    /// stack. It is used to synchronize data source versions to make sure smaller versions
    /// always come before greater ones for corresponding NamedIdentity.
    /// </summary>
    internal class DataSourceVersionsSync
    {
        private object _lockDataSourceVersions = new object();
        private Dictionary<NamedIdentity, List<IComparable>> _dataSourcesMap
                                = new Dictionary<NamedIdentity, List<IComparable>>();

        public void PushDataSourceVersions(IImmutableDictionary<NamedIdentity, IComparable> dataSources)
        {
            if (dataSources == null)
            {
                return;
            }

            lock (_lockDataSourceVersions)
            {
                foreach (var dataSourceKvp in dataSources)
                {
                    List<IComparable> versionsList = null;
                    if (!_dataSourcesMap.TryGetValue(dataSourceKvp.Key, out versionsList))
                    {
                        versionsList = new List<IComparable>();
                        _dataSourcesMap.Add(dataSourceKvp.Key, versionsList);
                    }

                    versionsList.Add(dataSourceKvp.Value);
                    // we don't expect too many versions, so simple sorting should not be to costly
                    versionsList.Sort(); 
                }
            }
        }

        public IImmutableDictionary<NamedIdentity, IComparable> PopMinimalDataSourceVersions(
            IImmutableDictionary<NamedIdentity, IComparable> requestedDataSources)
        {
            if (requestedDataSources == null)
            {
                return null;
            }

            lock (_lockDataSourceVersions)
            {
                var builder = new Dictionary<NamedIdentity, IComparable>();
                foreach (var dataSourceKvp in requestedDataSources)
                {
                    List<IComparable> versionsList = null;
                    if (!_dataSourcesMap.TryGetValue(dataSourceKvp.Key, out versionsList) ||
                        versionsList == null ||
                        versionsList.Count <= 0)
                    {
                        // This should never happen, since we always should Push data sources before popping
                        continue; 
                    }

                    var minimalVersion = versionsList[0];
                    versionsList.RemoveAt(0);

                    builder.Add(dataSourceKvp.Key, minimalVersion);
                }

                return builder.Count > 0 ? builder.ToImmutableDictionary() : null;
            }
        }
    }
}
