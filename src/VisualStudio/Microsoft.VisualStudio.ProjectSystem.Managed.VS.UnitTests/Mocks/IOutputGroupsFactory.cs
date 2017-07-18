// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Build;
using Moq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal static class IOutputGroupsServiceFactory
    {
        public static IOutputGroupsService Create(string finalOutputPath)
        {
            // These dictionary values come from Microsoft.VisualStudio.ProjectSystem.Build.MetadataNames, which is a private class inside CPS.
            // They're used to implement the GetKeyOutputAsync extension method on IOutputGroupsService. The Key part of the immutable list
            // is not used by this extension, and is left empty.
            var dictionaryBuilder = ImmutableDictionary.CreateBuilder<string, string>();
            dictionaryBuilder.Add("IsKeyOutput", "true");
            dictionaryBuilder.Add("FinalOutputPath", finalOutputPath);
            var dictionaryList = ImmutableList.Create(new KeyValuePair<string, IImmutableDictionary<string, string>>(string.Empty, dictionaryBuilder.ToImmutableDictionary()));

            var outputGroup = new Mock<IOutputGroup>();
            outputGroup.Setup(o => o.IsSuccessful).Returns(true);
            outputGroup.Setup(o => o.Outputs).Returns(dictionaryList);

            var outputGroupsService = new Mock<IOutputGroupsService>();
            outputGroupsService.Setup(o => o.GetOutputGroupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(outputGroup.Object));

            return outputGroupsService.Object;
        }
    }
}
