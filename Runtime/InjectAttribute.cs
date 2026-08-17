using System;
using UnityEngine.Scripting;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// Marks a field, constructor or method for the container to resolve.
    ///
    /// Derives from <see cref="PreserveAttribute"/> so IL2CPP's managed stripping leaves
    /// every injected member intact under a release build's stripping level — without
    /// this, method and constructor injection can silently disappear from a stripped
    /// build while every editor test still passes (Fix 3, 0.3.0).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false)]
    public class InjectAttribute : PreserveAttribute
    {
    }
}
