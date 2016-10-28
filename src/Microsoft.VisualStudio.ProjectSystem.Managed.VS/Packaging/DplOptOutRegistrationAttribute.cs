using Microsoft.VisualStudio.Shell;
using System;
using System.Globalization;

namespace Microsoft.VisualStudio.Packaging
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class DplOptOutRegistrationAttribute : RegistrationAttribute
    {
        private Guid _projectTypeGuid;
        private bool _optOutDeferredProjectLoad;

        public DplOptOutRegistrationAttribute(string projectTypeGuid, bool optOutDeferredProjectLoad)
        {
            _projectTypeGuid = new Guid(projectTypeGuid);
            _optOutDeferredProjectLoad = optOutDeferredProjectLoad;
        }

        public override void Register(RegistrationContext context)
        {
            using (Key key = context.CreateKey(GetRegKeyName()))
            {
                if (_optOutDeferredProjectLoad)
                {
                    key.SetValue("AllowDeferredProjectLoad", "0");
                }
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(GetRegKeyName());
        }

        private string GetRegKeyName()
        {
            return string.Format(CultureInfo.InvariantCulture, "Projects\\{0}", _projectTypeGuid.ToString("B"));
        }
    }
}
