using System;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// Thrown when something needs a scope's <see cref="System.IServiceProvider"/> before
    /// that scope has finished building — most commonly a <see cref="GameObjectServiceScope"/>
    /// whose scene loaded before <see cref="AppServiceScope.Current"/> was set (Feature 8,
    /// 0.3.0). The fix is always a boot-order one: nothing should reference a scope before
    /// it's ready, and nothing should need to if the app's entry point genuinely finishes
    /// building before anything else runs.
    /// </summary>
    public sealed class ServiceScopeNotReadyException : Exception
    {
        public object Subject { get; }

        public ServiceScopeNotReadyException(object subject, string message) : base(message)
        {
            Subject = subject;
        }
    }
}
