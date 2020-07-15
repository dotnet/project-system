' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    'CONSIDER: move these to the individual page class files, or at least to a separate .vb file

#Region "Com Classes for our property pages"

#Region "Application property pages (VB and C#)"

    'Property page class hierarchy:
    '
    ' ApplicationPropPageBase
    '   + ApplicationPropPageVBBase
    '     + ApplicationPropPageVBWinForms
    '     + ApplicationPropPageVBWPF
    '   + ApplicationPropPage
    '       + CSharpApplicationPropPage
    '

#Region "ApplicationPropPageComClass (Not directly used, inherited from by C#)"

    <Guid("1C25D270-6E41-4360-9221-1D22E4942FAD"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(ApplicationPropPageComClass))>
    Public NotInheritable Class ApplicationPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ApplicationTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(ApplicationPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New ApplicationPropPage
        End Function

    End Class

#End Region

#Region "ApplicationWithMyPropPageComClass (VB Application property page)"

    'Note: This is the VB Application page (naming is historical)
    <Guid("8998E48E-B89A-4034-B66E-353D8C1FDC2E"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(ApplicationWithMyPropPageComClass))>
    Public NotInheritable Class ApplicationWithMyPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ApplicationTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(ApplicationPropPageVBWinForms)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New ApplicationPropPageVBWinForms
        End Function

    End Class

#End Region

#Region "WPFApplicationWithMyPropPageComClass (VB Application page for WPF)"

    <Guid("00aa1f44-2ba3-4eaa-b54a-ce18000e6c5d"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(WPFApplicationWithMyPropPageComClass))>
    Public NotInheritable Class WPFApplicationWithMyPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ApplicationTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(WPF.ApplicationPropPageVBWPF)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New WPF.ApplicationPropPageVBWPF
        End Function

    End Class

#End Region

#Region "CSharpApplicationPropPageComClass (C# Application property page)"

    <Guid("5E9A8AC2-4F34-4521-858F-4C248BA31532"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(CSharpApplicationPropPageComClass))>
    Public NotInheritable Class CSharpApplicationPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ApplicationTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(CSharpApplicationPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New CSharpApplicationPropPage
        End Function

    End Class

#End Region

#End Region

#Region "PackagePropPageComClass (Package property page)"

    <Guid("21b78be8-3957-4caa-bf2f-e626107da58e"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(PackagePropPageComClass))>
    Public NotInheritable Class PackagePropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_PackageTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(PackagePropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New PackagePropPage
        End Function

    End Class

#End Region

#Region "CompilePropPageComClass"

    <Guid("EDA661EA-DC61-4750-B3A5-F6E9C74060F5"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(CompilePropPageComClass))>
    Public NotInheritable Class CompilePropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_CompileTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(CompilePropPage2)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New CompilePropPage2
        End Function

        Protected Overrides Property DefaultSize As Drawing.Size
            Get
                ' This is somewhat hacky, but the compile's size page can sometimes exceed the default
                ' minimum size for a property page. The PropPageDesignerView will query for this in order to
                ' figure out what the minimum autoscrollsize should be set to, but it will also check 
                ' the size of the actual control and use the min of those two values, so as long as we
                ' we return a default size that is larger than what our maximum minimum size will be, we 
                ' should be fine
                Return New Drawing.Size(Integer.MaxValue, Integer.MaxValue)
            End Get
            Set
                MyBase.DefaultSize = Value
            End Set
        End Property

    End Class

#End Region

#Region "ServicesPropPageComClass"

    <Guid("43E38D2E-43B8-4204-8225-9357316137A4"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(ServicesPropPageComClass))>
    Public NotInheritable Class ServicesPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Services
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(ServicesPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New ServicesPropPage
        End Function

    End Class

#End Region

#Region "DebugPropPageComClass"

    <Guid("6185191F-1008-4FB2-A715-3A4E4F27E610"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(DebugPropPageComClass))>
    Public NotInheritable Class DebugPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_DebugTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(DebugPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New DebugPropPage
        End Function

    End Class

#End Region

#Region "VBBasePropPageComClass"

    <Guid("4E43F4AB-9F03-4129-95BF-B8FF870AF6AB"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(ReferencePropPageComClass))>
    Public NotInheritable Class ReferencePropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ReferencesTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(ReferencePropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New ReferencePropPage
        End Function

    End Class

#End Region

#Region "BuildPropPageComClass"

    <Guid("A54AD834-9219-4aa6-B589-607AF21C3E26"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(BuildPropPageComClass))>
    Public NotInheritable Class BuildPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BuildTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(BuildPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New BuildPropPage
        End Function
    End Class

#End Region

#Region "BuildEventsPropPageComClass"

    <Guid("1E78F8DB-6C07-4d61-A18F-7514010ABD56"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(BuildEventsPropPageComClass))>
    Public NotInheritable Class BuildEventsPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BuildEventsTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(BuildEventsPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New BuildEventsPropPage
        End Function
    End Class

#End Region

#Region "ReferencePathsPropPageComClass"

    <Guid("031911C8-6148-4e25-B1B1-44BCA9A0C45C"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(ReferencePathsPropPageComClass))>
    Public NotInheritable Class ReferencePathsPropPageComClass
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ReferencePathsTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(ReferencePathsPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New ReferencePathsPropPage
        End Function
    End Class

#End Region

#Region "CodeAnalysisPropPageComClass (Code Analysis property page)"

    <Guid("c02f393c-8a1e-480d-aa82-6a75d693559d"), ComVisible(True), CLSCompliant(False)>
    <ProvideObject(GetType(CodeAnalysisPropPageComClass))>
    Public NotInheritable Class CodeAnalysisPropPageComClass 'See class hierarchy comments above
        Inherits VBPropPageBase

        Protected Overrides ReadOnly Property Title As String
            Get
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_CodeAnalysisTitle
            End Get
        End Property

        Protected Overrides ReadOnly Property ControlType As Type
            Get
                Return GetType(CodeAnalysisPropPage)
            End Get
        End Property

        Protected Overrides Function CreateControl() As Control
            Return New CodeAnalysisPropPage
        End Function

    End Class
#End Region

#End Region

End Namespace
