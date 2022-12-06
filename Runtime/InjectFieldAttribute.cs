using System;

namespace SimpleDependencyInjection
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectFieldAttribute : Attribute
    {
    }
}
