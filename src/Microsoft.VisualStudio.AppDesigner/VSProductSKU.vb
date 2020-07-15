' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Microsoft.VisualStudio.Editors.AppDesInterop

Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon

'NOTE: To test property pages under different SKUs, use the PDSku and PDSubSku
'  switches (see common\switches.vb).

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    'Class retries SKU info from the shell to make available
    ' within our assembly
    Public NotInheritable Class VSProductSKU

        Private Shared s_productSKU As VSASKUEdition = VSASKUEdition.None
        Private Shared s_productSubSKU As VSASubSKUEdition = VSASubSKUEdition.None

        'CONSIDER: The preferred way to enable/disable runtime features is now to use
        '  registry keys (which are controlled via FLDB) rather than using the SKU/SubSKU.
        '  Too late to change now, should consider for next version.
        Public Enum VSASKUEdition
            None = 0
            Express = 500 'From vsappid80.idl
            Standard = 1000
            VSTO = 1500   'From vsappid80.idl
            Professional = 2000
            AcademicStudent = 2100
            'AcademicStudentMSDNAA = 2200
            'AcademicTeaching = 2300
            'AcademicEnterprise = AcademicTeaching  ' OBSOLETTE, use AcademicTeaching
            AcademicProfessional = AcademicStudent ' OBSOLETTE, use AcademicStudent
            ' Book                  = 2400,  ' OBSOLETTE
            DownloadTrial = 2500
            Enterprise = 3000
        End Enum

        Public Enum VSASubSKUEdition As Integer
            None = 0
            VC = &H1
            VB = &H2
            CSharp = &H4
            Architect = &H8
            IDE = &H10
            Web = &H40 'from vsappid80.idl
        End Enum

        Private Const VSAPROPID_SKUEdition As Integer = -8534
        Private Const VSAPROPID_SubSKUEdition As Integer = -8546

        ''' <summary>
        ''' Returns the product SKU as an enum.
        ''' </summary>
        Public Shared ReadOnly Property ProductSKU As VSASKUEdition
            Get
                EnsureInited()
                Return s_productSKU
            End Get
        End Property

        ''' <summary>
        ''' Returns the product Sub-SKU as an enum.
        ''' </summary>
        Public Shared ReadOnly Property ProductSubSKU As VSASubSKUEdition
            Get
                EnsureInited()
                Return s_productSubSKU
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is a Standard SKU
        ''' </summary>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsStandard As Boolean
            Get
                EnsureInited()
                Return s_productSKU >= VSASKUEdition.Standard AndAlso s_productSKU < VSASKUEdition.VSTO
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is a VSTO SKU
        ''' </summary>
        Public Shared ReadOnly Property IsVSTO As Boolean
            Get
                EnsureInited()
                Return s_productSKU >= VSASKUEdition.VSTO AndAlso s_productSKU < VSASKUEdition.Professional
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is a Professional SKU
        ''' </summary>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsProfessional As Boolean
            Get
                EnsureInited()
                Return s_productSKU >= VSASKUEdition.Professional AndAlso s_productSKU < VSASKUEdition.Enterprise
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is an Express SKU
        ''' </summary>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsExpress As Boolean
            Get
                EnsureInited()
                Return s_productSKU = VSASKUEdition.Express
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is an Academic SKU
        ''' </summary>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsAcademic As Boolean
            Get
                EnsureInited()
                Return s_productSKU = VSASKUEdition.AcademicStudent
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is an Enterprise SKU
        ''' </summary>
        ''' <remarks>From a macro in vsappid.idl</remarks>
        Public Shared ReadOnly Property IsEnterprise As Boolean
            Get
                EnsureInited()
                Return s_productSKU >= VSASKUEdition.Enterprise
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is a VB SKU
        ''' </summary>
        Public Shared ReadOnly Property IsVB As Boolean
            Get
                EnsureInited()
                Return s_productSubSKU = VSASubSKUEdition.VB
            End Get
        End Property

        ''' <summary>
        ''' Returns True iff this is a VC SKU
        ''' </summary>
        Public Shared ReadOnly Property IsVC As Boolean
            Get
                EnsureInited()
                Return s_productSubSKU = VSASubSKUEdition.VC
            End Get
        End Property

#Region "Private implementation"

        ''' <summary>
        ''' Makes sure that our information on the current SKU has been read, and reads it if not
        ''' </summary>
        Private Shared Sub EnsureInited()
            If s_productSKU = VSASKUEdition.None Then
                If Common.VBPackageInstance IsNot Nothing Then
                    Init(DirectCast(Common.VBPackageInstance, IServiceProvider))
                End If
            End If
        End Sub

        ''' <summary>
        ''' Reads information on the current SKU
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        Private Shared Sub Init(ServiceProvider As IServiceProvider)
            Dim VsAppIdService As IVsAppId
            Dim objSKU As Object = Nothing
            Dim objSubSKU As Object = Nothing
            Dim hr As Integer

            If ServiceProvider Is Nothing Then
                Return
            End If

            VsAppIdService = TryCast(ServiceProvider.GetService(GetType(IVsAppId)), IVsAppId)
            If VsAppIdService IsNot Nothing Then
                Try
                    hr = VsAppIdService.GetProperty(VSAPROPID_SKUEdition, objSKU)
                    If hr >= 0 AndAlso (TypeOf objSKU Is Integer) Then
                        s_productSKU = DirectCast(CInt(objSKU), VSASKUEdition)
                    End If
                    hr = VsAppIdService.GetProperty(VSAPROPID_SubSKUEdition, objSubSKU)
                    If hr >= 0 AndAlso (TypeOf objSubSKU Is Integer) Then
                        s_productSubSKU = DirectCast(CInt(objSubSKU), VSASubSKUEdition)
                    End If
                Catch ex As Exception
                    'ignore for now
                    Debug.Fail("Exception getting SKU from AppId service: " & ex.ToString)
                    Debug.WriteLine(ex.ToString())
                End Try
            End If

#If DEBUG Then
            Trace.WriteLine("Project Designer: SKU detected as " & s_productSKU.ToString())
            Trace.WriteLine("Project Designer: Sub-SKU detected as " & s_productSubSKU.ToString())

            If Common.Switches.PDSku.ValueDefined Then
                Dim NewSku As VSASKUEdition = Common.Switches.PDSku.Value
                Trace.WriteLine("****** PROJECT DESIGNER ONLY: OVERRIDING SKU VALUE TO: " & NewSku.ToString())
                s_productSKU = NewSku
            End If
            If Common.Switches.PDSubSku.ValueDefined Then
                Dim NewSubSku As VSASubSKUEdition = Common.Switches.PDSubSku.Value
                Trace.WriteLine("****** PROJECT DESIGNER ONLY: OVERRIDING SUB-SKU VALUE TO: " & NewSubSku.ToString())
                s_productSubSKU = NewSubSku
            End If
#End If
        End Sub

#End Region

    End Class

End Namespace
