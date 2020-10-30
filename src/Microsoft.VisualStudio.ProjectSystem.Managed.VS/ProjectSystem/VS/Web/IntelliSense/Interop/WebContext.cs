// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

#pragma warning disable RS0016 // TODO:
#pragma warning disable RS0041 // TODO:
#pragma warning disable IDE1006 // Naming Styles
#nullable disable

namespace Microsoft.VisualStudio.Web.Interop
{
    //----------------------------------------------------------------------
    // IVsWebProjectSupport
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("a32703cf-c5b8-438a-a043-8a7eefc06415")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebProjectSupport
    {
        // root url for app
        [PreserveSig]
        int GetWebUrl(
            [Out][MarshalAs(UnmanagedType.BStr)] out string ppWebUrl);

        // root path for app
        [PreserveSig]
        int GetWebPath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string ppWebPath);

        // root url for project
        [PreserveSig]
        int GetWebProjectUrl(
            [Out][MarshalAs(UnmanagedType.BStr)] out string ppWebProjectUrl);

        // root path for project
        [PreserveSig]
        int GetWebProjectPath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string ppWebProjectPath);

        // Used to enable and initialize remote authoring service
        [PreserveSig]
        int GetWebRemoteAuthoringUrl(
            [Out][MarshalAs(UnmanagedType.BStr)] out string ppWebRemoteAuthoringUrl);

        // Used to enable and initialize the client build manager service
        [PreserveSig]
        int GetClientBuildManagerPath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string ppClientBuildManagerPath);

        // Indicates if the web url is to an IIS server
        [PreserveSig]
        int UsesIISWebServer(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool usesIISWebServer);

        [PreserveSig]
        int GetWebUrlService(
            [In][MarshalAs(UnmanagedType.Interface)] IVsWebUrlService pDefaultWebUrlService,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrlService ppWebUrlService);

        [PreserveSig]
        int GetDefaultLanguage(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrLanguage);

        [PreserveSig]
        int OnReferenceAdded(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszReferencePath);

        [PreserveSig]
        int OnFileAdded(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFilePath,
            [In][MarshalAs(UnmanagedType.VariantBool)] bool foldersMustBeInProject);

        [PreserveSig]
        int StartWebAdminTool();

        [PreserveSig]
        int GetWebAssemblyResolveService(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebAssemblyResolveService assemblyResolveService);

        [PreserveSig]
        int GetWebDynamicMasterPageService(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebDynamicMasterPageService dynamicMasterPageService);

        [PreserveSig]
        int GetIISMetabasePath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string strIISMetabasePath);

        [PreserveSig]
        int GetUrlPicker(
            [Out][MarshalAs(UnmanagedType.IUnknown)] out object urlPicker);

        [PreserveSig]
        int IsDesignViewDisabled(
            [In][MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [Out][MarshalAs(UnmanagedType.Bool)] out bool isDesignViewDisabled);

        [PreserveSig]
        int GetNewStyleSheetFolder(
            [Out][MarshalAs(UnmanagedType.BStr)] out string styleSheetFolder);
    }

    //----------------------------------------------------------------------
    // IVsWebProjectSupport2
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("22d35466-f15a-4ecd-91a9-ad28a5b94a88")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebProjectSupport2
    {
        [PreserveSig]
        int CodeFolderAdded(
            [In][MarshalAs(UnmanagedType.LPWStr)] string relativeFolderUrl);

        [PreserveSig]
        int CodeFolderRemoved(
            [In][MarshalAs(UnmanagedType.LPWStr)] string relativeFolderUrl);
    }

    //----------------------------------------------------------------------
    // IVsWebSiteProject
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("965b6b4f-6d36-4c02-816d-81f0c8a6c129")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsWebSiteProject
    {
        [PreserveSig]
        int AddGeneratedFilesFolder(
            [In][MarshalAs(UnmanagedType.U4)] uint itemid,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pwzPath);

        [PreserveSig]
        int RemoveGeneratedFilesFolder(
            [In][MarshalAs(UnmanagedType.U4)] uint itemid);

        [PreserveSig]
        int RefreshGeneratedFilesFolder(
            [In][MarshalAs(UnmanagedType.U4)] uint itemid);

        [PreserveSig]
        int DoGeneratedFilesNeedRefresh(
            [In][MarshalAs(UnmanagedType.U4)] uint itemid,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pwzPath);

        [PreserveSig]
        int OnBeforeProcessWebContextAppDomainReset([In][MarshalAs(UnmanagedType.Bool)] bool bAsync, [In][MarshalAs(UnmanagedType.Bool)] bool btopLevelSyncOnly);

        [PreserveSig]
        int OnAfterProcessWebContextAppDomainReset([In][MarshalAs(UnmanagedType.Bool)] bool bAsync, [In][MarshalAs(UnmanagedType.Bool)] bool btopLevelSyncOnly);

        [PreserveSig]
        int WebContext_OnAppDomainReset([In][MarshalAs(UnmanagedType.I4)] int reason);

        [PreserveSig]
        int WebContext_SetCodeGenDir([In][MarshalAs(UnmanagedType.LPWStr)] string wszCodeGenDir);

        [PreserveSig]
        int WebContext_ForceReferenceUpdate([In][MarshalAs(UnmanagedType.I4)] int reason);

        [PreserveSig]
        int WebContext_ExpectReferenceUpdate([In][MarshalAs(UnmanagedType.I4)] int reason);

        [PreserveSig]
        int WebContext_IsFolderMergeInProgress([In][MarshalAs(UnmanagedType.LPWStr)] string wszPath, [Out][MarshalAs(UnmanagedType.Bool)] out bool pfInProgress);

        [PreserveSig]
        int GetCodeBehindOwner([In][MarshalAs(UnmanagedType.LPWStr)] string wszRelUrl, [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrOwnerRelUrl);

        [PreserveSig]
        int GetCodeBehindFile([In][MarshalAs(UnmanagedType.LPWStr)] string wszRelUrl, [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrCodeBehindRelUrl);
    }
    //----------------------------------------------------------------------
    // IVsWebSiteProject2
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("B45EC41E-5728-4ad5-9A0A-076C78C9BC66")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebSiteProject2
    {
        [PreserveSig]
        int OnWebItemContextAdded([In][MarshalAs(UnmanagedType.U4)] uint itemid, [MarshalAs(UnmanagedType.Interface)] IVsWebItemContext pWebItemContext);
        [PreserveSig]
        int OnWebItemContextRemoved([In][MarshalAs(UnmanagedType.U4)] uint itemid);
    }

    //----------------------------------------------------------------------
    // IVsWebUrlService
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("706FE9A2-4884-4CCB-9132-D86CADAA9373")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebUrlService
    {
        // root url for app
        [PreserveSig]
        int GetAppUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppAppUrl);

        // root url for project
        [PreserveSig]
        int GetPrjUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppPrjUrl);

        // create url from url string
        [PreserveSig]
        int CreateUrl(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebUrl);

        // create url from path string
        [PreserveSig]
        int CreateUrlFromPath(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebUrl);
    }

    //----------------------------------------------------------------------
    // IVsWebUrl
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("13895A1D-38AC-42B5-AE81-6788A467B6EF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebUrl
    {
        // name of url item
        [PreserveSig]
        int GetName(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrName);

        // extension of url item
        // (not used yet)
        [PreserveSig]
        int GetExtension(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrExtension);

        // absolute url "http://server/app/proj/dir/file.ext" or file,ftp,https,ftps
        [PreserveSig]
        int GetAbsolute(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrAbsolute);

        // application relative url "proj/dir/file.ext"
        [PreserveSig]
        int GetAppRelative(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrAppRelative);

        // server relative url "/images/file.ext"
        [PreserveSig]
        int GetSrvRelative(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrSrvRelative);

        // ASP application relative url "~/proj/dir/file.ext"
        [PreserveSig]
        int GetAspRelative(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrAspAppRelative);

        // relative to provided url "subdir/otherfile.ext"
        [PreserveSig]
        int GetRelative(
            [In][MarshalAs(UnmanagedType.Interface)] IVsWebUrl pBaseUrl,
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrRelative);

        // relative to provided url with directory escaping "../otherdir/otherfile.ext"
        [PreserveSig]
        int GetEscapedRelative(
            [In][MarshalAs(UnmanagedType.Interface)] IVsWebUrl pBaseUrl,
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrRelative);

        // Convert url to physical path "C:\vsprojects\app\proj\dir\file.ext"
        [PreserveSig]
        int GetAbsolutePath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrAbsolutePath);

        // Convert url to app relative path "proj\dir\file.ext"
        [PreserveSig]
        int GetAppRelativePath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrAppRelativePath);

        // Convert url to relative path "subdir\otherfile.ext"
        [PreserveSig]
        int GetRelativePath(
            [In][MarshalAs(UnmanagedType.Interface)] IVsWebUrl pBaseUrl,
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrRelativePath);

        // relative to provided url with directory escaping "..\otherdir\otherfile.ext"
        [PreserveSig]
        int GetEscapedRelativePath(
            [In][MarshalAs(UnmanagedType.Interface)] IVsWebUrl pBaseUrl,
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrRelativePath);

        // Project relative url "dir/file.ext"
        [PreserveSig]
        int GetPrjRelative(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrPrjRelative);

        // Project relative path "dir\file.ext"
        [PreserveSig]
        int GetPrjRelativePath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrPrjRelativePath);

        // Project absolute path "C:\vsprojects\app\proj\dir\file.ext"
        [PreserveSig]
        int GetPrjAbsolutePath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrPrjAbsolutePath);

        // returns url with this as base (so if relative resolve relative to this)
        [PreserveSig]
        int GetUrl(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppUrl);

        // returns url to parent (at root returns null)
        [PreserveSig]
        int GetParentUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppParentUrl);

        // returns url to parent (at root returns null)
        [PreserveSig]
        int GetChildUrls(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrls ppChildUrls);

        // ensures the url file/folder is present at the absolute path
        [PreserveSig]
        int EnsureAbsolutePath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrAbsolutePath);

        // returns the contents of a file
        [PreserveSig]
        int GetFileContents(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrContents);

        // returns the contents of a file only if it is open for editing
        [PreserveSig]
        int GetOpenFileContents(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrContents);

        // determines if exists
        [PreserveSig]
        int Exists(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool pfExists);

        // determines if url is to a file
        [PreserveSig]
        int IsFile(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool pfIsFile);

        // determines if url is to a folder
        [PreserveSig]
        int IsFolder(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool pfIsFolder);

        // gets the file associated with the url
        [PreserveSig]
        int GetFile(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFile ppWebFile);

        // gets the folder associated with the url
        [PreserveSig]
        int GetFolder(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFolder ppWebFolder);

        // returns if the url is inside the web application
        [PreserveSig]
        int InApp(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool pfInApp);

        // determines if url is to a file that is loaded in memory and dirty
        [PreserveSig]
        int IsFileDirty(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool pfIsFileDirty);

        // determines if url is to a file that can be edited as part of the project
        int IsFileEditable(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool isEditable);

        // determines if url is to a file that is open for editing
        [PreserveSig]
        int IsFileOpen(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool pfIsFileOpen);
    }

    //----------------------------------------------------------------------
    // IVsWebUrl2
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("133140BD-AE25-455D-87FD-92A7A99098C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebUrl2
    {
        // Ensures the IsFolder/IsFile methods return the desired value (without needing to query the file system)
        [PreserveSig]
        int SetIsFolder(
            [In][MarshalAs(UnmanagedType.Bool)] bool fIsFolder);
    }

    //----------------------------------------------------------------------
    // IVsWebUrls
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("C2CAF19F-F415-4017-A4DA-BEAF40C0FD86")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebUrls
    {
        [PreserveSig]
        int GetItem(
            [In][MarshalAs(UnmanagedType.U4)] uint index,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebUrl);

        [PreserveSig]
        int GetCount(
            [Out][MarshalAs(UnmanagedType.U4)] out uint pCount);
    }

    //----------------------------------------------------------------------
    // IVsWebFolder
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("BB121800-A83B-4D35-A9AA-E5DB55B33A8D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebFolder
    {
        // name of folder
        [PreserveSig]
        int GetName(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrName);

        // parent folder
        [PreserveSig]
        int GetParentFolder(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFolder ppWebFolder);

        // child folders
        [PreserveSig]
        int GetFolders(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFolders ppWebFolders);

        // child files
        [PreserveSig]
        int GetFiles(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFiles ppWebFiles);
    }

    //----------------------------------------------------------------------
    // IVsWebFolders
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("F51731ED-EEE3-4B26-9A5B-F8549D78F371")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebFolders
    {
        [PreserveSig]
        int GetItem(
            [In][MarshalAs(UnmanagedType.U4)] uint index,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFolder ppWebFolder);

        [PreserveSig]
        int GetCount(
            [Out][MarshalAs(UnmanagedType.U4)] out uint pCount);
    }

    //----------------------------------------------------------------------
    // IVsWebFile
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("A2BCCB0B-5B44-499C-B9D0-6B8FFC69201A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebFile
    {
        // name of file
        [PreserveSig]
        int GetName(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrName);

        // (not defined/used yet)
        //[PreserveSig] int GetTimestamp();
        //[PreserveSig] int GetContents();
        //[PreserveSig] int GetSize();
        //[PreserveSig] int GetHash();
        //[PreserveSig] int GetUrl();
        //[PreserveSig] int GetPhysicalPath();

        // parent folder containing file
        [PreserveSig]
        int GetFolder(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFolder ppWebFolder);
    }

    //----------------------------------------------------------------------
    // IVsWebFiles
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("F484133B-FCD4-470F-8737-24E0F39686DF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebFiles
    {
        [PreserveSig]
        int GetItem(
            [In][MarshalAs(UnmanagedType.U4)] uint index,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebFile ppWebFile);

        [PreserveSig]
        int GetCount(
            [Out][MarshalAs(UnmanagedType.U4)] out uint pCount);
    }

    //----------------------------------------------------------------------
    // IVsWebContext
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("dc3d8e47-a003-480f-9804-bdf22b1506eb")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebContext
    {
        [PreserveSig]
        int GetContextService(
            [In] ref Guid SID,
            [In] ref Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface)] out object ppService);

        [PreserveSig]
        int GetWebProjectContext(
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy pHierarchy,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebProjectContext ppWebProjectContext);

        [PreserveSig]
        int GetWebProjectContextFromPath(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebProjectContext ppWebProjectContext);

        [PreserveSig]
        int GetWebProjectContextFromUrl(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebProjectContext ppWebProjectContext);

        [PreserveSig]
        int GetWebItemContext(
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy pHierarchy,
            [In][MarshalAs(UnmanagedType.U4)] uint itemid,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebItemContextFromPath(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebItemContextFromUrl(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebURL(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebUrl);

        [PreserveSig]
        int SetDelegateCLangReferenceManager([In][MarshalAs(UnmanagedType.IUnknown)] object pCLangReferenceManager);

        [PreserveSig]
        int GetDelegateCLangReferenceManager([Out][MarshalAs(UnmanagedType.IUnknown)] out object ppCLangReferenceManager);

        [PreserveSig]
        int ProcessWebContextAppDomainReset([In][MarshalAs(UnmanagedType.Bool)] bool bAsync, [In][MarshalAs(UnmanagedType.Bool)] bool btopLevelSyncOnly);

        [PreserveSig]
        int EnsureCodeDirectoriesInitialized();

        [PreserveSig]
        int RefreshCodeDirectories([In][MarshalAs(UnmanagedType.Bool)] bool async);
    }

    //----------------------------------------------------------------------
    // IVsWebProjectContext
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("74821b0c-95fe-4940-ad1b-0f703cfb19b4")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebProjectContext
    {
        [PreserveSig]
        int GetContextService(

            [In] ref Guid SID,
            [In] ref Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface)] out object ppService);

        [PreserveSig]
        int GetWebContext(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebContext ppWebContext);

        [PreserveSig]
        int GetWebItemContext(
            [In][MarshalAs(UnmanagedType.U4)] uint itemid,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebItemContextFromPath(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebItemContextFromUrl(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebProjectUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebProjectUrl);

        [PreserveSig]
        int GetWebUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebUrl);

        [PreserveSig]
        int UpdateAnchorPath();

        [PreserveSig]
        int CloseProject();

        [PreserveSig]
        int UnloadAppDomain(
            [In][MarshalAs(UnmanagedType.Bool)] bool bWaitForReset);

        [PreserveSig]
        int UpdateProjectReference(
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy pHier);

        [PreserveSig]
        int OnBeforeFileAdd(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileAdded);

        [PreserveSig]
        int OnBeforeFileRemove(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileRemoved);

        [PreserveSig]
        int OnBeforeFileMove(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pwszOldAbsPath,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pwszNewAbsPath);

        [PreserveSig]
        int OnBeforeFileSave(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pwszFileSaved);

        [PreserveSig]
        int GetClientBuildManager(
            [Out] out IntPtr ppvCBM);

        [PreserveSig]
        int OnAsyncLoadComplete();

        [PreserveSig]
        int IsDesignViewDisabled(
            [In][MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [Out][MarshalAs(UnmanagedType.Bool)] out bool isDesignViewDisabled);

        [PreserveSig]
        int GetNewStyleSheetFolder([Out][MarshalAs(UnmanagedType.BStr)] out string pbstrNewStyleSheetFolder);

        [PreserveSig]
        int OwnerClosesProject([In][MarshalAs(UnmanagedType.Bool)] bool bOwnerClosesProject);
    }

    //----------------------------------------------------------------------
    // IVsWebItemContext
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("6d2c329f-dd47-44bb-8a54-07ddb7a94517")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebItemContext
    {
        [PreserveSig]
        int GetContextService(
            [In] ref Guid SID,
            [In] ref Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface)] out object ppService);

        [PreserveSig]
        int GetWebProjectContext(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebProjectContext ppWebProjectContext);

        [PreserveSig]
        int GetWebContext(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebContext ppWebContext);

        [PreserveSig]
        int GetWebItemUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebItemUrl);

        [PreserveSig]
        int GetWebProjectUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebProjectUrl);

        [PreserveSig]
        int GetWebUrl(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebUrl ppWebUrl);

        [PreserveSig]
        int AddFileToIntellisense(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileUrl,         // Url of item to add (could be relative to . or ~/)
            [Out][MarshalAs(UnmanagedType.U4)] out uint pItemID);        // ItemID of the item added.

        [PreserveSig]
        int EnsureFileOpened(
            [In][MarshalAs(UnmanagedType.U4)] uint itemid,          // Itemid of the file to open
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWindowFrame ppFrame); // Window frame of open item

        [PreserveSig]
        int RemoveFileFromIntellisense(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileUrl);        // Url of item to add (could be relative to . or ~/)

        [PreserveSig]
        int GetWebRootPath(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrWebRootPath);   // Returns the path to where the web is rooted.

        [PreserveSig]
        int GetIntellisenseProjectName(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrProjectName);   // Returns unique project name for this intellisense project.

        [PreserveSig]
        int AddDependentAssemblyFile(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileUrl);        // Url of item to add (could be relative to . or ~/)

        [PreserveSig]
        int RemoveDependentAssemblyFile(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFileUrl);        // Url of item to add (could be relative to . or ~/)

        [PreserveSig]
        int ConvertToAppRelPath(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszFilePath,        // Path to convert, possibly relative
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrAppRelPath);    // Returns app relative path

        [PreserveSig]
        int CBMCallbackActive();                     // returns S_FALSE if not in a CBM Callback else S_OK;

        [PreserveSig]
        int WaitForIntellisenseReady();              // Wait until intellisense project is ready, ie VB in bound state

        [PreserveSig]
        int IsDocumentInProject(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrlFilePath,        // Path of item to add (could be relative to . or ~/)
            [Out][MarshalAs(UnmanagedType.U4)] out uint pItemID);        // ItemID of the item in the project.

        [PreserveSig]
        int GetIntellisenseHost(
            [Out] out IntPtr ppIntellisenseHost);

        [PreserveSig]
        int GetCodeIntellisenseHost(
            [Out] out IntPtr ppCodeIntellisenseHost);

        [PreserveSig]
        int UsesAppCodeIntellisenseHost(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool usesAppCodeIntellisenseHost);

        [PreserveSig]
        int GetCodeDomProviderName(
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrCodeDOMProviderName);

        [PreserveSig]
        int EnsureIntellisenseProject();

        [PreserveSig]
        int RefreshIntellisenseProject();

        [PreserveSig]
        int LoadIntellisenseProject();

        [PreserveSig]
        int InitStandaloneCodeFileIntellisense();

        [PreserveSig]
        int CloseIntellisense();

        // Give a way to actively fire IVsIntellisenseProjectEvent
        [PreserveSig]
        int FireStatusChange([In][MarshalAs(UnmanagedType.U4)] uint dwStatus);

        [PreserveSig]
        int FireConfigChange();

        [PreserveSig]
        int FireReferenceChange([In][MarshalAs(UnmanagedType.U4)] uint refChangeType, [In][MarshalAs(UnmanagedType.LPWStr)] string bstrAssemblyPath);

        [PreserveSig]
        int FireCodeFileChange([In][MarshalAs(UnmanagedType.LPWStr)] string bstrOldCodeFile, [In][MarshalAs(UnmanagedType.LPWStr)] string bstrNewCodeFile);

        [PreserveSig]
        int FireOpenHidden();

        [PreserveSig]
        int SetCodeDirectory([In][MarshalAs(UnmanagedType.Bool)] bool bCodeDir, [In][MarshalAs(UnmanagedType.Bool)] bool bWireIntellisense, [In][MarshalAs(UnmanagedType.Bool)] bool bUpdateWebConfig);

        [PreserveSig]
        int IsDesignViewDisabled(
            [Out][MarshalAs(UnmanagedType.Bool)] out bool isDesignViewDisabled);

        [PreserveSig]
        int GetNewStyleSheetFolder(
            [Out][MarshalAs(UnmanagedType.BStr)] out string styleSheetFolder);
    }

    //----------------------------------------------------------------------
    // IVsWebContextService
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("7b19d539-1352-4db0-baf5-44f4a34f3122")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebContextService
    {
        [PreserveSig]
        int GetWebItemContext(
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy pHierarchy,
            [In][MarshalAs(UnmanagedType.U4)] uint itemid,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebProjectContext(
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy pHierarchy,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebProjectContext ppWebProjectContext);

        [PreserveSig]
        int GetWebContext(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszRoot,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebContext ppWebContext);

        [PreserveSig]
        int GetWebContexts(
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebContexts ppWebContexts);

        [PreserveSig]
        int GetWebItemContextFromPath(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);

        [PreserveSig]
        int GetWebItemContextFromUrl(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszUrl,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebItemContext ppWebItemContext);
    }

    //----------------------------------------------------------------------
    // IVsWebAssemblyResolveService
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("9D33C3A7-8483-4E73-BF07-D77B7A5CCC49")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebAssemblyResolveService
    {
        [PreserveSig]
        int GetAssemblyName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszName,
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrName);

        [PreserveSig]
        int GetAssemblyPath(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszName,
            [Out][MarshalAs(UnmanagedType.BStr)] out string pbstrPath);
    }

    //----------------------------------------------------------------------
    // IVsWebDynamicMasterPageService
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("40036A15-95BF-4BC0-AAAD-43C7146B0485")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebDynamicMasterPageService
    {
        [PreserveSig]
        int DetermineMasterPageFile(
            [In][MarshalAs(UnmanagedType.I4)] int pageAttributes,
            [In][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] pageAttributeNames,
            [In][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] pageAttributeValues,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pageUrl,
            [In][MarshalAs(UnmanagedType.Interface)] IVsWebDynamicMasterPageServiceAsyncCallback callback);
    }

    //----------------------------------------------------------------------
    // IVsWebDynamicMasterPageServiceAsyncCallback
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("1747DC4D-B88B-43C0-86F5-0167ED8B8666")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebDynamicMasterPageServiceAsyncCallback
    {
        [PreserveSig]
        int SetMasterPageFile(
            [In][MarshalAs(UnmanagedType.LPWStr)] string masterPageFile);
    }

    //----------------------------------------------------------------------
    // IVsWebContexts
    //----------------------------------------------------------------------
    [ComImport()]
    [Guid("993edbc2-ffb1-4630-a83e-d28ccd144820")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsWebContexts
    {
        [PreserveSig]
        int GetItem(
            [In][MarshalAs(UnmanagedType.U4)] uint index,
            [Out][MarshalAs(UnmanagedType.Interface)] out IVsWebContext ppWebContext);

        [PreserveSig]
        int GetCount(
            [Out][MarshalAs(UnmanagedType.U4)] out uint pCount);
    }
}
