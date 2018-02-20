// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    /// <summary>
    /// Allows registration of an IVsSingleFileGenerator that is stored in a remote assembly, without
    /// having to create a dummy class and use CodeGeneratorRegistration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class RemoteCodeGeneratorRegistrationAttribute : RegistrationAttribute
    {
        private string _contextGuid;
        private Guid _generatorGuid;
        private string _generatorName;
        private string _generatorRegKeyName;
        private bool _generatesDesignTimeSource = false;
        private bool _generatesSharedDesignTimeSource = false;
        /// <summary>
        /// Creates a new CodeGeneratorRegistrationAttribute attribute to register a custom
        /// code generator for the provided context. 
        /// </summary>
        /// <param name="generatorGuid">The guid of Code generator. Class that implements IVsSingleFileGenerator</param>
        /// <param name="generatorClassName">The class name of the Code generator.</param>
        /// <param name="generatorName">The generator name</param>
        /// <param name="contextGuid">The context GUID this code generator would appear under.</param>
        public RemoteCodeGeneratorRegistrationAttribute(string generatorGuid, string generatorClassName, string generatorName, string contextGuid)
        {
            Requires.NotNull(generatorGuid, nameof(generatorGuid));
            Requires.NotNull(contextGuid, nameof(contextGuid));
            Requires.NotNull(generatorName, nameof(generatorName));
            Requires.NotNull(generatorClassName, nameof(generatorClassName));

            _contextGuid = contextGuid;
            _generatorName = generatorName;
            _generatorRegKeyName = generatorClassName;
            if (!Guid.TryParse(generatorGuid, out _generatorGuid))
                throw new ArgumentException($"{generatorGuid} is not a valid GUID!", generatorGuid);
        }

        public RemoteCodeGeneratorRegistrationAttribute(string generatorGuid, string generatorClassName, string contextGuid) : this(generatorGuid, generatorClassName, generatorClassName, contextGuid) { }

        /// <summary>
        /// Get the Guid representing the project type
        /// </summary>
        public string ContextGuid {
            get { return _contextGuid; }
        }

        /// <summary>
        /// Get the Guid representing the generator type
        /// </summary>
        public Guid GeneratorGuid {
            get { return _generatorGuid; }
        }

        /// <summary>
        /// Get or Set the GeneratesDesignTimeSource value
        /// </summary>
        public bool GeneratesDesignTimeSource {
            get { return _generatesDesignTimeSource; }
            set { _generatesDesignTimeSource = value; }
        }

        /// <summary>
        /// Get or Set the GeneratesSharedDesignTimeSource value
        /// </summary>
        public bool GeneratesSharedDesignTimeSource {
            get { return _generatesSharedDesignTimeSource; }
            set { _generatesSharedDesignTimeSource = value; }
        }


        /// <summary>
        /// Gets the Generator name 
        /// </summary>
        public string GeneratorName {
            get { return _generatorName; }
        }

        /// <summary>
        /// Gets the Generator reg key name under 
        /// </summary>
        public string GeneratorRegKeyName {
            get { return _generatorRegKeyName; }
            set { _generatorRegKeyName = value; }
        }

        /// <summary>
        /// Property that gets the generator base key name
        /// </summary>
        private string GeneratorRegKey {
            get { return string.Format(CultureInfo.InvariantCulture, @"Generators\{0}\{1}", ContextGuid, GeneratorRegKeyName); }
        }
        /// <summary>
        ///     Called to register this attribute with the given context.  The context
        ///     contains the location where the registration information should be placed.
        ///     It also contains other information such as the type being registered and path information.
        /// </summary>
        public override void Register(RegistrationContext context)
        {
            using (Key childKey = context.CreateKey(GeneratorRegKey))
            {
                childKey.SetValue(string.Empty, GeneratorName);
                childKey.SetValue("CLSID", GeneratorGuid.ToString("B"));

                if (GeneratesDesignTimeSource)
                    childKey.SetValue("GeneratesDesignTimeSource", 1);

                if (GeneratesSharedDesignTimeSource)
                    childKey.SetValue("GeneratesSharedDesignTimeSource", 1);

            }

        }

        /// <summary>
        /// Unregister this file extension.
        /// </summary>
        /// <param name="context"></param>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(GeneratorRegKey);
        }
    }
}

