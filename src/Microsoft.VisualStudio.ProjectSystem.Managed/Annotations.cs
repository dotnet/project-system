// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class AssertsTrueAttribute : Attribute
    {
        public AssertsTrueAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class AssertsFalseAttribute : Attribute
    {
        public AssertsFalseAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class EnsuresNotNullAttribute : Attribute
    {
        public EnsuresNotNullAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class NotNullWhenFalseAttribute : Attribute
    {
        public NotNullWhenFalseAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class NotNullWhenTrueAttribute : Attribute
    {
        public NotNullWhenTrueAttribute() { }
    }
}
