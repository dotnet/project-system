// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    internal static class UriUtilities
    {
        /// <summary>
        /// Converts the given httpUrl to an https url with the port specified. Note that it will
        /// throw Uri exceptions if the httpUrl is not valid
        /// </summary>
        public static string MakeSecureUrl(string httpUrl, int sslPort)
        {
            UriBuilder uriBuilder = new UriBuilder(httpUrl);
            uriBuilder.Scheme = Uri.UriSchemeHttps;
            uriBuilder.Port = sslPort;
            return uriBuilder.Uri.AbsoluteUri;
        }

        /// <summary>
        /// Extension method to return whether a uri is a secure one or not
        /// </summary>
        public static bool IsSSLUri(this Uri uri)
        {
            return uri.Scheme.Equals(Uri.UriSchemeHttps);
        }
    }
}
