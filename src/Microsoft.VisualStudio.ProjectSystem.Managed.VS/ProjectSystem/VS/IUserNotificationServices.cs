// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IUserNotificationServices
    {
        /// <summary>
        ///     Shows a Yes/No confirmation box to the user.
        /// </summary>
        /// <returns>
        ///     <see langword="true"/> if the user clicked the Yes button, otherwise; 
        ///     <see langword="false"/> if the user clicked the No button.
        /// </returns>
        ///<exception cref="COMException">
        ///     This method was not accessed from the UI thread.
        /// </exception>
        bool Confirm(string message);

        /// <summary>
        ///     Shows a warning to the user.
        /// </summary>
        ///<exception cref="COMException">
        ///     This method was not accessed from the UI thread.
        /// </exception>
        void ShowWarning(string warning);

        /// <summary>
        ///     Shows a error to the user.
        /// </summary>
        ///<exception cref="COMException">
        ///     This method was not accessed from the UI thread.
        /// </exception>
        void ShowError(string error);
    }
}
