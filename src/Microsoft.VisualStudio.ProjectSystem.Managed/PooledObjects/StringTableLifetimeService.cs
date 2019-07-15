// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Buffers.PooledObjects
{
    /// <summary>
    /// Ensures that the string table will release all its strings when the solution is unloads all .NET projects
    /// </summary>
    [Export(typeof(IImplicitlyActiveService))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class StringTableLifetimeService : AbstractMultiLifetimeComponent<StringTableLifetimeService.Instance>, IImplicitlyActiveService
    {
        private Instance? _instance;

        public StringTableLifetimeService(JoinableTaskContextNode joinableTaskContextNode)
            : base(joinableTaskContextNode)
        {
        }

        public Task ActivateAsync()
        {
            if (_instance is null)
            {
                CreateInstance();
            }
         
            return _instance!.InitializeAsync();
        }

        public Task DeactivateAsync()
        {
            if (_instance != null)
            {
                return _instance.DisposeAsync();
            }

            return Task.CompletedTask;
        }

        protected override Instance CreateInstance()
        {
            _instance = new Instance();
            return _instance;
        }

        internal class Instance : IMultiLifetimeInstance
        {
            public StringTable? StringTable { get; private set; }

            public Task InitializeAsync()
            {
                StringTable = StringTable.GetInstance();
                return Task.CompletedTask;
            }

            public Task DisposeAsync()
            {
                StringTable?.ClearAll();
                return Task.CompletedTask;
            }
        }
    }
}
