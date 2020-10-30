// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

#nullable disable
#pragma warning disable RS0016 // TODO:
#pragma warning disable RS0041 // TODO:

namespace Microsoft.VisualStudio.Web.Application
{
    [Guid("3b7eef23-31aa-4c6b-8c40-5f343b558196")]
    [Flags]
    public enum UDC_Flags : int
    {
        UDC_NoFlags = 0,
        UDC_Force = 1,  // Forces update even if document is clean and unchanged
        UDC_Create = 2,  // Create the designer file if not present
    };

    // Taken from webdirprj.idl
    [Flags]
    internal enum ScriptMapInfo
    {
        NoInfo = 0,
        NotRegistered = 1,    // ASP.NET is not registered (aspnet_regiis has not been run)
        NotApplication = 2,    // The website is not marked as an application
        UpdateNotSupported = 4,    // Update is not supported (remote webs)
        NotEnabled = 8,    // IIS6.0 - aspnet has not been enabled on the server.
    };

    [ComImport()]
    [Guid("6c0237e7-5ce4-4504-bc69-f2f1b7e6db18")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebApplicationProject
    {
        [PreserveSig]
        int GetCodeBehindEventBinding(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsCodeBehindEventBinding codeBehindEventBinding);

        [PreserveSig]
        int UpdateDesignerClass(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,
            [In][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] string[] publicFields,
            [In][MarshalAs(UnmanagedType.U4)] UDC_Flags flags);

        [PreserveSig]
        int GetUrlForItem(
            [In][MarshalAs(UnmanagedType.U4)] uint itemid,
            [Out][MarshalAs(UnmanagedType.BStr)] out string itemURL);

        [PreserveSig]
        int GetOpenedUrl(
            [Out][MarshalAs(UnmanagedType.BStr)] out string openedURL);

        [PreserveSig]
        int GetDataEnvironment(
           [Out] out IntPtr ppvDataEnv);

        [PreserveSig]
        int IsBlockingItemTypeResolver(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool isBlockingItemTypeResolver);

        [PreserveSig]
        int BlockItemTypeResolver(
            [In][MarshalAs(UnmanagedType.Bool)] bool blockItemTypeResolver);
    }

    [ComImport()]
    [Guid("05DDC9DC-013C-4ea2-AF1B-64E9F912BB62")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebApplicationProject2
    {
        [PreserveSig]
        int GetIWebApplication(
           [Out] out IntPtr ppvDataEnv);

        [PreserveSig]
        int GetBrowseUrl(
            [Out][MarshalAs(UnmanagedType.BStr)] out string browseURL);
    }

    [ComImport()]
    [Guid("97422BAE-2144-4207-AB16-B4C3B7A2D9A3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsWebAppUpgrade
    {
        void SetProjPathAndHierarchy(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszProjectDirectory,
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy pIVsHier);

        void UpgradeEveAspxFileToWebApp(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        void UpgradeEveProjToWebApp(
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy pIVsHier,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszProjectName,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFolder,
            [In][MarshalAs(UnmanagedType.Bool)] bool bProjFileUpgrade);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsUpgradingProject(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszProjectName);

        [PreserveSig]
        int CreateResourceDocData(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            [Out][MarshalAs(UnmanagedType.SysInt)] out IntPtr punkDocData);

        [PreserveSig]
        int ChangePageAttribute(
                 [In][MarshalAs(UnmanagedType.LPWStr)] string filename,
                 [In][MarshalAs(UnmanagedType.LPWStr)] string attributeName,
                 [In][MarshalAs(UnmanagedType.LPWStr)] string oldValue,
                 [In][MarshalAs(UnmanagedType.LPWStr)] string newValue);

        [PreserveSig]
        int GetScriptMapInfo(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrVersion,
            [In][Out][MarshalAs(UnmanagedType.I4)] ref ScriptMapInfo pScriptMapInfo);

        [PreserveSig]
        int UpdateScriptMaps(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [In][MarshalAs(UnmanagedType.I4)] uint dwTgtMajorVersion,
            [In][MarshalAs(UnmanagedType.I4)] uint dwTgtMinorVersion,
            [In][MarshalAs(UnmanagedType.Bool)] bool isUpgrade);

        [PreserveSig]
        int CreateVirtualDirectory(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszVirtualRootPath,
            [In] bool bSetScriptMaps,
            [In][MarshalAs(UnmanagedType.I4)] uint dwTgtMajorVersion,
            [In][MarshalAs(UnmanagedType.I4)] uint dwTgtMinorVersion);

        [PreserveSig]
        int GetUrlMatchingPath(
           [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
           [In][MarshalAs(UnmanagedType.LPWStr)] string pszDiskPath,
           [Out][MarshalAs(UnmanagedType.BStr)] out string strMatchingUrl);

        [PreserveSig]
        int SetASPNETPermissionsForPath(
           [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrlorPath,
           [In][MarshalAs(UnmanagedType.Bool)] bool bReadAccessOnly);

        [PreserveSig]
        int GetAppOfflineFile(
            [Out][MarshalAs(UnmanagedType.BStr)] out string strOffLineFile);

        [PreserveSig]
        int GetPathForLocalUrl(
           [In][MarshalAs(UnmanagedType.LPWStr)] string pszLocalUrl,
           [Out][MarshalAs(UnmanagedType.BStr)] out string strDiskPath);

        [PreserveSig]
        int WrapServiceProvider(
            [In][MarshalAs(UnmanagedType.Interface)] IVsProject pIVsProj,
            [In][MarshalAs(UnmanagedType.Interface)] OLE.Interop.IServiceProvider pSP,
            [Out][MarshalAs(UnmanagedType.Interface)] out OLE.Interop.IServiceProvider pWrappedSP);

        [PreserveSig]
        int EnsureLocalPathProperty(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszLocalPath);

        [PreserveSig]
        int GetPathForLocalUrlEx(
           [In][MarshalAs(UnmanagedType.LPWStr)] string pszLocalUrl,
           [Out][MarshalAs(UnmanagedType.BStr)] out string strDiskPath,
           [Out][MarshalAs(UnmanagedType.VariantBool)] out bool bIsHostedOnIISExpress);

        [PreserveSig]
        int CreateVirtualDirectoryEx(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszVirtualRootPath,
            [In][MarshalAs(UnmanagedType.VariantBool)] bool bSetScriptMaps,
            [In][MarshalAs(UnmanagedType.I4)] uint dwTgtMajorVersion,
            [In][MarshalAs(UnmanagedType.I4)] uint dwTgtMinorVersion,
            [In][MarshalAs(UnmanagedType.VariantBool)] bool bOverwriteExisting,
            [Out][MarshalAs(UnmanagedType.VariantBool)] out bool bIsHostedOnIISExpress);

        [PreserveSig]
        int GetPageAttribute(
                 [In][MarshalAs(UnmanagedType.LPWStr)] string filename,
                 [In][MarshalAs(UnmanagedType.LPWStr)] string attributeName,
                 [Out][MarshalAs(UnmanagedType.BStr)] out string value);

        [PreserveSig]
        int SetASPNETDataDirectoryPermissions(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszSiteUrl,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszDataDirectory);

        [PreserveSig]
        int SetDirectoryPermissions(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszSiteUrl,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszDataDirectory,
            [In][MarshalAs(UnmanagedType.VariantBool)] bool bIncludeWrite,
            [In][MarshalAs(UnmanagedType.VariantBool)] bool bIncludeExecute);
    }

    /// <summary>
    /// Internal interface the allows our entry to be in the top half of the spashscreen.
    /// </summary>
    [Guid("591E80E4-5F44-11d3-8BDC-00C04F8EC28C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport, ComVisible(true)]
    internal interface IVsMicrosoftInstalledProduct
    {
        [PreserveSig]
        int IdBmpSplash(out uint pIdBmp);
        [PreserveSig]
        int IdIcoLogoForAboutbox(out uint pIdIco);
        [PreserveSig]
        int OfficialName(out string pbstrName);
        [PreserveSig]
        int ProductDetails(out string pbstrProductDetails);
        [PreserveSig]
        int ProductID(out string pbstrPID);
        [PreserveSig]
        int ProductRegistryName(out string pbstrRegName);
    }
}
