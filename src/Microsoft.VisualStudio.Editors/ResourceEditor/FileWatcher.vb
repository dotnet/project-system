' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports System.IO
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common

'CONSIDER: Only watch files in the current category.  Currently I'm watching all files, regardless
'CONSIDER: of whether the user is showing that category or not.
Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Watches for changes on files, and notifies any listeners when those
    '''   changes happen.
    ''' </summary>
    Friend NotInheritable Class FileWatcher
        Implements IDisposable

        'An interface for anyone interested in listening to file watcher notifications.
        Public Interface IFileWatcherListener
            Sub OnFileChanged(FileNameAndPath As String)
        End Interface

        ''' <summary>
        ''' A list of DirectoryWatchers for each directory of interest.  We have exactly
        '''   one DirectoryWatcher for each directory that contains a file we're watching.
        '''   This is the most efficient manner of using the FileSystemWatcher, since it
        '''   is based on directories, and allocates system resources per directory.
        ''' </summary>
        Private ReadOnly _directoryWatchers As New Dictionary(Of String, DirectoryWatcher)(StringComparers.Paths)

        'Any System.Windows.Forms control on the primary thread.  This is used for invoking 
        '  the system filewatch events (called on a secondary thread) back to the main thread.
        Private ReadOnly _controlForSynchronizingThreads As Control

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ControlForSynchronizingThreads">Any System.Windows.Forms control on the primary thread.  This is used for invoking the system filewatch events (called on a secondary thread) back to the main thread.</param>
        Public Sub New(ControlForSynchronizingThreads As Control)
            Debug.Assert(ControlForSynchronizingThreads IsNot Nothing)
            _controlForSynchronizingThreads = ControlForSynchronizingThreads
        End Sub

        ''' <summary>
        ''' Dispose
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            For Each Watcher In _directoryWatchers.Values
                Watcher.Dispose()
            Next

            _directoryWatchers.Clear()
        End Sub

        ''' <summary>
        ''' Returns the number of directories being watched.
        ''' </summary>
        Public ReadOnly Property DirectoryWatchersCount As Integer
            Get
                Return _directoryWatchers.Count
            End Get
        End Property

        ''' <summary>
        ''' Start watching a file.  The listener will get notified when it changes.
        ''' </summary>
        ''' <param name="PathAndFileName">File to watch.  Must be an absolute path.</param>
        ''' <param name="Listener"></param>
        ''' <remarks>It's okay to call WatchFile repeatedly on the same file/listener pair.</remarks>
        Public Sub WatchFile(PathAndFileName As String, Listener As IFileWatcherListener)
            Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: Watch " & PathAndFileName)

            If Not Path.IsPathRooted(PathAndFileName) Then
                Debug.Fail("FileWatcher.WatchFile() does not accept relative paths")
                Exit Sub
            End If

            'Break up the path into directory and file.
            Dim DirectoryPath As String = Path.GetDirectoryName(PathAndFileName)
            Dim FileName As String = Path.GetFileName(PathAndFileName)
            Debug.Assert(GetFullPathTolerant(Path.Combine(DirectoryPath, FileName)) = GetFullPathTolerant(PathAndFileName))

            'Look for a (or create a new) DirectoryWatcher for this file's directory.
            Dim DirectoryWatcher As DirectoryWatcher = GetWatcherForDirectory(DirectoryPath, CreateIfNotFound:=True)
            Debug.Assert(DirectoryWatcher IsNot Nothing, "This shouldn't happen")

            'Add the listener.
            DirectoryWatcher.AddFileListener(FileName, Listener)
        End Sub

        ''' <summary>
        ''' Stops watching a particular file.
        ''' </summary>
        ''' <param name="PathAndFileName">The full path and file to stop watching.</param>
        ''' <param name="Listener">The listener that wants to stop watching.</param>
        ''' <remarks>
        ''' It's okay to remove a listener that's not currently listening.  That's just a NOOP.
        ''' </remarks>
        Public Sub UnwatchFile(PathAndFileName As String, Listener As IFileWatcherListener)
            Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: Unwatch " & PathAndFileName)

            'Break up the path into directory and file.
            Dim DirectoryPath As String = Path.GetDirectoryName(PathAndFileName)
            Dim FileName As String = Path.GetFileName(PathAndFileName)
            Debug.Assert(GetFullPathTolerant(Path.Combine(DirectoryPath, FileName)) = GetFullPathTolerant(PathAndFileName))

            'Look for a (or create a new) DirectoryWatcher for this file's directory.
            Dim DirectoryWatcher As DirectoryWatcher = GetWatcherForDirectory(DirectoryPath, CreateIfNotFound:=False)
            If DirectoryWatcher Is Nothing Then
                Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: Couldn't find directorywatcher for listener on Unwatch - Ignoring")
                Exit Sub
            End If

            'Remove the listener
            DirectoryWatcher.RemoveListener(FileName, Listener)
            If DirectoryWatcher.FileCount = 0 Then
                'This directory watcher is not watching anything anymore.  We can remove it.
                _directoryWatchers.Remove(NormalizeDirectoryPath(DirectoryPath))
                Debug.Assert(Not _directoryWatchers.ContainsValue(DirectoryWatcher), "Wasn't able to remove directory watcher for " & DirectoryWatcher.DirectoryPath)
                DirectoryWatcher.Dispose()
            End If
        End Sub

        ''' <summary>
        ''' Gets a DirectoryWatcher for this path.  If one doesn't already exist, a new one
        '''   will be created.
        ''' </summary>
        ''' <param name="DirectoryPath">Directory path to watch.</param>
        ''' <param name="CreateIfNotFound">If True, will create a new DirectoryWatcher if none is found matching the file.</param>
        ''' CONSIDER: be tolerant of 8.3 paths
        Private Function GetWatcherForDirectory(DirectoryPath As String, CreateIfNotFound As Boolean) As DirectoryWatcher
            DirectoryPath = NormalizeDirectoryPath(DirectoryPath)

            Dim Watcher As DirectoryWatcher = Nothing
            If Not _directoryWatchers.TryGetValue(DirectoryPath, Watcher) Then
                'None found.  Create a new one if requested
                If CreateIfNotFound Then
                    Watcher = New DirectoryWatcher(DirectoryPath, Me)
                    _directoryWatchers(DirectoryPath) = Watcher
                End If
            End If

            Return Watcher
        End Function

        ''' <summary>
        ''' Normalizes a directory path so we can use it as a key.
        ''' </summary>
        ''' <param name="DirectoryPath">The directory path to normalize</param>
        Private Shared Function NormalizeDirectoryPath(DirectoryPath As String) As String
            Debug.Assert(DirectoryPath <> "")
            DirectoryPath = GetFullPathTolerant(DirectoryPath)
            Debug.Assert(DirectoryPath.IndexOf(Path.AltDirectorySeparatorChar) = -1, "Normalized path shouldn't contain alternate directory separators - these should have been removed by the Path methods")

            Return DirectoryPath
        End Function

        ''' <summary>
        ''' Given a FileWatcherEntry, invoke its OnFileChanged event in such a way that the
        '''   call is marshalled to the thread on which m_ControlForSynchronizingThreads exists.
        ''' </summary>
        ''' <param name="Entry">The file watcher entry to invoke.</param>
        Private Sub MarshallFileChangedEvent(Entry As FileWatcherEntry)
            _controlForSynchronizingThreads.BeginInvoke(New MethodInvoker(AddressOf Entry.OnFileChanged))
        End Sub

#Region "Private class - DirectoryWatcher"

        ''' <summary>
        ''' A private class that watches a single directory and manages a list of single
        '''   file watchers (each with multiple listeners).
        ''' </summary>
        Private NotInheritable Class DirectoryWatcher
            Implements IDisposable

            'The path that we're watching
            Private ReadOnly _directoryPath As String

            'A list of FileWatcherEntry's, one for each file in this directory being watched.
            Private _fileWatcherEntries As New Dictionary(Of String, FileWatcherEntry)(StringComparers.Paths)

            'A system FileSystemWatcher class.  This is the actual class that does the watching
            '  on our directory.  It's most efficient to use it on a single directory, since 
            '  each of these takes up system resources.  Therefore we have one per directory
            '  of each file watched.
            Private WithEvents _fileSystemWatcher As FileSystemWatcher

            'The parent FileWatcher class
            Private ReadOnly _parentFileWatcher As FileWatcher

            Public Sub New(DirectoryPath As String, ParentFileWatcher As FileWatcher)
                Debug.Assert(ParentFileWatcher IsNot Nothing)
                _parentFileWatcher = ParentFileWatcher
                Debug.Assert(DirectoryPath <> "")

                _directoryPath = DirectoryPath

                Try
                    _fileSystemWatcher = New FileSystemWatcher(DirectoryPath)
                Catch ex As System.Threading.ThreadAbortException
                    Throw
                Catch ex As StackOverflowException
                    Throw
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(FileWatcher.New), NameOf(FileWatcher))
                    'CONSIDER: The directory does not exist.  Find a parent directory that
                    '  does exist.  What if drive doesn't exist?
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: Unable to create FileSystemWatcher: directory does not exist: " & DirectoryPath)
                    Exit Sub
                End Try

                'Although taking advantage of this capability would be useful, it would be
                '  difficult to do in an efficient manner (what if someone has a resource in
                '  the root directory, for example - we would see so many events, we might
                '  even overflow the buffer the FileSystemWatcher allocates).  We'll have to
                '  stick to a single directory at a time.
                _fileSystemWatcher.IncludeSubdirectories = False

                'Watch only for files that have been changed
                _fileSystemWatcher.NotifyFilter = NotifyFilters.FileName Or NotifyFilters.LastWrite
            End Sub

            ''' <summary>
            ''' Dispose
            ''' </summary>
            Public Sub Dispose() Implements IDisposable.Dispose
                If _fileSystemWatcher IsNot Nothing Then
                    _fileSystemWatcher.Dispose()
                    _fileSystemWatcher = Nothing
                End If
                _fileWatcherEntries = Nothing
            End Sub

            ''' <summary>
            ''' The directory path being watched.
            ''' </summary>
            Public ReadOnly Property DirectoryPath As String
                Get
                    Return _directoryPath
                End Get
            End Property

            ''' <summary>
            ''' Returns the number of files being watched in this directory
            ''' </summary>
            Public ReadOnly Property FileCount As Integer
                Get
                    Return _fileWatcherEntries.Count
                End Get
            End Property

            ''' <summary>
            ''' Adds an entry to watch a single file within this directory.
            ''' </summary>
            ''' <param name="FileNameOnly">The FileName (no path) to watch</param>
            ''' <param name="Listener">The listener to add for this file</param>
            Public Sub AddFileListener(FileNameOnly As String, Listener As IFileWatcherListener)
                FileNameOnly = NormalizeFileName(FileNameOnly)

                Dim Entry As FileWatcherEntry = Nothing
                If Not _fileWatcherEntries.TryGetValue(FileNameOnly, Entry) Then
                    'We don't have an entry for this file yet.  Add one now.
                    Entry = New FileWatcherEntry(Me, FileNameOnly)
                    _fileWatcherEntries(FileNameOnly) = Entry
                End If

                'Add this listener to the entry
                Entry.AddListener(Listener)

                If _fileSystemWatcher IsNot Nothing Then
                    _fileSystemWatcher.EnableRaisingEvents = True
                End If
            End Sub

            ''' <summary>
            ''' Removes a listener for a particular file in this directory.
            ''' </summary>
            ''' <param name="FileNameOnly">File name (no path) of the file that was being listened</param>
            ''' <param name="Listener">The listener to remove for this file.</param>
            ''' <remarks>
            ''' It's okay to remove a listener that's not listening.  This is just a NOOP.
            ''' </remarks>
            Public Sub RemoveListener(FileNameOnly As String, Listener As IFileWatcherListener)
                FileNameOnly = NormalizeFileName(FileNameOnly)

                Dim Entry As FileWatcherEntry = Nothing

                If _fileWatcherEntries.TryGetValue(FileNameOnly, Entry) Then
                    Entry.RemoveListener(Listener)

                    If Entry.ListenerCount = 0 Then
                        'No more listeners for this entry - we can remove it now.
                        _fileWatcherEntries.Remove(FileNameOnly)
                    End If
                Else
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: Couldn't find filewatcherentry for listener on Unwatch - Ignoring")
                End If

                If _fileWatcherEntries IsNot Nothing AndAlso _fileWatcherEntries.Count = 0 _
                        AndAlso _fileSystemWatcher IsNot Nothing Then
                    'There are no more entries - we can disable the file system watcher, which allows
                    '  it to remove its system resources
                    _fileSystemWatcher.EnableRaisingEvents = False
                End If
            End Sub

            ''' <summary>
            ''' Normalizes a filename so we can make comparisons between files
            ''' </summary>
            ''' <param name="FileNameOnly">The file (no path) to normalize</param>
            Private Shared Function NormalizeFileName(FileNameOnly As String) As String
                Debug.Assert(Path.GetDirectoryName(FileNameOnly) = "" AndAlso Not Path.IsPathRooted(FileNameOnly),
                    "DirectoryWatcher does not accept paths with the filename - should be relative to the directory path in DirectoryWatcher")
                Debug.Assert(Path.GetFileName(FileNameOnly) = FileNameOnly)
                Return FileNameOnly
            End Function

            ''' <summary>
            ''' This is called when a file in this directory has changed in any way that
            '''   interests us.
            ''' </summary>
            ''' <param name="FullDirectoryPath">Full path of the directory where the file was (should be our directory)</param>
            ''' <param name="FileName">The file name (no path) of the file in this directory that has changed.</param>
            Private Sub OnFileChanged(FullDirectoryPath As String, FileName As String)
                Debug.Assert(Not String.IsNullOrEmpty(FileName))
                Debug.Assert(String.Equals(Path.GetDirectoryName(FullDirectoryPath), _directoryPath, StringComparison.OrdinalIgnoreCase))
                Debug.Assert(Path.IsPathRooted(FullDirectoryPath))

                Dim FileNameThatChanged As String = NormalizeFileName(FileName)

                'Is this file one that we're interested in?
                Dim EntryThatChanged As FileWatcherEntry = Nothing
                If _fileWatcherEntries.TryGetValue(FileNameThatChanged, EntryThatChanged) Then
                    '... Yes.
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "    Matched entry: " & EntryThatChanged.ToString)

                    'Let the parent file watcher use its synchronization control to
                    '  synchronize a notification to our filewatcher entry to the
                    '  main thread.  The filewatcher entry will take things from there.
                    _parentFileWatcher.MarshallFileChangedEvent(EntryThatChanged)
                Else
                    'The file that changed isn't one we're interested in.
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "    No match.")
                End If
            End Sub

            ''' <summary>
            ''' Called by the FileSystemWatcher for this directory when a file has been changed.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub FileSystemWatcher_Changed(sender As Object, e As FileSystemEventArgs) Handles _fileSystemWatcher.Changed
                Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "DirectoryWatcher: Raw changed event: " & e.ChangeType & ", " & e.FullPath & ": " & e.Name & ", Thread = " & Hex(System.Threading.Thread.CurrentThread.GetHashCode))
                OnFileChanged(e.FullPath, e.Name)
            End Sub

            ''' <summary>
            ''' Called by the FileSystemWatcher for this directory when a file has been renamed.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub FileSystemWatcher_Renamed(sender As Object, e As RenamedEventArgs) Handles _fileSystemWatcher.Renamed
                Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "DirectoryWatcher: Raw renamed event: " & e.ChangeType & ", " & e.FullPath & ": " & e.Name & ", Thread = " & Hex(System.Threading.Thread.CurrentThread.GetHashCode))

                ' Both the old file and the new file might be interesting events.

                ' ReadDirectoryChangesW/FileSystemWatcher.Rename can on occasion send on a rename with or without the new name or the old name, 
                ' so make sure we handle this situation. See the comments in FileSystemWatcher.CompletionStatusChanged for more information.

                If Not String.IsNullOrEmpty(e.Name) Then
                    OnFileChanged(e.FullPath, e.Name)
                End If

                If Not String.IsNullOrEmpty(e.OldName) Then
                    OnFileChanged(e.FullPath, e.OldName)
                End If

            End Sub

            ''' <summary>
            ''' Called by the FileSystemWatcher for this directory when a file in it has been created.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub FileSystemWatcher_Created(sender As Object, e As FileSystemEventArgs) Handles _fileSystemWatcher.Created
                OnFileChanged(e.FullPath, e.Name)
            End Sub

            ''' <summary>
            ''' Called by the FileSystemWatcher for this directory when a file has been deleted.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            Private Sub FileSystemWatcher_Deleted(sender As Object, e As FileSystemEventArgs) Handles _fileSystemWatcher.Deleted
                OnFileChanged(e.FullPath, e.Name)
            End Sub
        End Class

#End Region

#Region "Private class - FileWatcherEntry"

        ''' <summary>
        ''' Private class that represents a single file being watched.  It can
        '''   handle multiple listeners.
        ''' </summary>
        Private NotInheritable Class FileWatcherEntry
            Implements IDisposable

            'The file name (no path) of the file being watched.
            Private ReadOnly _fileNameOnly As String

            'The parent DirectoryWatcher for this file.
            Private ReadOnly _directoryWatcher As DirectoryWatcher

            'A System.Windows.Forms timer used to delay processing of the file changed
            '  event until it's likely that no more changes to the file are
            '  imminent.
            Private WithEvents _timer As Timer

            'The delay in milliseconds that we wait before fire the file changed
            '  notifications, so we receive a single notification rather than 
            '  several (while the file's being written, etc.).  These often come
            '  in multiples.
            Private Const DelayInMilliseconds As Integer = 400

            Private ReadOnly _listeners As New HashSet(Of IFileWatcherListener)

#If DEBUG Then
            'Hashcode of the last thread we fired notifications on.  Used to verify
            '  we're properly notifying only on a single thread.
            Private _lastThreadHashCode As Integer
#End If

            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <param name="Directory">The DirectoryWatcher for the directory that this file is in.</param>
            ''' <param name="FileNameOnly">The filename (no path) of the file to watch.</param>
            Friend Sub New(Directory As DirectoryWatcher, FileNameOnly As String)
                Debug.Assert(Directory IsNot Nothing)
                Debug.Assert(FileNameOnly <> "")
                _fileNameOnly = FileNameOnly
                _directoryWatcher = Directory
            End Sub

            ''' <summary>
            ''' Dispose.
            ''' </summary>
            Public Sub Dispose() Implements IDisposable.Dispose
                If _timer IsNot Nothing Then
                    _timer.Dispose()
                    _timer = Nothing
                End If
                _listeners.Clear()
            End Sub

            ''' <summary>
            ''' Gets the path and filename of the file.
            ''' </summary>
            Public ReadOnly Property PathAndFileName As String
                Get
                    Return Path.Combine(_directoryWatcher.DirectoryPath, _fileNameOnly)
                End Get
            End Property

            ''' <summary>
            ''' Retrieves the current number of listeners.
            ''' </summary>
            Public ReadOnly Property ListenerCount As Integer
                Get
                    Return _listeners.Count
                End Get
            End Property

            ''' <summary>
            ''' Adds a new listener for this file.
            ''' </summary>
            ''' <param name="Listener">The listener to add.</param>
            Public Sub AddListener(Listener As IFileWatcherListener)
                If _listeners.Contains(Listener) Then
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: There's already an entry for this listener on this file - Ignoring")
                    Exit Sub
                End If

                _listeners.Add(Listener)
            End Sub

            ''' <summary>
            ''' Removes a listener.
            ''' </summary>
            ''' <param name="Listener"></param>
            ''' <remarks>
            ''' It's okay to remove a listener that's not listening.  That's just a NOOP.
            ''' </remarks>
            Public Sub RemoveListener(Listener As IFileWatcherListener)
                If _listeners.Contains(Listener) Then
                    _listeners.Remove(Listener)
                Else
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: Couldn't find entry for this listener on this file - Ignoring")
                End If
            End Sub

            ''' <summary>
            ''' ToString() override for debugging purposes.
            ''' </summary>
            Public Overrides Function ToString() As String
                Return "FileWatcherEntry: PathAndFileName = """ & PathAndFileName & """, " & ListenerCount & " listeners"
            End Function

            ''' <summary>
            ''' Called when the file referred to by this entry is changed.  This
            '''   method must be called on the synchronizing control's thread.
            ''' </summary>
            Friend Sub OnFileChanged()
                Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "    FileWatcherEntry: Received file change on main thread: " & ToString() & ", Thread = " & Hex(System.Threading.Thread.CurrentThread.GetHashCode) & ", Milliseconds = " & Now.Millisecond)

                'It is common to get several notifications about the same file while it's
                '  being written.  We want to let things settle down a bit before we
                '  notify our listeners of the change, so we can send them a single
                '  notification after it's likely the file's done being written.

                If _timer Is Nothing Then
                    'This is the first notification we've seen - create a timer to track the
                    '  delay.
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "        Enabling timer delay of " & DelayInMilliseconds & "ms.")
                    _timer = New Timer With {
                        .Interval = DelayInMilliseconds
                    }
                Else
                    'We already have a timer, so that means we're in the delay stage of
                    '  a previous notification of the same event.  We'll reset the timer
                    '  and keep waiting.
                    Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "        Currently active file change ignored.  Re-enabling timer delay of " & DelayInMilliseconds & "ms.")
                    _timer.Enabled = False
                End If

                _timer.Enabled = True
            End Sub

            ''' <summary>
            ''' Called by our timer when the delay time is over.  This means that we received
            '''   a notification that the file changed, and we haven't received any more for this
            '''   same file since the delay time.  So it's time to go ahead and notify all the
            '''   listeners.
            ''' </summary>
            ''' <param name="sender"></param>
            ''' <param name="e"></param>
            ''' <remarks>
            ''' Note that we're using a System.Windows.Forms timer, which always fires its events
            '''   in the thread from which it was created.  So no synchronization issues.
            ''' </remarks>
            Private Sub Timer_Elapsed(sender As Object, e As EventArgs) Handles _timer.Tick
                Debug.WriteLineIf(Switches.RSEFileWatcher.TraceVerbose, "    FileWatcherEntry: Raising delayed FileChanged event: " & ToString() & ", Thread = " & Hex(System.Threading.Thread.CurrentThread.GetHashCode) & ", Milliseconds = " & Now.Millisecond)

                'First thing to do is get rid of the timer - we don't need it anymore.
                _timer.Dispose()
                _timer = Nothing

#If DEBUG Then
                If _lastThreadHashCode <> 0 Then
                    Debug.Assert(_lastThreadHashCode = System.Threading.Thread.CurrentThread.GetHashCode, "FileWatcherEntry: We're not firing on the same thread every time")
                End If
                _lastThreadHashCode = System.Threading.Thread.CurrentThread.GetHashCode
#End If
                Debug.WriteLineIf(Switches.RSEFileWatcher.TraceInfo, "FileWatcher: Notifying listeners: """ & PathAndFileName & """, " & ToString())

                'Go ahead and notify all listeners
                For Each Listener As IFileWatcherListener In _listeners
                    If Listener IsNot Nothing Then
                        Listener.OnFileChanged(PathAndFileName)
                    Else
                        Debug.Fail("How'd that get in here?")
                    End If
                Next
            End Sub

        End Class

#End Region

    End Class

End Namespace
