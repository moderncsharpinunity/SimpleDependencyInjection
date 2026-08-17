using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// Thrown by <see cref="ServiceProvider.GetService"/> when a type has no registration
    /// in this provider or any parent (Feature 8, 0.3.0). Derives from
    /// <see cref="ArgumentException"/> — same as the message the container has always
    /// thrown — so existing <c>Throws.ArgumentException</c> assertions keep passing; the
    /// difference is this one carries enough to actually fix the problem instead of just
    /// naming the missing type.
    /// </summary>
    public sealed class ServiceNotRegisteredException : ArgumentException
    {
        public Type ServiceType { get; }

        /// <summary>This provider's name, then its parent's, then its parent's parent's…</summary>
        public IReadOnlyList<string> ScopeChain { get; }

        /// <summary>What IS registered directly in the innermost scope — the one that
        /// actually failed to resolve — so the fix (usually "register it here, or one
        /// level up") is visible without opening a debugger.</summary>
        public IReadOnlyList<string> RegisteredInThisScope { get; }

        public ServiceNotRegisteredException(Type serviceType, IReadOnlyList<string> scopeChain,
            IReadOnlyList<string> registeredInThisScope)
            : base(BuildMessage(serviceType, scopeChain, registeredInThisScope))
        {
            ServiceType = serviceType;
            ScopeChain = scopeChain;
            RegisteredInThisScope = registeredInThisScope;
        }

        static string BuildMessage(Type serviceType, IReadOnlyList<string> scopeChain,
            IReadOnlyList<string> registeredInThisScope)
        {
            string chain = scopeChain != null && scopeChain.Count > 0
                ? string.Join(" → ", scopeChain) : "(unnamed scope)";
            string registered = registeredInThisScope != null && registeredInThisScope.Count > 0
                ? string.Join(", ", registeredInThisScope.OrderBy(n => n)) : "(nothing)";

            return $"Type is not a dependency: {serviceType?.FullName}\n" +
                   $"Service '{serviceType?.FullName}' is not registered in this scope or any parent.\n" +
                   $"  Scope chain: {chain}\n" +
                   $"  Registered in {(scopeChain != null && scopeChain.Count > 0 ? scopeChain[0] : "this scope")}: {registered}";
        }
    }
}
