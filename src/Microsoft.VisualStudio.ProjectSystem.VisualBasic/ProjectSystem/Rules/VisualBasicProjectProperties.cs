// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Represent the Visual Basic project properties.
    /// </summary>
    [Export]
    [ExcludeFromCodeCoverage]
    internal partial class VisualBasicProjectProperties : ProjectProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectProperties"/> class.
        /// </summary>
        [ImportingConstructor]
        public VisualBasicProjectProperties([Import] ConfiguredProject configuredProject)
            : base(configuredProject)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectProperties"/> class.
        /// </summary>
        public VisualBasicProjectProperties(ConfiguredProject configuredProject, string file, string itemType, string itemName)
            : base(configuredProject, file, itemType, itemName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectProperties"/> class.
        /// </summary>
        public VisualBasicProjectProperties(ConfiguredProject configuredProject, IProjectPropertiesContext projectPropertiesContext)
            : base(configuredProject, projectPropertiesContext)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectProperties"/> class.
        /// </summary>
        public VisualBasicProjectProperties(ConfiguredProject configuredProject, UnconfiguredProject unconfiguredProject)
            : base(configuredProject, unconfiguredProject)
        {
        }
    }
}
