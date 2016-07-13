using Microsoft.VisualStudio.Shell;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class GeneratorExtensionRegistrationAttribute : RegistrationAttribute
    {
        private readonly string _extension;
        private readonly string _generator;
        private readonly Guid _contextGuid;

        public GeneratorExtensionRegistrationAttribute(string extension, string generator, string contextGuid)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));
            if (contextGuid == null)
                throw new ArgumentNullException(nameof(contextGuid));

            _extension = extension;
            _generator = generator;
            if (!Guid.TryParse(contextGuid, out _contextGuid))
                throw new ArgumentException($"{contextGuid} is not a valid GUID!", contextGuid);
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
