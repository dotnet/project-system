// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities
{
    public class DefaultHttpClient
    {
        public DefaultHttpClient()
        {
        }

        public async Task<string> GetStringAsync(Uri uri)
        {
            using (HttpClient client = CreateClient())
            {
                return await client.GetStringAsync(uri).ConfigureAwait(false);
            }
        }

        private HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Remove("Connection");
            return client;
        }
    }
}
