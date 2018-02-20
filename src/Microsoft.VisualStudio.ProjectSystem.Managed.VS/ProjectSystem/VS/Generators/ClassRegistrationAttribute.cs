// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Generators
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class ClassRegistrationAttribute : RegistrationAttribute
    {
        private readonly string _clsId;
        private readonly string _classInfo;

        public ClassRegistrationAttribute(string clsId, string classInfo)
        {
            Requires.NotNull(clsId, nameof(clsId));
            Requires.NotNull(classInfo, nameof(classInfo));

            _clsId = clsId;
            _classInfo = classInfo;
        }

        public override void Register(RegistrationContext context)
        {
            var _classType = Type.GetType(_classInfo);
            using (Key childKey = context.CreateKey($"CLSID\\{_clsId}"))
            {

                childKey.SetValue("Assembly", _classType.Assembly.FullName);
                childKey.SetValue("Class", _classType.FullName);
                childKey.SetValue("InprocServer32", "$System$\\mscoree.dll");
                childKey.SetValue("ThreadingModel", "Both");
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(_clsId);
        }
    }
}
