//--------------------------------------------------------------------------------------------
// ISSLPortProvider
//
// This is a temporary interface to allow the current debug property page (which lives in the 
// dotnet assembly to get the ssl port. The debug page needs to be refactored and when that 
// happens this interface should be removed
//
// Copyright(c) 2015 Microsoft Corporation
//--------------------------------------------------------------------------------------------
namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    internal interface ISSLPortProvider
    {
        int GetAvailableSSLPort(string applicationUrl);
    }
}
