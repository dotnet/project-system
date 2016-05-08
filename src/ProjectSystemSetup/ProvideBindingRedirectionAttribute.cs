// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     A <see cref="RegistrationAttribute"/> that provides binding redirects for the project system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class ProvideProjectSystemBindingRedirectionAttribute : RegistrationAttribute
    {
        private readonly ProvideBindingRedirectionAttribute _redirectionAttribute;

        public ProvideProjectSystemBindingRedirectionAttribute(string assemblyName)
        {
            // ProvideBindingRedirectionAttribute is sealed, so we can't inherit from it to provide defaults.
            // Instead, we'll do more of an aggregation pattern here.
            _redirectionAttribute = new ProvideBindingRedirectionAttribute
            {
                AssemblyName = assemblyName,
                OldVersionLowerBound = "0.0.0.0",
                CodeBase = assemblyName + ".dll",
            };
        }

        public override void Register(RegistrationContext context)
        {
            _redirectionAttribute.Register(context);
        }

        public override void Unregister(RegistrationContext context)
        {
            _redirectionAttribute.Unregister(context);
        }
    }
}