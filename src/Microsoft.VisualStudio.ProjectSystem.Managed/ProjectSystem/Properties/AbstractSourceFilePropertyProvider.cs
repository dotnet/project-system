// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    internal abstract class AbstractSourceFilePropertyProvider : IProjectPropertiesProvider
    {
        private readonly UnconfiguredProject _unconfiguredProject;
        private readonly IProjectPropertiesProvider _defaultProjectFilePropertiesProvider;
        private readonly Workspace _workspace;
        private readonly IProjectThreadingService _threadingService;

        public AbstractSourceFilePropertyProvider(UnconfiguredProject unconfiguredProject,
                                              [Import("Microsoft.VisualStudio.ProjectSystem.ProjectFile")] IProjectPropertiesProvider defaultProjectFilePropertiesProvider,
                                              Workspace workspace,
                                              IProjectThreadingService threadingService)
        {
            _unconfiguredProject = unconfiguredProject;
            _defaultProjectFilePropertiesProvider = defaultProjectFilePropertiesProvider;
            _workspace = workspace;
            _threadingService = threadingService;
        }

        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChanging;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChangedOnWriter;
        public event AsyncEventHandler<ProjectPropertyChangedEventArgs> ProjectPropertyChanged;

        public string DefaultProjectPath => _unconfiguredProject.FullPath;

        public IProjectProperties GetCommonProperties()
        {
            return _defaultProjectFilePropertiesProvider.GetCommonProperties();
        }

        public IProjectProperties GetItemProperties(string itemType, string item)
        {
            return _defaultProjectFilePropertiesProvider.GetItemProperties(itemType, item);
        }

        public IProjectProperties GetItemTypeProperties(string itemType)
        {
            return _defaultProjectFilePropertiesProvider.GetItemTypeProperties(itemType);
        }

        public IProjectProperties GetProperties(string file, string itemType, string item)
        {
            return new SourceFileProperties(ProjectPropertiesContext.GetContext(_unconfiguredProject, itemType, item), _workspace, _threadingService);
        }
    }
}
