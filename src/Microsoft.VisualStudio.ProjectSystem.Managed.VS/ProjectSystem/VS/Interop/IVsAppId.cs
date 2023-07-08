// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Interop
{
    [Guid("1EAA526A-0898-11d3-B868-00C04F79F802"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsAppId
    {
        [PreserveSig]
        int SetSite(IOleServiceProvider pSP);

        [PreserveSig]
        int GetProperty(int propid, // VSAPROPID
            [MarshalAs(UnmanagedType.Struct)] out object pvar);

        [PreserveSig]
        int SetProperty(int propid, //[in] VSAPROPID
            [MarshalAs(UnmanagedType.Struct)] object var);

        [PreserveSig]
        int GetGuidProperty(int propid, // VSAPROPID
            out Guid guid);

        [PreserveSig]
        int SetGuidProperty(int propid, // [in] VSAPROPID
            ref Guid rguid);

        [PreserveSig]
        int Initialize();  // called after main initialization and before command executing and entering main loop
    }

    internal enum VSAPropID
    {
        NIL = -1,
        LAST = -8500,  // !!!! NOTE !!!! THIS MUST BE THE SAME AS THE FIRST PROP DEFINED
        GuidAppIDPackage = -8501,  // GUID of the Application ID Package; e.g. this is used to load resource strings.
        AppName = -8502,  // BSTR or I4 localize name of the App used in title bar.
        // (either a string or a resource id that is loaded from GuidAppIDPackage UILibrary)
        CmdLineOptDialog = -8503,  // I4 - Command Line Options dialog resource id in appid package satellite dll
        HideSolutionConcept = -8504,  // BOOL - default FALSE. TRUE if appid uses the solution, but does show it to the user
        // !!! can be called before main initialization happen
        ShowStartupDialogs = -8505,  // BOOL - default TRUE
        ShowIDE = -8506,  // BOOL - default TRUE
        // !!! can be called before main initialization happen
        ShowHierarchyRootInTitle = -8507,  // BOOL - default TRUE
        SolutionFileExt = -8508,  // BSTR - solution file extension (default - ".sln");
        UserOptsFileExt = -8509,  // BSTR - solution options file extension (default - ".suo");
        AltMSODLL = -8510,  // BSTR - path/filename for alternate MSOx DLL (default - ask MSI), exactly as passed to LoadLibrary
        CreateProjShortcuts = -8511,  // BOOL - default TRUE should shortcuts to solutions/projects be added to 'recent' folder?
        AppIcon = -8512,  // I4 - HICON  for 32x32 icon
        AppSmallIcon = -8513,  // I4 - HICON  for 16x16 icon
        DefaultHomePage = -8514,  // BSTR - default Home page URL (for Web browser)
        DefaultSearchPage = -8515,  // BSTR - default Search page URL (for Web browser)
        WBExternalObject = -8516,  // IDispatch * (for IDocHostUIHandler::GetExternal), default SApplicationObject
        AppShortName = -8517,  // BSTR or I4 localize name of the short version of the App name, less than 32 chars.
        ClsidAppIdServer = -8518,  // CLSID under which we're registered as a JIT Debug or Attach Server
        GuidGeneralOutput = -8519,  // GUID of the "General" output window for the shell. First request for this creates it.
        UseDebugLaunchService = -8520,  // default FALSE. TRUE if debugger should use SVsDebugLaunch launch service
        GuidDefaultDebugEngine = -8521,  // GUID of the default debug engine for this appid
        CmdLineOptStrFirst = -8522,  // I4 - beginning of res id range of the Command Line Options string resource(s) in appid package
        // satellite dll. used instead of CmdLineOptDialog when output piped to console
        CmdLineOptStrLast = -8523,  // I4 - end of res id range of the Command Line Options string resource(s) in appid package
        // satellite dll. used instead of CmdLineOptDialog when output piped to console
        IsRegisteredAsRuntimeJITDebugger = -8524,  // used to register as a runtime JIT Debugger
        PersistProjExplorerState = -8525,  // BOOL - default is TRUE.  Persists expansion state of the project explorer
        PredefinedAliasesID = -8526,  // Deprecated -- use PredefinedAliasesString instead
        //   (was I4 - resource id in appid package satellite dll of predefined aliases text)
        DisableDynamicHelp = -8527,  // BOOL - default is FALSE.  Should the Dynamic Help window be shown on F1
        UsesMRUCommandsOnFileMenu = -8528,  // BOOL - default is TRUE.  Are the MRU commands on the File menu used?
        AllowsDroppedFilesOnMainWindow = -8529,  // BOOL - default is TRUE.  Should the main window accept dropped files (i.e., WS_EX_ACCEPTFILES)?
        DisableAnswerWizardControl = -8530,  // BOOL - default is TRUE.  Should the AnswerWizard menubar control be disabled?
        DisableAnsiCodePageCheck = -8531,  // BOOL - default is FALSE.  Should the Ansi codepage check be used when loading UI libraries?
        DisableInstructionUnitStepping = -8532,  // Set to TRUE to disable debugger's support for source-instruction stepping.
        UseVisualStudioDialogShortcuts = -8533,  // BOOL - default is TRUE.  Should the VS shortcuts be used in the Open/Save/Browse dialogs?
        SKUEdition = -8534,  // Either a VSASKUEdition or a string. VSASKUEdition if it is a standard version, or a BSTR if a custom version.
        Logo = -8535,  // BSTR - logo for command line
        DDEApplication = -8536,  // BSTR - application supported in DDE (expected in WM_DDE_INITIATE).  Required for DDE support.
        DDETopic = -8537,  // BSTR - topic supported in DDE (expected in WM_DDE_INITIATE)  Required for DDE support.
        VSIPLicenseRequired = -8538,  // BOOL - default is FALSE.  If TRUE, about box puts up stuff about VSIP license required
        DropFilesOnMainWindowHandler = -8539,  // GUID - package GUID, which implements IVSDropFilesHandler to override default behaviour
        CmdLineError = -8540,  // BSTR or I4 - error message for invalid cmd line to show before cmd line options help
        // (either a string or a resource id that is loaded from GuidAppIDPackage UILibrary)
        AllowCurrentUserSafeDomains = -8541,  // BOOL - default is FALSE. Should security manager add safe domains from HKCU\<appid hive>\VsProtocol\SafeDomains
        TechSupportLink = -8542,  // BSTR - should be link to tech support for this appid.
        HideMiscellaneousFilesByDefault = -8543,  // BOOL - default is FALSE.  Should the Miscellaneous Files project be hidden by default?
        PredefinedAliasesString = -8544,  // BSTR - predefined aliases for the appid
        ShowRuntimeInAboutBox = -8545,   // BOOL - default is FALSE.  Should runtime (and runtime ver) show up at the top of about box?
        SubSKUEdition = -8546,  // I4 - some combination of the bits defined in VSASubSKUEdition or zero (if none).
        StatusBarClientText = -8547,   // BSTR global (application) scoped text for Client Text field of status bar.
        NewProjDlgSlnTreeNodeTitle = -8548,  // BSTR or I4 localized replacement name for the 'Visual Studio Solutions' node in the 'Project Types' tree in
                                             // the New Project dialog. (either a string or a resource id that is loaded from GuidAppIDPackage UILibrary)
        DefaultProjectsLocation = -8549,  // BSTR full path to the projects location (overrides the 'Visual Studio Projects' location)
        SolutionFileCreatorIdentifier = -8550,  // BSTR string used as the second line in the solution file (used for determining SLN double-click behavior)
        HideSolutionExplorerToolbar = -8551,  // BOOL - default is FALSE. Should the Solution Explorer tool window hide its toolbar?
        DefaultUserFilesFolderRoot = -8552,  // BSTR name of folder at the end of the default my documents location, e.g. 'Visual Studio' in the default case: '%USERPROFILE%\My Documents\Visual Studio'
        UserFilesSubFolderName = -8553,  // BSTR name of folder used for appid-specific subfolders under '%USERPROFILE%\My Documents\Visual Studio', e.g. 'Visual Basic Express' for '%USERPROFILE%\My Documents\Visual Studio\Settings\Visual Basic Express'
        NewProjDlgInstalledTemplatesHdr = -8554,  // BSTR or I4 localized replacement name for the 'Visual Studio installed templates' header in the 'Templates' list
                                                  // in the New Project dialog. (either a string or a resource id that is loaded from GuidAppIDPackage UILibrary)
        IncludeAddNewOnlineTemplateIcon = -8555,  // BOOL - default is TRUE. Should the "Add New Online Template" icon be added to the New Project/Item dialogs?
                                                  // Note: if this icon is not added then the "My Templates" group only shows up if other user templates are added.
        AddinsAllowed = -8556,  // VARIANT_BOOL indicating wether Add-ins can be loaded or not. If not implemented, then VARIANT_TRUE is assumed.
        App64Icon = -8557,  // I4 - HICON  for 64x64 icon
        UseAutoRecovery = -8558,  // BOOL - default is TRUE. In order to turn off AutoRecovery, an AppID should implement this
                                  // propid and set its value to FALSE.
        DisableOutputWindow = -8559,  // VARIANT_BOOL indicating whether shell should treat the output window as disabled. Returning VARIANT_TRUE means that
                                      // solution build manager will not try to output anything into the output window and 'Show Output window when build starts' will be hidden
                                      // in the Options dialog. Default value is VARIANT_FALSE.
        DisableStartPage = -8560,  // VARIANT_BOOL indicating whether we should disable the start page in the shell
        StartPageTheme = -8561,  // INT_PTR pointing to the memory containing the VSSTARTPAGETHEME struct. The memory should be allocated and de-allocated at the
                                 // appid implementation level.
        LicenseGUID = -8562,  // Returns the highest applicable license GUID, if licenses are required.
                              // If no licenses are required, returns E_NOTIMPL.
        RegistrationDlgBanner = -8563,  // The banner on top of the Registration and Trial dialogs.
        AutoRecoveryTookPlace = -8564,  // VARIANT_BOOL indicating if an AutoRecovery took place. The default value is VARIANT_FALSE, and it is set to VARAINT_TRUE
                                        // iff an AutoRecovery happened, before the DTEEvents::OnStartupComplete event is fired. An AppID should query this value
                                        // if they need to know if a Recovery took place to change their startup action, for example
        AboutBoxTheme = -8565,  // INT_PTR pointing to the memory containing the VSABOUTBOXTHEME struct. The memory should be allocated and de-allocated at the
                                // appid implementation level.
        SQMTitle = -8566,  // BSTR Title for the SQM optin dialog.
        AutoSaveNewUnsavedFiles = -8567,  // VARIANT_BOOL indicating whether the disaster recovery mechanism should save previously unsaved files.  The default is FALSE.
        Preview = -8568,  // I4 Enumeration indicating whether this is:
                          // 0: full release
                          // 1: CTP
                          // 2: Beta
                          // 3: RC
        DaysUntilExpiration = -8569,   // I4 Days until expiration:
                                       // <n> days
                                       // -1 if already expired.
                                       // if this is a full release, ie. not expiring, always returns 0
        ReleaseString = -8570,     // BSTR what this release is branded as, e.g. November CTP, Beta 2, etc.
        ReleaseString_Short = -8571,     // BSTR what this release is branded as, e.g. November CTP, Beta 2, etc.
        RegistryRoots = -8572,    // SafeArray of BSTRs, in order from earliest to latest
        DisableUACSupport = -8573,
        RunAsNormalUser = -8574,    // VT_BOOL. TRUE if machine-wide registry values should be moved under HKEY_CURRENT_USER
                                    //          and common app-data files are written under per-user app-data with a
                                    //          "Configuration" or "UserSettings" subkey/subfolder
        ConfigurationRoot = -8575,    // VT_BSTR  Alternative registry root to use when user settings and machine configuration
                                      //          need to be different. If not implemented, then use the default registry root.
        DontWriteToUserAppData = -8576,    // VT_BOOL. TRUE if we should not write to the user's appdata folder
                                           //          This might be TRUE in a "kiosk" application where the application leaves no
                                           //          trace of the user behind on the machine.
        SQMLogFile = -8577,    // BSTR full name of the SQM log created for the current session.
        SupportRestartManager = -8578,    // VT_BOOL (default is TRUE). In order to turn off support for Restart Manager, an AppID should implement this
        SamplesURL = -8579,    // BSTR URL to show in the internal web browser for Help - Samples command
        AppDataDir                      = -8580,    // BSTR (Remote) application data directory
        LocalAppDataDir                 = -8581,    // BSTR Local application data directory
        CommonAppDataDir                = -8582,    // BSTR common (all users) application data directory
        ConfigurationTimestampUtc       = -8583,    // VT_DATE value that represents the last time the configuration cache was built
        //         of Visual Studio was initializing
        CommonExtensionSearchPath       = -8584,    // SafeArray of BSTRs. APPID specific list of folders where to look for Common (shared by all users) VS extensions.
        //                     VS Extension Manager looks under these locations for VSIX manifest files.
        UserExtensionsRootFolder        = -8585,    // BSTR. APPID specific folder path for User extensions. VS Extension Manager
        //       looks under this location for VSIX manifest files.
        LoadUserExtensions              = -8586,    // VT_BOOL. Tells PkgDef management and Extension Manager API whether to load User extensions.
        //          This property is calculated based on the security logic of extension management and user preferrences.
        LoadedUserExtensions            = -8587,    // SafeArray of BSTRs. List of folders that were searched for enabled user extensions.
        //                     These are the essentially the user extensions that were enabled when the appid initialized.
        AllowLoadingAllPackages         = -8588,    // VT_BOOL. Each APPID specifies through this property if it allows loading ALL Visual Studio Packages
        //          without PLK checking. Default is FALSE.

        RunningInSafeMode               = -8589,    // VT_BOOL. Specifies whether the AppID is running in safe mode.

        VSAPROPID_ProductFamily         = -8590,    // I4. See PIDFamily enum in DDConfig.h for list of valid values.
        VSAPROPID_SplashScreenTheme     = -8591,    // INT_PTR pointing to the memory containing the VSSPLASHSCREENTHEME struct. The memory should be allocated and
        // de-allocated at the appid implementation level.
        VSAPROPID_RequiresElevation     = -8592,    // VT_BOOL. True means the appid always requires elevation
        //          False means the appid never requires elevation
        //          Default means the appid doesn't care, allow msenv to make the decision based on other factors (command line switches, etc.)
        VSAPROPID_ApplicationRootFolder     = -8593,    // BSTR Full path of root location of installation (e.g. drive>:\Program Files\Microsoft Visual Studio <version>\)
        VSAPROPID_ApplicationExtensionsFolder = -8594,    // BSTR Full path of folder for installing per-machine Extensions (e.g. Example: C:\Program Files\Microsoft Visual Studio <VS version>\Common7\IDE\Extensions)
        VSAPROPID_GenericTheme          = -8595,    // INT_PTR pointing to the memory containing the VSGENERICTHEME struct. The memory should be allocated and
        // de-allocated at the appid implementation level.
        VSAPROPID_ActivityLogPath       = -8596,    // VT_BSTR, Read-Only. Path to ActivityLog file.
        VSAPROPID_ReleaseVersion = -8597,    // VT_BSTR, Read-Only. The build version of the release and the branch/machine/user information used to build it (e.g. "10.0.30319.01 RTMRel" or "10.0.30128.1 BRANCHNAME(COMPUTERNAME-USERNAME)"). This is the same as the release string shown in Help/About.
        VSAPROPID_EnableSamples = -8598,    // VT_BOOL. Specifies whether samples are enabled. Defaults to false if not specified for Isolated Shell appids.
        VSAPROPID_EnableMicrosoftGalleries = -8599,  // VT_BOOL. Specifies whether Microsoft-owned extension galleries are enabled. Defaults to false if not specified for Isolated Shell appids.
        VSAPROPID_EnablePrivateGalleries = -8600,    // VT_BOOL. Specifies whether private extension galleries are enabled. Defaults to false if not specified for Isolated Shell appids.
        VSAPROPID_AppVectorIcon = -8601,    // VT_BSTR. Gets a vector path for an icon. This vector path must conform to the path markup syntax used by System.Windows.Media.Geometry.
        VSAPROPID_AppBrandName = -8602,    // VT_BSTR. The localized full brand name of the application, including SKU information. E.g. "Microsoft Visual Studio Professional 2012 RC" or "Microsoft Visual Studio Express 2012 RC for Windows 8"
        VSAPROPID_AppShortBrandName = -8603,    // VT_BSTR. A short version of VSAPROPID_AppBrandName, less than 32 chars. E.g. "VS Pro 2012 RC" or "VS Express 2012 RC for Win8"
        VSAPROPID_SKUInfo = -8604,    // VT_BSTR. A localized text describing the current SKU (name, year, release type, etc). E.g. "Ultimate 2012 RC" or "Express 2012 RC for Web"
        VSAPROPID_GuidDefaultColorTheme = -8605,    // GUID representing the color theme that should be used by default for the appid. If unimplemented by the appid, or if the theme does not exist when the appid is launched, the default light theme is chosen.
        VSAPROPID_ActivityLogServiceObject = -8606,    // VT_UNKNOWN. IUnknown the free thread activity log service object.
        VSAPROPID_AppUpdateIcon = -8607,    // VT_INT_PTR - HICON for SM_CXICON x SM_CYICON app update icon.
        VSAPROPID_AppUpdateSmallIcon = -8608,    // VT_INT_PTR - HICON for SM_CXSMICON x SM_CYSMICON app update icon.
        VSAPROPID_AppUpdate64Icon = -8609,    // VT_INT_PTR - HICON for 64 x 64 app update icon.
        VSAPROPID_IsSubscriptionAware = -8610,    // VT_BOOL. Specifies whether the application supports subscription license from VS Online
        VSAPROPID_SubscriptionLicenseId = -8611,    // GUID unique LicenseID that application specifies under $RootFolder$\Licenses for coordinating its VS Online subscription tokens.
        VSAPROPID_SubscriptionRightsName = -8612,    // VT_BSTR. Unique Name that identifies this application with the VS Online Licensing Service.
        VSAPROPID_SupportsConnectedUser = -8613,    // VT_BOOL. Specifies whether the application supports Connected User UI (e.g. Connected User sign-in, ID Card, roaming settings, first launch sign-in invitation, etc.)
        VSAPROPID_SettingsRegistryRoots = -8614,    // SafeArray of BSTRs, in order from earliest to latest, including current version, of registry roots checked during settings migration
        VSAPROPID_EnableOfflineHelpNotification = -8615,    // VT_BOOL. Specifies whether the help notification should be published to the notification hub on first launch
        VSAPROPID_DefaultProfile = -8616,    // VT_BSTR (optional). Specifies the default profile for the appid (e.g. "General")
        VSAPROPID_ThemeThumbnailProvider = -8617,    // VT_UNKNOWN (optional). Specifies an IUnknown from which the IVsThemeThumbnailProvider interface for the appid can be queried.
        VSAPROPID_CommunityEdition = -8618,    // VT_BOOL. Specifies whether VS is community edition. Only applicable to VS Professional.
        VSAPROPID_LicenseURL = -8619,    // BSTR URL to show in the About box for the license terms
        VSAPROPID_EditionName = -8620,    // BSTR Name to be used for the APPID in return value of DTE.Edition property
        VSAPROPID_AppQueryLoadServiceObject = -8621,    // VT_UNKNOWN. IUnknown the free threaded app query load service object.
        VSAPROPID_IsVSTelemetryEnabled = -8622,    // VT_BOOL. Specifies whether the VS Telemetry API is enabled in the SKU or not.
        VSAPROPID_WorkingFodersRootExt = -8623,             // BSTR - solution working folders extension(default - "");
        VSAPROPID_UnlocalizedReleaseString_Short = -8624,   // BSTR what this release is branded as, e.g. November CTP, Beta 2, etc. (unlocalized)
                                                            //      For the localized version of this string, use VSAPROPID_ReleaseString_Short
        VSAPROPID_EnableNoToolWinMode = -8625,              // VT_BOOL. Specifies whether the AppId enables NoToolWin mode.
        VSAPROPID_InIsolationMode = -8626,                  // VT_BOOL. Specifies whether the AppId is running in isolation.
        VSAPROPID_IsolationInstallationName        = -8627,     // VT_BSTR. The AppId's isolation installation name.
        VSAPROPID_IsolationInstallationId          = -8628,     // VT_BSTR. The AppId's isolation installation id.
        VSAPROPID_IsolationInstallationVersion     = -8629,     // VT_BSTR. The AppId's isolation installation version.
        VSAPROPID_IsolationInstallationWorkloads   = -8630,     // VT_BSTR. The AppId's isolation installation workloads.
        VSAPROPID_IsolationInstallationPackages    = -8631,     // VT_BSTR. The AppId's isolation installation packages.
        VSAPROPID_IsolationInstallationUserDataFilePath = -8632,     // VT_BSTR. The AppId's isolation installation userdata file path.
        VSAPROPID_IsolationInstallationLogsDirectory    = -8633,     // VT_BSTR. The AppId's isolation installation logs directory.
        VSAPROPID_SetupEngineFilePath                   = -8634,     // VT_BSTR. The Setup Engine file path;.
        VSAPROPID_LegacyCompatDirectory                 = -8635,     // VT_BSTR. The root legacy compat directory that MSIs that are not isolation aware may install things to
        VSAPROPID_CommonExtensionExclusionList          = -8636,     // SafeArray of BSTRs. A list of directories to exclude from extension processing (pkgdef, MEF, etc..)
        VSAPROPID_SetupIsValid                          = -8637,     // VT_BOOL. Specifies whether Setup finished correctly.
        VSAPROPID_ChannelId                             = -8638,     // VT_BSTR. The AppId's installation channel ID, for example VisualStudio.15.Release
        VSAPROPID_ChannelManifestId                     = -8639,     // VT_BSTR. The AppId's installation channel manifest unique ID, for example VisualStudio.15.Release/public.d15rel/15.0.26020.0
        VSAPROPID_InstallationNickname                  = -8640,     // VT_BSTR. The AppId's installation nickname to disambiguate between SxS installations.
        VSAPROPID_ProductDisplayVersion                 = -8641,     // VT_BSTR. The AppId's product display version.
        VSAPROPID_ProductSemanticVersion                = -8642,     // VT_BSTR. The AppId's product semantic version.
        VSAPROPID_ChannelTitle                          = -8643,     // VT_BSTR. The AppId's installation channel title.
        VSAPROPID_ChannelSuffix                         = -8644,     // VT_BSTR. The AppId's installation channel suffix.
        VSAPROPID_AlphaPacksCount                       = -8645,     // VT_BSTR. The number of alpha-packs this installation has.
        VSAPROPID_CampaignId                            = -8646,     // VT_BSTR. The campaign id associated with this install.
        VSAPROPID_AppHostVersion                        = -8647,     // VT_BSTR. The AppId's host version, preferred by _DTE.Version property.
        VSAPROPID_SKUName                               = -8648,     // VT_BSTR. The SkuName, unlocalized and sent with Telemetry.
        VSAPROPID_BranchName                            = -8649      // VT_BSTR. The branch name of the build.
    }
}
