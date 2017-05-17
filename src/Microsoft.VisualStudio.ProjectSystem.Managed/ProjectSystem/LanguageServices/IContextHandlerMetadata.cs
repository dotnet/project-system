// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices
{
    public interface IContextHandlerMetadata : IOrderPrecedenceMetadataView
    {
        /// <summary>
        ///     Gets the evaluation rule this handler handles.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> containing the evaluation rule that this <see cref="IEvaluationHandler"/> 
        ///     handles.
        /// </value>
        [DefaultValue(null)]
        string EvaluationRuleName
        {
            get;
        }
    }
}