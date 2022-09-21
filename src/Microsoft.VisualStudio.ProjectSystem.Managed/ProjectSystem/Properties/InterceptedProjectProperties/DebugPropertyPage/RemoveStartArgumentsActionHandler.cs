// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages.Designer;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

[Export(typeof(ILinkActionHandler))]
[ExportMetadata("CommandName", "RemoveStartArguments")]
internal class RemoveStartArgumentsActionHandler : ILinkActionHandler
{
    private const string PropertyCommandArgsInUserFile = "StartArguments";
    private const string UserSuffix = ".user";

    private string? _xmlFile;
    private List<string>? _userFileCommandArgs;

    public async Task HandleAsync(UnconfiguredProject project, IReadOnlyDictionary<string, string> editorMetadata)
    {
        _xmlFile = project.FullPath + UserSuffix;

        await RemoveElementAsync();

        await CopyUserArgsToLaunchProfileAsync(project);
    }

    private async Task RemoveElementAsync()
    {
        await TaskScheduler.Default;

        XmlDocument doc = new() { XmlResolver = null };

        try
        {
#pragma warning disable CA3075 // Insecure DTD processing in XML
            doc.Load(_xmlFile);
#pragma warning restore CA3075 // Insecure DTD processing in XML
        }
        catch (Exception ex)
        {
            TraceUtilities.TraceException("Failed to load .user file", ex);
        }

        XmlNodeList xmlNodeList = doc.GetElementsByTagName(PropertyCommandArgsInUserFile);

        for (int i = xmlNodeList.Count - 1; i >= 0; --i)
        {
            _userFileCommandArgs ??= new();
            _userFileCommandArgs.Add(xmlNodeList[i].InnerText);
            xmlNodeList[i].ParentNode.RemoveChild(xmlNodeList[i]);
        }

        try
        {
            doc.Save(_xmlFile);
        }
        catch (Exception ex)
        {
            TraceUtilities.TraceException("Failed to save .user file", ex);
        }

        return;
    }

    private async Task CopyUserArgsToLaunchProfileAsync(UnconfiguredProject project)
    {
        ILaunchSettingsProvider? launchSettingsProvider = project.Services.ExportProvider.GetExportedValue<ILaunchSettingsProvider>();

        ILaunchSettings launchSettings = await launchSettingsProvider.WaitForFirstSnapshot();

        IWritableLaunchSettings writableLaunchSettings = launchSettings.ToWritableLaunchSettings();

        if (SetPropertyValue(writableLaunchSettings))
        {
            await launchSettingsProvider.UpdateAndSaveSettingsAsync(writableLaunchSettings.ToLaunchSettings());
        }
    }

    private bool SetPropertyValue(IWritableLaunchSettings writableLaunchSettings)
    {
        if (writableLaunchSettings is null || writableLaunchSettings.ActiveProfile is null || _userFileCommandArgs is null)
        {
            return false;
        }

        // TODO: Fix: The user may not be editing the active profile.
        foreach (string? commandArg in _userFileCommandArgs)
        {
            writableLaunchSettings.ActiveProfile.CommandLineArgs ??= string.Empty;
            writableLaunchSettings.ActiveProfile.CommandLineArgs += " " + commandArg;
        }

        return true;
    }
}
