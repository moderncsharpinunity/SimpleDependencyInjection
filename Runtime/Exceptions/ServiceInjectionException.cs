using System;
using UnityEngine;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// Wraps a <see cref="ServiceNotRegisteredException"/> with exactly which object,
    /// which field/method/constructor, and which declaring type failed to resolve
    /// (Feature 8, 0.3.0) — under field injection, a bare "type not registered" leaves
    /// you to search every <c>[Inject]</c> field by hand; this names the one that broke.
    /// </summary>
    public sealed class ServiceInjectionException : Exception
    {
        public Type DependantType { get; }

        /// <summary>Where the failing member is declared — may be a base class of
        /// <see cref="DependantType"/>.</summary>
        public Type DeclaringType { get; }

        /// <summary>A field name, or "MethodName(parameter N: 'name')" for a method or
        /// constructor parameter.</summary>
        public string MemberName { get; }

        public Type ServiceType { get; }

        public ServiceInjectionException(object dependant, Type declaringType, string memberName,
            Type serviceType, ServiceNotRegisteredException inner)
            : base(BuildMessage(dependant, declaringType, memberName, inner), inner)
        {
            DependantType = dependant?.GetType();
            DeclaringType = declaringType;
            MemberName = memberName;
            ServiceType = serviceType;
        }

        static string BuildMessage(object dependant, Type declaringType, string memberName,
            ServiceNotRegisteredException inner)
        {
            string context = dependant is Component component
                ? $" (GameObject '{component.gameObject.name}')"
                : dependant is UnityEngine.Object unityObject ? $" ('{unityObject.name}')" : "";

            return $"Cannot inject '{memberName}' on {dependant?.GetType().FullName}{context}, " +
                   $"declared in {declaringType?.FullName}.\n{inner.Message}";
        }
    }
}
