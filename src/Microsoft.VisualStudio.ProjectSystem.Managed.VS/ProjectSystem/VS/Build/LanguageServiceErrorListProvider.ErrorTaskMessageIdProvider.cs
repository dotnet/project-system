// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Build
{
    partial class LanguageServiceErrorListProvider
    {
        /// <summary>
        ///     Generates cookies for task items
        /// </summary>
        internal class ErrorTaskMessageIdProvider
        {
            private int _messageId;

            internal ErrorTaskMessageIdProvider()
                : this(Guid.NewGuid())
            {
            }

            internal ErrorTaskMessageIdProvider(Guid id)
            {
                Requires.Argument(id != Guid.Empty, "id", "Do not pass GUID_NULL to cookieGenerator");

                this.ProviderId = id;
            }

            internal Guid ProviderId
            {
                get;
            }

            internal uint GetMessageId()
            {
                return (uint)Interlocked.Increment(ref this._messageId);
            }
        }
    }
}
