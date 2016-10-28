using Microsoft.VisualStudio.Shell;
using System;
using System.Globalization;

namespace Microsoft.VisualStudio.Packaging
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class DplOptOutRegistrationAttribute : RegistrationAttribute
    {
        public DplOptOutRegistrationAttribute(string projectTypeGuid, bool optOutDeferredProjectLoad)
        {
            this.ProjectTypeGuid = new Guid(projectTypeGuid);
            this.OptOutDeferredProjectLoad = optOutDeferredProjectLoad;
        }


        /// <summary>
        /// Gets a unique value specific to this project file extension.
        /// </summary>
        public Guid ProjectTypeGuid { get; private set; }

        /// <summary>
        /// Value to say if the project type wants to Opt out of Deferred Project Load.
        /// </summary>
        public bool OptOutDeferredProjectLoad { get; private set; }

        public override void Register(RegistrationContext context)
        {
            using (Key key = context.CreateKey(GetRegKeyName()))
            {
                if (OptOutDeferredProjectLoad)
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
            return string.Format(CultureInfo.InvariantCulture, "Projects\\{0}", this.ProjectTypeGuid.ToString("B"));
        }
    }
}
