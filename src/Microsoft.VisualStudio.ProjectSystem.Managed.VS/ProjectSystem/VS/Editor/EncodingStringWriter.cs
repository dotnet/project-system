// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal class EncodingStringWriter : StringWriter
    {
        private readonly Encoding _encoding;

        public EncodingStringWriter(Encoding encoding)
        {
            Requires.NotNull(encoding, nameof(encoding));
            _encoding = encoding;
        }

        public override Encoding Encoding => _encoding;
    }
}
