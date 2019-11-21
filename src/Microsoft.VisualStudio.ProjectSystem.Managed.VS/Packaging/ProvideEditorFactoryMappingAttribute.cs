using System;
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
