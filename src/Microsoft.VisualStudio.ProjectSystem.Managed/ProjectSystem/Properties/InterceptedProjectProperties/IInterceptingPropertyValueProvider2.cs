// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// An extension to <see cref="IInterceptingPropertyValueProvider"/> that allows value providers to share which project file
    /// properties they are writing to.
    /// </summary>
    public interface IInterceptingPropertyValueProvider2 : IInterceptingPropertyValueProvider
    {
        /// <summary>
        /// Obtain the MSBuild properties to which this <see cref="IInterceptingPropertyValueProvider"/> is writing to, given the parameters.
        /// </summary>
        Task<bool> IsValueDefinedInContextAsync(string propertyName, IProjectProperties defaultProperties);
    }
}
