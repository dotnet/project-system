// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    [Export(typeof(IHttpClient))]
    internal class DefaultHttpClient : IHttpClient
    {
        private static readonly Lazy<HttpClient> s_client = new(CreateClient);

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
