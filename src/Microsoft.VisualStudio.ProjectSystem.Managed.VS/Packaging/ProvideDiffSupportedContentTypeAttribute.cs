// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class ProvideDiffSupportedContentTypeAttribute : RegistrationAttribute
    {
        private const string RegistryKey = @"Diff\SupportedContentTypes";

        private readonly string _contentType;
        private readonly string _extension;

        public ProvideDiffSupportedContentTypeAttribute(string extension, string contentType)
        {
            _extension = extension;
            _contentType = contentType;
        }

        public override void Register(RegistrationContext context)
        {
            using Key key = context.CreateKey(RegistryKey);

            key.SetValue(_extension, _contentType);
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveValue(RegistryKey, _extension);
        }
    }
}
