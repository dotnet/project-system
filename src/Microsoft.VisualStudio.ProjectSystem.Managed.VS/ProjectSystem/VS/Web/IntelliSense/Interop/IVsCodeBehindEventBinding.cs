// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IVsHierarchy = Microsoft.VisualStudio.Shell.Interop.IVsHierarchy;

#pragma warning disable RS0016 // TODO:

namespace Microsoft.VisualStudio.Web.Application
{
    [ComImport()]
    [Guid("0e686aef-7878-42e2-b58e-ea6857eb9791")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsCodeBehindEventBinding
    {
        // Initialize the codebehind event binder
        //
        [PreserveSig]
        int Initialize(
            [In][MarshalAs(UnmanagedType.Interface)] IOleServiceProvider oleServiceProvider,
            [In][MarshalAs(UnmanagedType.Interface)] IVsHierarchy hierarchy);

        // Shut down codebehind event binder
        //
        [PreserveSig]
        int Close();

        // Generates a valid new event handler method name for the specified class and event
        //
        [PreserveSig]
        int CreateUniqueEventHandlerName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectName,               // Button1
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [Out][MarshalAs(UnmanagedType.BStr)] out string eventHandlerName);        // Button1_Click

        // Verifies if the specified event handler exists in the class.
        //
        [PreserveSig]
        int IsExistingEventHandler(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectTypeName,           // System.Web.UI.WebControls.Button
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventHandlerName,         // Button1_Click 
            [Out][MarshalAs(UnmanagedType.Bool)] out bool isExistingEventHandler);

        // Creates a new event handler in the class with the specified name and signature.
        //
        [PreserveSig]
        int CreateEventHandler(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectTypeName,           // System.Web.UI.WebControls.Button
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventHandlerName);        // Button1_Click 

        // Opens the specified file, makes it the active document, and places the caret
        // in the specified event handler.
        //
        // If the handler can't be found it should still display the document if possible.
        //
        [PreserveSig]
        int ShowEventHandler(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectTypeName,           // System.Web.UI.WebControls.Button
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventHandlerName);        // Button1_Click 

        // Returns the names of all class methods matching the requested event handler signature.
        //
        [PreserveSig]
        int GetCompatibleEventHandlers(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectTypeName,           // System.Web.UI.WebControls.Button
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] eventHandlerNames);

        // Returns if the language supports static event binding (for instance VB Handles clauses).
        //
        [PreserveSig]
        int IsStaticEventBindingSupported(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [Out][MarshalAs(UnmanagedType.Bool)] out bool isStaticEventBindingSupported);

        // Returns the event handler name if the specified event is static bound.
        //
        [PreserveSig]
        int GetStaticEventBinding(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectName,               // Button1
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [Out][MarshalAs(UnmanagedType.BStr)] out string eventHandlerName);        // Button1_Click

        // Removes the specified static event binding from the object in the class.
        //
        [PreserveSig]
        int RemoveStaticEventBinding(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectName,               // Button1
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventHandlerName);        // Button1_Click 

        // Adds the specified static event binding to the object in the class.
        //
        [PreserveSig]
        int AddStaticEventBinding(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectTypeName,           // System.Web.UI.WebControls.Button
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectName,               // Button1
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventHandlerName);        // Button1_Click 
    }

    [ComImport()]
    [Guid("D18CA7EE-EEFE-4895-8B7C-8336979E5351")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsCodeBehindEventBinding2 : IVsCodeBehindEventBinding
    {
        // Creates a new event handler in the class with the specified name and signature.
        //
        [PreserveSig]
        int CreateEventHandler(
            [In][MarshalAs(UnmanagedType.LPWStr)] string document,                 // C:\Web1\WebForm1.aspx
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehind,               // WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string codeBehindFile,           // C:\Web1\WebForm1.aspx.vb
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,                // Web1.WebForm1
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectTypeName,           // System.Web.UI.WebControls.Button
            [In][MarshalAs(UnmanagedType.LPWStr)] string objectName,               // Button1
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventName,                // Click
            [In][MarshalAs(UnmanagedType.LPWStr)] string eventHandlerName,         // Button1_Click 
            [In][MarshalAs(UnmanagedType.Bool)] bool addStaticEventBinding);     // Add static event binding
    }

    [ComImport()]
    [Guid("A61CAE96-2429-4304-BBE5-698581E93935")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsCodeEventBinding
    {
        [PreserveSig]
        int GetCompatibleMethods(
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,
            [In][MarshalAs(UnmanagedType.LPWStr)] string signature,
            [Out][MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] eventHandlerNames);

        [PreserveSig]
        int CreateUniqueMethodName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,
            [In][MarshalAs(UnmanagedType.LPWStr)] string componentID,
            [In][MarshalAs(UnmanagedType.LPWStr)] string methodNameBase,
            [Out][MarshalAs(UnmanagedType.BStr)] out string uniqueMethodName);

        [PreserveSig]
        int CreateMethod(
            [In][MarshalAs(UnmanagedType.LPWStr)] string className,
            [In][MarshalAs(UnmanagedType.LPWStr)] string methodName,
            [In][MarshalAs(UnmanagedType.LPWStr)] string content);
    }
}
