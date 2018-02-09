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

        [ImportingConstructor]
        public DefaultHttpClient(IProjectThreadingService threadingService)
        {
            _threadingService = threadingService;
        }

        private readonly IProjectThreadingService _threadingService;

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
