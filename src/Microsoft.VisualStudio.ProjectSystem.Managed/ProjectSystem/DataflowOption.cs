// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks.Dataflow;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides properties and methods containing common dataflow link and block options.
    /// </summary>
    internal static class DataflowOption
    {
        /// <summary>
        ///     Returns a new instance of <see cref="DataflowLinkOptions"/> with 
        ///     <see cref="DataflowLinkOptions.PropagateCompletion"/> set to <see langword="true"/>.
        /// </summary>
        public static DataflowLinkOptions PropagateCompletion
        {
            get
            {
                // DataflowLinkOptions is mutable, make sure always create
                // a new copy to avoid accidentally currupting state
                return new DataflowLinkOptions()
                {
                    PropagateCompletion = true  //  // Make sure source block completion and faults flow onto the target block.
                };
            }
        }
    }
}
