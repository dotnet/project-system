// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    [Export(typeof(IHttpClient))]
    internal class DefaultHttpClient : IHttpClient
    {
        private static readonly Lazy<HttpClient> s_client = new Lazy<HttpClient>(CreateClient);

        [ImportingConstructor]
        public DefaultHttpClient()
        {
        }

        public Task<string> GetStringAsync(Uri uri)
        {
            return s_client.Value.GetStringAsync(uri);
        }

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Remove("Connection");
            return client;
        }
    }
}
