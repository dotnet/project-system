// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal sealed class DispatchThread
    {
        private Thread _thread;
        private Dispatcher _dispatcher;
        private readonly object _invokeSyncRoot = new object();
        private Exception _invokeException;
        private bool _isInvoking;
        private bool _isClosed;

        internal DispatchThread()
        {
            using (var resetEvent = new AutoResetEvent(false))
            {
                _thread = new Thread(delegate ()
                {
                    // This is necessary to make sure a dispatcher exists for this thread.
                    Dispatcher unused = Dispatcher.CurrentDispatcher;

                    unused.UnhandledException += new DispatcherUnhandledExceptionEventHandler(OnUnhandledException);

                    resetEvent.Set();
                    try
                    {
                        Dispatcher.Run();
                    }
                    catch (ThreadAbortException)
                    {
                    }
                });

                _thread.Name = GetType().FullName;
                _thread.IsBackground = true;
                _thread.SetApartmentState(ApartmentState.STA);
                _thread.Start();

                resetEvent.WaitOne();

                AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            }

            _dispatcher = Dispatcher.FromThread(_thread);
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            // Need to dispose of the dispatch thread prior to the app domain going away.
            Close();
        }

        internal Thread Thread
        {
            get { return _thread; }
        }

        private SynchronizationContext syncContext;
        internal SynchronizationContext SyncContext
        {
            get
            {
                if (syncContext == null)
                {
                    syncContext = new DispatcherSynchronizationContext(_dispatcher);
                }
                return syncContext;
            }
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // We don't deal with unhandled exceptions from BeginInvoke,
            // since there's no one to throw them to.
            if (_isInvoking)
            {
                // e.Exception should be a TargetInvocationException from calling Invoke,
                // the InnerExceptionis the one to forward on
                if (e.Exception is TargetInvocationException)
                {
                    _invokeException = e.Exception.InnerException;
                }
                else
                {
                    _invokeException = e.Exception;
                }

                e.Handled = true;
            }
        }

        internal void Invoke(Action action)
        {
            if (_isClosed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            lock (_invokeSyncRoot)
            {
                _isInvoking = true;
                _invokeException = null;

                try
                {
                    _dispatcher.Invoke(DispatcherPriority.Normal, action);

                    if (_invokeException != null)
                    {
                        throw _invokeException;
                    }
                }
                finally
                {
                    _isInvoking = false;
                }
            }
        }

        internal DispatcherOperation BeginInvoke(Action action)
        {
            if (_isClosed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return _dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        internal void Close()
        {
            if (!_isClosed)
            {
                _dispatcher.InvokeShutdown();
                _dispatcher = null;
                try
                {
                    _thread.Abort();
                }
                catch (ThreadAbortException)
                {
                }
            }

            _isClosed = true;
        }
    }
}
