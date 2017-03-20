// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using System.Collections.Generic;

using System.Xml;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    [DebuggerDisplay("{Fake_AllText}")]
    public class VsTextBufferFake : IVsTextLines, IVsTextFind, IDisposable
    {
        public int Fake_lockCount;
        public bool isDisposed;

        ~VsTextBufferFake()
        {
            Debug.Assert(isDisposed, "Didn't dispose a VsTextBufferFake instance");
        }

        #region IVsTextLines Members

        public List<string> Fake_Lines = new List<string>();

        public VsTextBufferFake()
        {
        }

        public VsTextBufferFake(string text)
        {
            Fake_ReadFromText(text);
        }

        public void Fake_ReadFromFile(string filename)
        {
            Fake_Lines.Clear();
            Fake_Lines.AddRange(System.IO.File.ReadAllLines(filename));
        }

        public void Fake_ReadFromText(string text)
        {
            Fake_Lines.Clear();
            Fake_Lines.AddRange(text.Split(new string[] { "\r\n" }, StringSplitOptions.None));
        }

        public string Fake_AllText
        {
            get
            {
                int iLastLine, iLastIndex;
                ErrorHandler.ThrowOnFailure(((IVsTextLines)this).GetLastLineIndex(out iLastLine, out iLastIndex));

                string allText = null;
                ErrorHandler.ThrowOnFailure(((IVsTextLines)this).GetLineText(0, 0, iLastLine, iLastIndex, out allText));

                return allText;
            }
        }

        private int HrCheckIndices(int iLine)
        {
            if (iLine < 0)
                return VSConstants.E_INVALIDARG;

            if (iLine >= Fake_Lines.Count)
                return VSConstants.E_INVALIDARG;

            return VSConstants.S_OK;
        }

        private int HrCheckIndices(int iLine, int iIndex)
        {
            int hr = HrCheckIndices(iLine);
            if (ErrorHandler.Failed(hr))
                return hr;

            if (iIndex < 0 || iIndex > Fake_Lines[iLine].Length)
                return VSConstants.E_INVALIDARG;

            return VSConstants.S_OK;
        }

        private int HrCheckIndices(int iStartLine, int iStartIndex, int iEndLine, int iEndIndex)
        {
            int hr = HrCheckIndices(iStartLine, iStartIndex);
            if (ErrorHandler.Failed(hr))
                return hr;

            hr = HrCheckIndices(iEndLine, iEndIndex);
            if (ErrorHandler.Failed(hr))
                return hr;

            if (iStartLine == iEndLine)
            {
                if (iStartIndex > iEndIndex)
                    return VSConstants.E_INVALIDARG;
            }
            else if (iStartLine > iEndLine)
                return VSConstants.E_INVALIDARG;

            return VSConstants.S_OK;
        }

        int IVsTextLines.AdviseTextLinesEvents(IVsTextLinesEvents pSink, out uint pdwCookie)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.CanReplaceLines(int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, int iNewLen)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.CopyLineText(int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, IntPtr pszBuf, ref int pcchBuf)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.CreateEditPoint(int iLine, int iIndex, out object ppEditPoint)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.CreateLineMarker(int iMarkerType, int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, IVsTextMarkerClient pClient, IVsTextLineMarker[] ppMarker)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.CreateTextPoint(int iLine, int iIndex, out object ppTextPoint)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.EnumMarkers(int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, int iMarkerType, uint dwFlags, out IVsEnumLineMarkers ppEnum)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.FindMarkerByLineIndex(int iMarkerType, int iStartingLine, int iStartingIndex, uint dwFlags, out IVsTextLineMarker ppMarker)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetLanguageServiceID(out Guid pguidLangService)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetLastLineIndex(out int piLine, out int piIndex)
        {
            piLine = Fake_Lines.Count - 1;
            piIndex = Fake_Lines[piLine].Length;
            return VSConstants.S_OK;
        }

        int IVsTextLines.GetLengthOfLine(int iLine, out int piLength)
        {
            HrCheckIndices(iLine);
            piLength = Fake_Lines[iLine].Length;
            return VSConstants.S_OK;
        }

        int IVsTextLines.GetLineCount(out int piLineCount)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetLineData(int iLine, LINEDATA[] pLineData, MARKERDATA[] pMarkerData)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetLineDataEx(uint dwFlags, int iLine, int iStartIndex, int iEndIndex, LINEDATAEX[] pLineData, MARKERDATA[] pMarkerData)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetLineIndexOfPosition(int iPosition, out int piLine, out int piColumn)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetLineText(int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, out string pbstrBuf)
        {
            pbstrBuf = "Shouldn't be using this text if it failed";

            int hr = HrCheckIndices(iStartLine, iStartIndex, iEndLine, iEndIndex);
            if (ErrorHandler.Failed(hr))
                return hr;

            if (iStartLine == iEndLine)
            {
                pbstrBuf = Fake_Lines[iStartLine].Substring(iStartIndex, iEndIndex - iStartIndex);
                return VSConstants.S_OK;
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                //First line
                string buf = null;
                ErrorHandler.ThrowOnFailure(((IVsTextLines)this).GetLineText(iStartLine, iStartIndex, iStartLine, Fake_Lines[iStartLine].Length, out buf));
                sb.Append(buf);

                //Middle lines
                for (int i = iStartLine + 1; i < iEndLine; ++i)
                {
                    ErrorHandler.ThrowOnFailure(((IVsTextLines)this).GetLineText(i, 0, i, Fake_Lines[i].Length, out buf));
                    if (i != iStartLine)
                        sb.AppendLine();
                    sb.Append(buf);
                }

                //Last line
                ErrorHandler.ThrowOnFailure(((IVsTextLines)this).GetLineText(iEndLine, 0, iEndLine, iEndIndex, out buf));
                if (iStartLine != iEndLine)
                    sb.AppendLine();
                sb.Append(buf);

                pbstrBuf = sb.ToString();

                return VSConstants.S_OK;
            }
        }

        int IVsTextLines.GetMarkerData(int iTopLine, int iBottomLine, MARKERDATA[] pMarkerData)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetPairExtents(TextSpan[] pSpanIn, TextSpan[] pSpanOut)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetPositionOfLine(int iLine, out int piPosition)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetPositionOfLineIndex(int iLine, int iIndex, out int piPosition)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetSize(out int piLength)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetStateFlags(out uint pdwReadOnlyFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.GetUndoManager(out Microsoft.VisualStudio.OLE.Interop.IOleUndoManager ppUndoManager)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.IVsTextLinesReserved1(int iLine, LINEDATA[] pLineData, int fAttributes)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.InitializeContent(string pszText, int iLength)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.LockBuffer()
        {
            ++Fake_lockCount;
            return VSConstants.S_OK;
        }

        int IVsTextLines.LockBufferEx(uint dwFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.ReleaseLineData(LINEDATA[] pLineData)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.ReleaseLineDataEx(LINEDATAEX[] pLineData)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.ReleaseMarkerData(MARKERDATA[] pMarkerData)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reload(int fUndoable)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.ReloadLines(int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, IntPtr pszText, int iNewLen, TextSpan[] pChangedSpan)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.ReplaceLines(int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, IntPtr pszText, int iNewLen, TextSpan[] pChangedSpan)
        {
            if (pChangedSpan != null)
                throw new NotImplementedException("pChangedSpan != null");

            string newText = Marshal.PtrToStringUni(pszText, iNewLen);
            /*
                        if (iNewLen > newText.Length)
                            return VSConstants.E_INVALIDARG;
                        newText = newText.Substring(iNewLen);
             * */

            string beforeText = null;
            int hr = ((IVsTextLines)this).GetLineText(0, 0, iStartLine, iStartIndex, out beforeText);
            if (ErrorHandler.Failed(hr))
                return hr;

            int iLastLine, iLastIndex;
            hr = ((IVsTextLines)this).GetLastLineIndex(out iLastLine, out iLastIndex);
            if (ErrorHandler.Failed(hr))
                return hr;

            string afterText = null;
            hr = ((IVsTextLines)this).GetLineText(iEndLine, iEndIndex, iLastLine, iLastIndex, out afterText);
            if (ErrorHandler.Failed(hr))
                return hr;

            string oldText = null;
            hr = ((IVsTextLines)this).GetLineText(iStartLine, iStartIndex, iEndLine, iEndIndex, out oldText);
            if (ErrorHandler.Failed(hr))
                return hr;

            string allText = null;
            hr = ((IVsTextLines)this).GetLineText(0, 0, iLastLine, iLastIndex, out allText);
            if (ErrorHandler.Failed(hr))
                return hr;
            Debug.Assert(allText.Equals(beforeText + oldText + afterText,  StringComparison.Ordinal), "Didn't separate buffer correctly");

            string newBufferText = beforeText + newText + afterText;
            Fake_ReadFromText(newBufferText);
            return VSConstants.S_OK;
        }

        int IVsTextLines.ReplaceLinesEx(uint dwFlags, int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, IntPtr pszText, int iNewLen, TextSpan[] pChangedSpan)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved1()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved10()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved2()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved3()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved4()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved5()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved6()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved7()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved8()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.Reserved9()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.SetLanguageServiceID(ref Guid guidLangService)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.SetStateFlags(uint dwReadOnlyFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.UnadviseTextLinesEvents(uint dwCookie)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextLines.UnlockBuffer()
        {
            --Fake_lockCount;
            if (Fake_lockCount < 0)
            {
                Debug.Fail("Unbalanced lock/unlockbuffer");
            }
            return VSConstants.S_OK;
        }

        int IVsTextLines.UnlockBufferEx(uint dwFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        #endregion

        #region IVsTextBuffer Members

        int IVsTextBuffer.GetLanguageServiceID(out Guid pguidLangService)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.GetLastLineIndex(out int piLine, out int piIndex)
        {
            piLine = Fake_Lines.Count - 1;
            return ((IVsTextBuffer)this).GetLengthOfLine(piLine, out piIndex);
        }

        int IVsTextBuffer.GetLengthOfLine(int iLine, out int piLength)
        {
            piLength = Fake_Lines[iLine].Length - 1;
            return VSConstants.S_OK;
        }

        int IVsTextBuffer.GetLineCount(out int piLineCount)
        {
            piLineCount = Fake_Lines.Count - 1;
            return VSConstants.S_OK;
        }

        int IVsTextBuffer.GetLineIndexOfPosition(int iPosition, out int piLine, out int piColumn)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.GetPositionOfLine(int iLine, out int piPosition)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.GetPositionOfLineIndex(int iLine, int iIndex, out int piPosition)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.GetSize(out int piLength)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.GetStateFlags(out uint pdwReadOnlyFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.GetUndoManager(out Microsoft.VisualStudio.OLE.Interop.IOleUndoManager ppUndoManager)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.InitializeContent(string pszText, int iLength)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.LockBuffer()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.LockBufferEx(uint dwFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reload(int fUndoable)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved1()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved10()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved2()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved3()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved4()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved5()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved6()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved7()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved8()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.Reserved9()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.SetLanguageServiceID(ref Guid guidLangService)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.SetStateFlags(uint dwReadOnlyFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.UnlockBuffer()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        int IVsTextBuffer.UnlockBufferEx(uint dwFlags)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        #endregion

        #region IVsTextFind Members

        int IVsTextFind.Find(string pszText, int iStartLine, int iStartIndex, int iEndLine, int iEndIndex, int iFlags, out int piLine, out int piCol)
        {
            piLine = 0;
            piCol = 0;

            int hr = HrCheckIndices(iStartLine, iStartIndex, iEndLine, iEndIndex);
            if (ErrorHandler.Failed(hr))
                return hr;

            if (iFlags != 0)
                throw new NotImplementedException("NYI: non-zero flags on Find");

            for (int i = iStartLine; i <= iEndLine; ++i)
            {
                int startIndexForLine = 0;
                if (i == iStartLine)
                    startIndexForLine = iStartIndex;

                int endIndexForLine = Fake_Lines[i].Length;
                if (i == iEndLine)
                    endIndexForLine = iEndIndex;

                Debug.Assert(endIndexForLine >= startIndexForLine);
                int iFound = (endIndexForLine == startIndexForLine) ? -1 : Fake_Lines[i].IndexOf(pszText, startIndexForLine, endIndexForLine - startIndexForLine);
                if (iFound >= 0)
                {
                    piLine = i;
                    piCol = iFound;
                    return VSConstants.S_OK;
                }
            }

            return VSConstants.E_FAIL;
        }



        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (Fake_lockCount != 0)
            {
                Debug.Fail("Unbalanced lock/unlockbuffer");
                throw new InvalidOperationException("Unbalanced lock/unlockbuffer");
            }
            isDisposed = true;
        }

        #endregion

    }
}
