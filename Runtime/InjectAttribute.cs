using System;

namespace SimpleDependencyInjection
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false)]
    public class InjectAttribute : Attribute
    {
    }
}
