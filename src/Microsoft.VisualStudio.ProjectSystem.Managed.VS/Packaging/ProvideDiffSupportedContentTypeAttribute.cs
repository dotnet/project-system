// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
