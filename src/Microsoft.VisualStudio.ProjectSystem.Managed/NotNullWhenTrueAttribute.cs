namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter,
                   AllowMultiple = false)]
    internal class NotNullWhenTrueAttribute : Attribute
    {
        public NotNullWhenTrueAttribute() { }
    }
}
