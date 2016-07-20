// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    /// Static component that helps to keep track of all open UnconfiguredProject instances
    /// and allows getting UnconfiguredProject instance for given project file path (this is 
    /// useful when we need to get UnconfiguredProject without going to UI thread).
    /// </summary>
    internal class ActiveProjectTracker
    {
        // Can't crate one
        private ActiveProjectTracker() { }

        // Our single public instnce
        public static readonly ActiveProjectTracker Instance = new ActiveProjectTracker();

        // This hashset tracks all the projects that we create.
        private HashSet<UnconfiguredProject> ActiveProjects = new HashSet<UnconfiguredProject>();

        /// <summary>
        /// Adds the project to the list of tracked DNX projects
        /// </summary>
        public void Add(UnconfiguredProject project)
        {
            lock (ActiveProjects)
            {
                ActiveProjects.Add(project);
            }
        }

        /// <summary>
        /// Returns true if the project exists on the list
        /// </summary>
        public bool Contains(UnconfiguredProject project)
        {
            lock (ActiveProjects)
            {
                return ActiveProjects.Contains(project);
            }
        }

        /// <summary>
        /// Returns true if the project exists on the list
        /// </summary>
        public bool Contains(string projectFullPath)
        {
            lock (ActiveProjects)
            {
                return ActiveProjects.Any(x => x.FullPath.Equals(projectFullPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Removes the project and returns the number of projcts still on the list
        /// </summary>
        public int RemoveReturnCount(UnconfiguredProject project)
        {
            lock (ActiveProjects)
            {
                ActiveProjects.Remove(project);
                return ActiveProjects.Count;
            }
        }

        /// <summary>
        /// Returns the count of projects
        /// </summary>
        public int Count
        {
            get
            {
                return ActiveProjects.Count;
            }
        }

        /// <summary>
        /// Returns the project which represents the projectFile or null if there isn't one.
        /// </summary>
        public UnconfiguredProject GetProjectOfPath(string projectFile)
        {
            lock (ActiveProjects)
            {
                return ActiveProjects.FirstOrDefault(p => { return p.FullPath.Equals(projectFile, StringComparison.OrdinalIgnoreCase); });
            }
        }

        public List<UnconfiguredProject> GetAllProjects()
        {
            lock (ActiveProjects)
            {
                // Create a copy for thread safety.
                return ActiveProjects.ToList();
            }
        }

        /// <summary>
        /// Only used by unit tests to flush the static list. It should never be called in production code
        /// </summary>
        public void Clear()
        {
            ActiveProjects.Clear();
        }
    }
}
