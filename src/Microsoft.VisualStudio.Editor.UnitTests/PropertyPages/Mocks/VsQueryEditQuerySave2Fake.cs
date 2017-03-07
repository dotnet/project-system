using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    class VsQueryEditQuerySave2Fake : IVsQueryEditQuerySave2
    {
        public class QueryEditFilesResult
        {
            public tagVSQueryEditResult pfEditVerdict;
            public tagVSQueryEditResultFlags prgfMoreInfo;
            public int hr;

            public QueryEditFilesResult()
            {
                pfEditVerdict = tagVSQueryEditResult.QER_EditOK;
                prgfMoreInfo = (tagVSQueryEditResultFlags)0;
                hr = VSConstants.S_OK;
            }

            public void SetToUserCancel()
            {
                pfEditVerdict = tagVSQueryEditResult.QER_NoEdit_UserCanceled;
                prgfMoreInfo = tagVSQueryEditResultFlags.QER_CheckoutCanceledOrFailed;
                hr = VSConstants.S_OK;
            }

            public void SetToReloaded()
            {
                pfEditVerdict = tagVSQueryEditResult.QER_EditOK;
                prgfMoreInfo = (tagVSQueryEditResultFlags)tagVSQueryEditResultFlags2.QER_Reloaded;
                hr = VSConstants.S_OK;
            }

            public void SetToCheckoutFailed()
            {
                pfEditVerdict = tagVSQueryEditResult.QER_EditNotOK;
                prgfMoreInfo = tagVSQueryEditResultFlags.QER_EditNotPossible;
                hr = VSConstants.S_OK;
            }

        }

        public QueryEditFilesResult Fake_QueryEditFilesResult = new QueryEditFilesResult();

        #region IVsQueryEditQuerySave2 Members

        int IVsQueryEditQuerySave2.BeginQuerySaveBatch()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsQueryEditQuerySave2.DeclareReloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsQueryEditQuerySave2.DeclareUnreloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsQueryEditQuerySave2.EndQuerySaveBatch()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsQueryEditQuerySave2.IsReloadable(string pszMkDocument, out int pbResult)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsQueryEditQuerySave2.OnAfterSaveUnreloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsQueryEditQuerySave2.QueryEditFiles(uint rgfQueryEdit, int cFiles, string[] rgpszMkDocuments, uint[] rgrgf, VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pfEditVerdict, out uint prgfMoreInfo)
        {
            pfEditVerdict = (uint)Fake_QueryEditFilesResult.pfEditVerdict;
            prgfMoreInfo = (uint)Fake_QueryEditFilesResult.prgfMoreInfo;
            return Fake_QueryEditFilesResult.hr;
        }

        int IVsQueryEditQuerySave2.QuerySaveFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo, out uint pdwQSResult)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int IVsQueryEditQuerySave2.QuerySaveFiles(uint rgfQuerySave, int cFiles, string[] rgpszMkDocuments, uint[] rgrgf, VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pdwQSResult)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
