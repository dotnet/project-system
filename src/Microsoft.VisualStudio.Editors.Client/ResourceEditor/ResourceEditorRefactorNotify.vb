' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VSDesigner.Common

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' The editor factory for the resource editor.  The job of this class is
    '''   simply to create a new resource editor designer when requested by the
    '''   shell.
    ''' </summary>
    <CLSCompliant(False),
    Guid("0407F754-C199-403e-B89B-1D8E1FF3DC79"),
    ProvideRefactorNotify(GetType(ResourceEditorRefactorNotify), ".resx", "E24C65DC-7377-472b-9ABA-BC803B73C61A")>
    Friend NotInheritable Class ResourceEditorRefactorNotify
        Implements IVsRefactorNotify

        ''' <summary>
        ''' If set to true, tells the shell that symbolic renames are OK. 
        ''' </summary>
        ''' <remarks>
        ''' Normally, we can't handle symbolic renames since we don't update the contents of the .resx
        ''' file (which means that we overwrite the changes the next time the file is generated. 
        ''' In the special case where the designer invokes the symbolic rename, we should allow it.
        ''' 
        ''' Since all the file generation should happen on the main thread, it is OK to have this member shared...
        ''' </remarks>
        Friend Shared AllowSymbolRename As Boolean

        ''' <summary>
        ''' nothing to do since we don't really store any state...
        ''' </summary>
        Friend Sub New()
        End Sub

        ' ******************* Implement IVsRefactorNotify *****************

        ''' <summary>
        ''' Called when a symbol is about to be renamed
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="cRQNames">count of RQNames passed in. This count can be greater than 1 when an overloaded symbol is being renamed</param>
        ''' <param name="rglpszRQName">RQName-syntax string that identifies the symbol(s) renamed</param>
        ''' <param name="lpszNewName">name that the symbol identified by rglpszRQName is being changed to</param>
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeGlobalSymbolRenamed(phier As IVsHierarchy, itemId As UInteger, cRQNames As UInteger, rglpszRQName() As String, lpszNewName As String, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeGlobalSymbolRenamed
            prgAdditionalCheckoutVSITEMIDS = Nothing
            If AllowSymbolRename Then
                Return NativeMethods.S_OK
            Else
                If RenamingHelper.IsRootNamespaceRename(phier, cRQNames, rglpszRQName, lpszNewName) Then
                    Return NativeMethods.S_OK
                Else
                    Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_RenameNotSupported)
                    ' Always return an error code to disable renaming of generated code
                    Return NativeMethods.E_NOTIMPL
                End If
            End If
        End Function

        ''' <summary>
        ''' Called when a method is about to have its params reordered
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="cRQNames">count of RQNames passed in. This count can be greater than 1 when an overloaded symbol is being renamed</param>
        ''' <param name="rglpszRQName">RQName-syntax string that identifies the symbol(s) renamed</param>
        ''' <param name="lpszNewName">name that the symbol identified by rglpszRQName is being changed to</param>
        ''' <returns>error code</returns>
        Private Function OnGlobalSymbolRenamed(phier As IVsHierarchy, itemId As UInteger, cRQNames As UInteger, rglpszRQName() As String, lpszNewName As String) As Integer Implements IVsRefactorNotify.OnGlobalSymbolRenamed
            'VSWhidbey #452759: Always return S_OK in OnGlobalSymbolRenamed.
            Return NativeMethods.S_OK
        End Function

        ''' <summary>
        ''' Called when a method is about to have params added
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method on which params are being added</param>
        ''' <param name="cParams">number of parameters in rgszRQTypeNames, rgszParamNames and rgszDefaultValues</param>
        ''' <param name="rgszParamIndexes">the indexes of the new parameters</param>
        ''' <param name="rgszRQTypeNames">RQName-syntax strings that identify the types of the new parameters</param>
        ''' <param name="rgszParamNames">the names of the parameters</param>
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeAddParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParams As UInteger, rgszParamIndexes() As UInteger, rgszRQTypeNames() As String, rgszParamNames() As String, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeAddParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called after a method has had params added
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method on which params are being added</param>
        ''' <param name="cParams">number of parameters in rgszRQTypeNames, rgszParamNames and rgszDefaultValues</param>
        ''' <param name="rgszParamIndexes">the indexes of the new parameters</param>
        ''' <param name="rgszRQTypeNames">RQName-syntax strings that identify the types of the new parameters</param>
        ''' <param name="rgszParamNames">the names of the parameters</param>
        ''' <returns>error code</returns>
        Private Function OnAddParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParams As UInteger, rgszParamIndexes() As UInteger, rgszRQTypeNames() As String, rgszParamNames() As String) As Integer Implements IVsRefactorNotify.OnAddParams
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called when a method is about to have its params reordered
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being reordered</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where the index in this array is the index to which the param is moving</param>
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeReorderParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeReorderParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called after a method has had its params reordered
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being reordered</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where the index in this array is the index to which the param is moving</param>
        ''' <returns>error code</returns>
        Private Function OnReorderParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger) As Integer Implements IVsRefactorNotify.OnReorderParams
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called when a method is about to have some params removed
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being removed</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where each value indicates the index of the parameter being removed</param>
        ''' <param name="prgAdditionalCheckoutVSITEMIDS">array of VSITEMID's if the RefactorNotify implementor needs to check out additional files</param>
        ''' <returns>error code</returns>
        Private Function OnBeforeRemoveParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger, ByRef prgAdditionalCheckoutVSITEMIDS As Array) As Integer Implements IVsRefactorNotify.OnBeforeRemoveParams
            prgAdditionalCheckoutVSITEMIDS = Nothing
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function

        ''' <summary>
        ''' Called when a method is about to have some params removed
        ''' </summary>
        ''' <param name="phier">hierarchy of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="itemId">itemid of the designer-owned item associated with the code-file that the language service changed</param>
        ''' <param name="lpszRQName">RQName-syntax string that identifies the method whose params are being removed</param>
        ''' <param name="cParamIndexes">number of parameters in rgParamIndexes</param>
        ''' <param name="rgParamIndexes">array of param indexes where each value indicates the index of the parameter being removed</param>
        ''' <returns>error code</returns>
        Private Function OnRemoveParams(phier As IVsHierarchy, itemId As UInteger, lpszRQName As String, cParamIndexes As UInteger, rgParamIndexes() As UInteger) As Integer Implements IVsRefactorNotify.OnRemoveParams
            Common.SetErrorInfo(Common.ServiceProviderFromHierarchy(phier), NativeMethods.E_NOTIMPL, My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ERR_ModifyParamsNotSupported)
            ' Always return an error code to disable parameter modifications for generated code
            Return NativeMethods.E_NOTIMPL
        End Function
    End Class
End Namespace
