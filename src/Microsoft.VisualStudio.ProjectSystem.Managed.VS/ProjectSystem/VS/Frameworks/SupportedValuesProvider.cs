// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;
using EnumCollection = System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>;
using EnumCollectionProjectValue = Microsoft.VisualStudio.ProjectSystem.IProjectVersionedValue<System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Frameworks
{
    /// <summary>
    ///     Abstract class for providers that process values from evaluation.
    /// </summary>
    internal abstract class SupportedValuesProvider : ChainedProjectValueDataSourceBase<EnumCollection>, IDynamicEnumValuesProvider, IDynamicEnumValuesGenerator
    {
        protected IProjectSubscriptionService SubscriptionService { get; }

        protected abstract string RuleName { get; }

        /// <summary>
        /// Specifies if a 'None' value should be added to the resulting list.
        /// </summary>
        private readonly bool _useNoneValue;

        protected SupportedValuesProvider(
            ConfiguredProject project,
            IProjectSubscriptionService subscriptionService,
            bool useNoneValue = false)
            : base(project, synchronousDisposal: false, registerDataSource: false)
        {
            SubscriptionService = subscriptionService;
            _useNoneValue = useNoneValue;
        }

        protected override IDisposable? LinkExternalInput(ITargetBlock<EnumCollectionProjectValue> targetBlock)
        {
            IProjectValueDataSource<IProjectSubscriptionUpdate> source = SubscriptionService.ProjectRuleSource;

            // Transform the values from evaluation to structure from the rule schema.
            DisposableValue<ISourceBlock<EnumCollectionProjectValue>> transformBlock = source.SourceBlock.TransformWithNoDelta(
                update => update.Derive(Transform),
                suppressVersionOnlyUpdates: false,
                ruleNames: RuleName);

            // Set the link up so that we publish changes to target block.
            transformBlock.Value.LinkTo(targetBlock, DataflowOption.PropagateCompletion);

            // Join the source blocks, so if they need to switch to UI thread to complete 
            // and someone is blocked on us on the same thread, the call proceeds.
            JoinUpstreamDataSources(source);

            return transformBlock;
        }

        protected EnumCollection Transform(IProjectSubscriptionUpdate input)
        {
            IProjectRuleSnapshot snapshot = input.CurrentState[RuleName];

            int capacity = _useNoneValue ? 1 + snapshot.Items.Count : snapshot.Items.Count;
            var list = new List<IEnumValue>(capacity);

            if (_useNoneValue)
            {
                list.Add(new PageEnumValue(new EnumValue()
                {
                    Name = string.Empty,
                    DisplayName = Resources.Property_NoneValue
                }));
            }

            list.AddRange(snapshot.Items.Select(ToEnumValue));
            list.Sort(SortValues); // TODO: This is a hotfix for item ordering. Remove this when completing: https://github.com/dotnet/project-system/issues/7025
            return list;
        }

        protected abstract int SortValues(IEnumValue a, IEnumValue b);

        protected abstract IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item);

        bool IDynamicEnumValuesGenerator.AllowCustomValues => false;

        Task<IEnumValue?> IDynamicEnumValuesGenerator.TryCreateEnumValueAsync(string userSuppliedValue) => TaskResult.Null<IEnumValue>();

        public Task<IDynamicEnumValuesGenerator> GetProviderAsync(IList<NameValuePair>? options)
        {
            return Task.FromResult<IDynamicEnumValuesGenerator>(this);
        }

        public async Task<EnumCollection> GetListedValuesAsync()
        {
            using (JoinableCollection.Join())
            {
                EnumCollectionProjectValue snapshot = await SourceBlock.ReceiveAsync();

                return snapshot.Value;
            }
        }

        protected sealed class NaturalStringComparer : IComparer<string>
        {
            public static NaturalStringComparer Instance { get; } = new NaturalStringComparer();

            public int Compare(string x, string y)
            {
                // sort nulls to the start
                if (x == null)
                    return y == null ? 0 : -1;
                if (y == null)
                    return 1;

                var ix = 0;
                var iy = 0;

                while (true)
                {
                    // sort shorter strings to the start
                    if (ix >= x.Length)
                        return iy >= y.Length ? 0 : -1;
                    if (iy >= y.Length)
                        return 1;

                    var cx = x[ix];
                    var cy = y[iy];

                    int result;
                    if (char.IsDigit(cx) && char.IsDigit(cy))
                        result = CompareInteger(x, y, ref ix, ref iy);
                    else
                        result = cx.CompareTo(y[iy]);

                    if (result != 0)
                        return result;

                    ix++;
                    iy++;
                }
            }

            private static int CompareInteger(string x, string y, ref int ix, ref int iy)
            {
                var lx = GetNumLength(x, ix);
                var ly = GetNumLength(y, iy);

                // shorter number first (note, doesn't handle leading zeroes)
                if (lx != ly)
                    return lx.CompareTo(ly);

                for (var i = 0; i < lx; i++)
                {
                    var result = x[ix++].CompareTo(y[iy++]);
                    if (result != 0)
                        return result;
                }

                return 0;
            }

            private static int GetNumLength(string s, int i)
            {
                var length = 0;
                while (i < s.Length && char.IsDigit(s[i++]))
                    length++;
                return length;
            }
        }
    }
}
