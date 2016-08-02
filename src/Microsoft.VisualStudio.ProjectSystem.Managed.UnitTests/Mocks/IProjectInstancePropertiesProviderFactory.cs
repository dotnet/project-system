using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class IProjectInstancePropertiesProviderFactory
    {
        public static IProjectInstancePropertiesProvider Create()
            => Mock.Of<IProjectInstancePropertiesProvider>();
    }
}
