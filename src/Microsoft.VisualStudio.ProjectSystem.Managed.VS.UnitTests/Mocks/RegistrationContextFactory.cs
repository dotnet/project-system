using System;

using Moq;

using static Microsoft.VisualStudio.Shell.RegistrationAttribute;

namespace Microsoft.VisualStudio.Shell
{
    internal class RegistrationContextFactory
    {
        public static RegistrationContext CreateInstance(Action<string> createKeyAction, Action<string, object> setValueAction)
        {
            var moq = new Mock<RegistrationContext>();

            moq.Setup(rc => rc.CreateKey(It.IsAny<string>())).Callback(createKeyAction).Returns(CreateKeyInstance(setValueAction));            

            return moq.Object;
        }

        private static Key CreateKeyInstance(Action<string, object> setValueAction)
        {
            var moq = new Mock<Key>();

            moq.Setup(k => k.SetValue(It.IsAny<string>(), It.IsAny<object>())).Callback(setValueAction);

            return moq.Object;
        }
    }
}
