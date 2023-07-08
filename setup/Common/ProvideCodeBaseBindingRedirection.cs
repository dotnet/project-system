// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     A <see cref="RegistrationAttribute"/> that provides code-base binding redirects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class ProvideCodeBaseBindingRedirectionAttribute : RegistrationAttribute
    {
        private readonly ProvideBindingRedirectionAttribute _redirectionAttribute;

        public ProvideCodeBaseBindingRedirectionAttribute(string assemblyName)
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
