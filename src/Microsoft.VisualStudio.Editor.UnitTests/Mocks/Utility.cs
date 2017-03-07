using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    static public class Utility
    {

        static string GetCallingMethod()
        {
            StackTrace st = new StackTrace(false);
            return st.GetFrame(2).ToString(); //Skip this function and the one that called it, meaning we get the frame from the method that called the one that called us...
        }

        static private void NotImplementedHelper(string message, string methodName)
        {
            string text = "This functionality has not been implemented in the unit testing framework.";
            if (!string.IsNullOrEmpty(message))
            {
                text += "  " + message;
            }

            text += "\r\n" + "Calling method: " + methodName;
            throw new NotImplementedException(text);
        }

        static public void NotImplemented(string message)
        {
            NotImplementedHelper(message, GetCallingMethod());
        }

        static public void NotImplementedIf(bool f)
        {
            if (f)
            {
                Utility.NotImplemented();
            }
        }

        static public void NotImplemented()
        {
            NotImplementedHelper(null, GetCallingMethod());
        }

        static public void FailTest(string message)
        {
            Unexpected("FAILED: " + message);
        }

        static public void Unexpected(string message)
        {
            string text = "Unexpected error: " + message;
            throw new Exception(text);
        }

        static public void FailTestIfFalse(bool f)
        {
            if (!f)
            {
                Unexpected("Assertion failed in test");
            }
        }

        static public void FailTestIfFalse(bool f, string message)
        {
            if (!f)
            {
                Unexpected(message);
            }
        }

        static public void FailTestOnFailedHResult(int hr)
        {
            if (ErrorHandler.Failed(hr))
            {
                Unexpected("Assertion failed in test");
            }
        }

        static public void FailTestOnFailedHResult(int hr, string message)
        {
            if (ErrorHandler.Failed(hr))
            {
                Unexpected(message);
            }
        }

        static public ICollection GetToStringValuesOfCollection(ICollection collection)
        {
            List<string> toStringValues = new List<string>();
            foreach (object o in collection)
            {
                toStringValues.Add(o.ToString());
            }

            return toStringValues;
        }

#if false
{
    public Function CoCreateInstance(ByVal guid As Guid) As Object
        Dim objectType As Type = Type.GetTypeFromCLSID(guid)
        Dim objectInstance As Object = System.Activator.CreateInstance(objectType)
        return objectInstance
    }
#endif

    }
}
