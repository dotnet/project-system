// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.AttachedCollections.Implementation
{
    [Export(typeof(ITargetFrameworkContentCache))]
    internal sealed class TargetFrameworkContentCache : ITargetFrameworkContentCache
    {
        private ImmutableDictionary<FrameworkReferenceIdentity, ImmutableArray<FrameworkReferenceAssemblyItem>> _cache = ImmutableDictionary<FrameworkReferenceIdentity, ImmutableArray<FrameworkReferenceAssemblyItem>>.Empty;

        public ImmutableArray<FrameworkReferenceAssemblyItem> GetContents(FrameworkReferenceIdentity framework)
        {
            return ImmutableInterlocked.GetOrAdd(ref _cache, framework, LoadItems);

            static ImmutableArray<FrameworkReferenceAssemblyItem> LoadItems(FrameworkReferenceIdentity framework)
            {
                string frameworkListPath = Path.Combine(framework.Path, "data", "FrameworkList.xml");
                var pool = new Dictionary<string, string>(StringComparer.Ordinal);

                XDocument doc;
                try
                {
                    doc = XDocument.Load(frameworkListPath);
                }
                catch
                {
                    return ImmutableArray<FrameworkReferenceAssemblyItem>.Empty;
                }

                ImmutableArray<FrameworkReferenceAssemblyItem>.Builder results = ImmutableArray.CreateBuilder<FrameworkReferenceAssemblyItem>();

                foreach (XElement file in doc.Root.Elements("File"))
                {
                    if (!Strings.IsNullOrEmpty(framework.Profile))
                    {
                        //  We must filter to a specific profile
                        string? fileProfile = file.Attribute("Profile")?.Value;

                        if (fileProfile is null)
                        {
                            // The file doesn't specify a profile, so skip it
                            continue;
                        }

                        if (!new LazyStringSplit(fileProfile, ';').Contains(framework.Profile, StringComparer.OrdinalIgnoreCase))
                        {
                            // File file specifies a profile, but not one we are looking for, so skip it
                            continue;
                        }
                    }

                    if (file.Attribute("ReferencedByDefault")?.Value.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Don't include if ReferencedByDefault=false
                        continue;
                    }

                    string? assemblyName = Pool(file.Attribute("AssemblyName")?.Value);
                    string? path = Pool(file.Attribute("Path")?.Value);
                    string? assemblyVersion = Pool(file.Attribute("AssemblyVersion")?.Value);
                    string? fileVersion = Pool(file.Attribute("FileVersion")?.Value);

                    if (assemblyName is not null)
                    {
                        results.Add(new FrameworkReferenceAssemblyItem(assemblyName, path, assemblyVersion, fileVersion, framework));
                    }
                }

                return results.ToImmutable();

                string? Pool(string? s)
                {
                    if (s is not null)
                    {
                        if (pool.TryGetValue(s, out string existing))
                        {
                            return existing;
                        }

                        pool.Add(s, s);
                    }

                    return s;
                }
            }
        }
    }
}
