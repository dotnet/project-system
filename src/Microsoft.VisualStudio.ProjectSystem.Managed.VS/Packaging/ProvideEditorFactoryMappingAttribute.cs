// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Packaging
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class ProvideEditorFactoryMappingAttribute : RegistrationAttribute
    {
        private readonly ProvideLanguageExtensionAttribute _wrapped;

        public ProvideEditorFactoryMappingAttribute(string editorFactoryGuid, string extension)
        {
            _wrapped = new ProvideLanguageExtensionAttribute(editorFactoryGuid, extension);
        }

        public override void Register(RegistrationContext context)
        {
            _wrapped.Register(context);
        }

        public override void Unregister(RegistrationContext context)
        {
            _wrapped.Unregister(context);
        }
    }
}
