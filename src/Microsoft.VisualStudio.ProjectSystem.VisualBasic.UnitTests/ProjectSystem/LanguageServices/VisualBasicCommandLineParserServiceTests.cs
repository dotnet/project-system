// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public class VisualBasicCommandLineParserServiceTests : CommandLineParserServiceTestBase
    {
        internal override ICommandLineParserService CreateInstance()
        {
            return new VisualBasicCommandLineParserService();
        }
    }
}
