//--------------------------------------------------------------------------------------------
// IIISSettings
//
// Interfaces which represent the iis settings section in LaunchSettings.json
//
// Copyright(c) 2015 Microsoft Corporation
//--------------------------------------------------------------------------------------------
namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public interface IServerBinding
    {
        string ApplicationUrl { get; }
        int SSLPort { get; }
    }

    public interface IIISSettings
    {
        bool WindowsAuthentication { get; }
        bool AnonymousAuthentication { get; }

        // Note that the following can be null if there are no settings defined for it
        IServerBinding IISBinding { get; }
        IServerBinding IISExpressBinding { get; }
    }
}
