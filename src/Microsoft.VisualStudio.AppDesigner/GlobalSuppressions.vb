' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

' This file is used by Code Analysis to maintain SuppressMessage 
' attributes that are applied to this project.
' Project-level suppressions either have no target or are given 
' a specific target and scoped to a namespace, type, member, etc.

' Baselined for the port, we should revisit these, see: https://github.com/dotnet/roslyn/issues/8183.
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.AppDesCommon.Utils.FocusFirstOrLastTabItem(System.IntPtr,System.Boolean)~System.Boolean")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.AppDesDesignerFramework.DeferrableWindowPaneProviderServiceBase.DesignerWindowPaneBase.OnUndoing(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.AppDesDesignerFramework.DeferrableWindowPaneProviderServiceBase.DesignerWindowPaneBase.OnUndone(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.PropPageBase.Create(System.IntPtr)~System.IntPtr")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.PropPageBase.Deactivate")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropertyPages.PropPageUserControlBase.PostValidation")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropPageDesigner.PropPageDesignerView.InitializeStateOfUICues")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropPageDesigner.PropPageDesignerView.PropertyPagePanel_GotFocus(System.Object,System.EventArgs)")>
<Assembly: CodeAnalysis.SuppressMessage("Reliability", "RS0015:Always consume the value returned by methods marked with PreserveSigAttribute", Justification:="<Pending>", Scope:="member", Target:="~M:Microsoft.VisualStudio.Editors.PropPageDesigner.PropPageDesignerView.UpdateWindowStyles(System.IntPtr)")>
