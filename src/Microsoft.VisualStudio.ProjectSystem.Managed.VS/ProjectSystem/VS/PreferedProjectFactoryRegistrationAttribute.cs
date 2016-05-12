// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class PreferedProjectFactoryRegistrationAttribute : RegistrationAttribute
    {
        public PreferedProjectFactoryRegistrationAttribute(string originalProjectTypeGuid, string preferedProjectTypeGuid)
        {
            OriginalProjectTypeGuid = new Guid(originalProjectTypeGuid).ToString("B");
            PreferedProjectTypeGuid = new Guid(preferedProjectTypeGuid).ToString("B");
        }

        public string OriginalProjectTypeGuid
        {
            get;
        }

        public string PreferedProjectTypeGuid
        {
            get;
        }

        public override void Register(RegistrationContext context)
        {
            using (Key key = context.CreateKey(this.RegKeyName(context, OriginalProjectTypeGuid)))
            {
                key.SetValue("PreferedProjectFactory", PreferedProjectTypeGuid);
            }

            using (Key key = context.CreateKey(this.RegKeyName(context, PreferedProjectTypeGuid)))
            {
                key.SetValue("OriginalProjectFactory", OriginalProjectTypeGuid);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveValue(this.RegKeyName(context, OriginalProjectTypeGuid), "PreferedProjectFactory");
            context.RemoveValue(this.RegKeyName(context, PreferedProjectTypeGuid), "OriginalProjectFactory");
        }

        /// <summary>
        /// Gets the path to the registry key that settings should fall under.
        /// </summary>
        private string RegKeyName(RegistrationContext context, string guid)
        {
            return string.Format(CultureInfo.InvariantCulture, "Projects\\{0}", guid);
        }
    }
}