// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Logging
{
    /// <summary>
    ///     Provides an implementation of <see cref="IProjectLogger"/> that delegates all 
    ///     logging methods onto <see cref="WriteLine(StringFormat)"/>.
    /// </summary>
    internal abstract class AbstractProjectLogger : IProjectLogger
    {
        protected AbstractProjectLogger()
        {
        }

        public abstract bool IsEnabled
        {
            get;
        }

        public void WriteLine(string text)
        {
            WriteLine(new StringFormat(text));
        }

        public void WriteLine(string format, object argument)
        {
            WriteLine(new StringFormat(format, argument));
        }

        public void WriteLine(string format, object argument1, object argument2)
        {
            WriteLine(new StringFormat(format, argument1, argument2));
        }

        public void WriteLine(string format, object argument1, object argument2, object argument3)
        {
            WriteLine(new StringFormat(format, argument1, argument2, argument3));
        }

        public void WriteLine(string format, params object[] arguments)
        {
            WriteLine(new StringFormat(format, arguments));
        }

        protected abstract void WriteLine(StringFormat format);
    }
}
