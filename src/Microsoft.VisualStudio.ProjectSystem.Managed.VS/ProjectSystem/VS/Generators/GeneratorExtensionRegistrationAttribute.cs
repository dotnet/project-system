// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class GeneratorExtensionRegistrationAttribute : RegistrationAttribute
    {
        private readonly string _extension;
        private readonly string _generator;
        private readonly string _contextGuid;

        public GeneratorExtensionRegistrationAttribute(string extension, string generator, string contextGuid)
        {
            _extension = extension ?? throw new ArgumentNullException(nameof(extension));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _contextGuid = contextGuid ?? throw new ArgumentNullException(nameof(contextGuid));
        }

        public override void Register(RegistrationContext context)
        {
            using (Key childKey = context.CreateKey($"Generators\\{_contextGuid}\\{_extension}"))
            {
                childKey.SetValue(string.Empty, _generator);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(_extension);
        }
    }
}
