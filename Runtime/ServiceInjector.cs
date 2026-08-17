using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public class ServiceInjector
    {
        /// <summary>Everything one class level in a hierarchy contributes to injection —
        /// its own declared <c>[Inject]</c> fields, and (for MonoBehaviours only) its own
        /// declared <c>[Inject]</c> method. Fields and methods share this one traversal
        /// (0.3.0, Fix 1) so a method can rely on that same level's fields already being
        /// set, exactly like a field initialiser runs before a constructor body.</summary>
        readonly struct InjectionLevel
        {
            public InjectionLevel(FieldInfo[] fields, MethodBase method)
            {
                Fields = fields;
                Method = method;
            }

            public FieldInfo[] Fields { get; }

            /// <summary>Null if this level declares no <c>[Inject]</c> method.</summary>
            public MethodBase Method { get; }
        }

        struct TypeCache
        {
            public TypeCache(InjectionLevel[] levels, MethodBase constructor)
            {
                Levels = levels;
                Constructor = constructor;
            }

            /// <summary>Base-first: index 0 is the topmost base class carrying
            /// <c>[Inject]</c> members, the last entry is the concrete type. Walked in
            /// this order so a base level's fields and method are fully resolved before a
            /// derived level's run — the same guarantee a chain of <c>base(...)</c>
            /// constructor calls gives (0.3.0, Fix 1).</summary>
            public InjectionLevel[] Levels { get; }

            /// <summary>Plain classes only. Invoked once, after every level's fields have
            /// been injected, so field-injected dependencies are already set inside the
            /// constructor body (this is why <see cref="ServiceFactory.FromNew{T}"/>
            /// injects fields via <see cref="FormatterServices.GetUninitializedObject"/>
            /// before calling this).</summary>
            public MethodBase Constructor { get; }
        }

        static readonly Dictionary<Type, TypeCache> typesCache = new Dictionary<Type, TypeCache>();
        static readonly Type unityObjectType = typeof(UnityEngine.Object);

        /// <summary>
        /// Injects <paramref name="monoBehaviour"/> and every MonoBehaviour under it.
        ///
        /// A <see cref="GameObjectServiceScope"/> found along the way is never injected
        /// from here directly — it injects and builds itself
        /// (see <see cref="GameObjectServiceScope"/>'s own bootstrap), because that build
        /// may be genuinely asynchronous and this method is not. Once a scope has claimed
        /// itself (<see cref="GameObjectServiceScope.Injected"/>), this method leaves its
        /// entire subtree to that scope — including the moment its own build actually
        /// finishes — rather than risk injecting a child from a still-building (or
        /// already-injected) scope a second time (0.3.0, Fix 2).
        ///
        /// Failures are collected across the whole subtree and thrown together as one
        /// <see cref="AggregateException"/>, so a scene with several missing
        /// registrations costs one play-mode entry to diagnose, not one per failure
        /// (0.3.0, Feature 8).
        /// </summary>
        public static void InjectRecursively(MonoBehaviour monoBehaviour, IServiceProvider serviceProvider)
        {
            var serviceScopeType = typeof(GameObjectServiceScope);
            var children = monoBehaviour.GetComponentsInChildren<MonoBehaviour>(true);
            var failures = new List<Exception>();

            foreach (var child in children)
            {
                if (serviceScopeType.IsAssignableFrom(child.GetType())) continue;

                var serviceScope = (GameObjectServiceScope)child.GetComponentInParent(serviceScopeType, true);
                if (serviceScope != null)
                {
                    if (serviceScope.Injected) continue; // the scope owns its whole subtree once claimed
                    if (serviceScope.ServiceProvider == null)
                    {
                        failures.Add(new ServiceScopeNotReadyException(child,
                            $"'{child.GetType().FullName}' on GameObject '{child.gameObject.name}' is under " +
                            $"'{serviceScope.GetType().FullName}', which has not finished building yet."));
                        continue;
                    }
                    TryInject(child, serviceScope.ServiceProvider, failures);
                }
                else
                {
                    TryInject(child, serviceProvider, failures);
                }
            }

            if (failures.Count > 0)
                throw new AggregateException(
                    $"{failures.Count} injection failure(s) while injecting '{monoBehaviour.name}'.", failures);
        }

        public static void Inject(object dependant, IServiceProvider serviceProvider)
        {
            Type type = dependant.GetType();
            if (!typesCache.TryGetValue(type, out TypeCache cache))
            {
                cache = typesCache[type] = CalculateType(type);
            }

            foreach (var level in cache.Levels)
            {
                foreach (var field in level.Fields)
                {
                    InjectField(dependant, serviceProvider, field);
                }

                if (level.Method != null)
                {
                    InjectMethod(dependant, level.Method, serviceProvider);
                }
            }

            if (cache.Constructor != null)
            {
                InjectMethod(dependant, cache.Constructor, serviceProvider);
            }
        }

        /// <summary>Finds the provider that reaches <paramref name="scope"/> itself — its
        /// nearest ENCLOSING scope's provider (searched starting one level above
        /// <paramref name="scope"/>, so it never finds itself), or <paramref name="fallback"/>
        /// if there is none. Used to inject a scope's own <c>[Inject]</c> members, which
        /// belong to whatever contains it, not to the context it's about to build
        /// (0.3.0, Fix 2).</summary>
        internal static IServiceProvider ResolveEnclosingProvider(GameObjectServiceScope scope, IServiceProvider fallback)
        {
            if (scope.transform.parent == null) return fallback;
            var enclosing = scope.transform.parent.GetComponentInParent<GameObjectServiceScope>(true);
            return enclosing != null && enclosing.ServiceProvider != null ? enclosing.ServiceProvider : fallback;
        }

        static TypeCache CalculateType(Type type)
        {
            bool isUnityObject = unityObjectType.IsAssignableFrom(type);
            var levels = new List<InjectionLevel>();

            for (var t = type; t != null && t != unityObjectType && t != typeof(object); t = t.BaseType)
            {
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                    .Where(f => f.GetCustomAttribute<InjectAttribute>(false) != null)
                    .ToArray();

                MethodBase method = null;
                if (isUnityObject)
                {
                    var methods = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance)
                        .Where(m => m.GetCustomAttribute<InjectAttribute>(false) != null)
                        .ToArray();

                    if (methods.Length > 1)
                    {
                        throw new InvalidOperationException(
                            $"Type {t.FullName} has more than one injected method. There can only be one per class.");
                    }
                    if (methods.Length == 1)
                    {
                        if (methods[0].ReturnType != typeof(void))
                        {
                            throw new InvalidOperationException(
                                $"Type {t.FullName} has an invalid injected method. They can only return void.");
                        }
                        method = methods[0];
                    }
                }

                if (fields.Length > 0 || method != null)
                {
                    levels.Add(new InjectionLevel(fields, method));
                }
            }

            levels.Reverse(); // base-first

            MethodBase constructor = null;
            if (!isUnityObject)
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                if (constructors.Length > 1)
                {
                    constructors = constructors.Where(c => c.GetCustomAttribute<InjectAttribute>(false) != null).ToArray();

                    if (constructors.Length > 1)
                    {
                        throw new InvalidOperationException(
                            $"Type {type.FullName} has more than one constructor. There can only be one per class or if there are many, only one with the inject attribute.");
                    }
                }

                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Type {type.FullName} has no public constructor for the container to inject. A class " +
                        "resolved with FromSame()/FromNew<T>() needs one — an otherwise field-only-injected " +
                        "class still needs an empty public constructor.");
                }

                constructor = constructors[0];
            }

            return new TypeCache(levels.ToArray(), constructor);
        }

        static void InjectField(object dependant, IServiceProvider serviceProvider, FieldInfo field)
        {
            try
            {
                field.SetValue(dependant, serviceProvider.GetService(field.FieldType));
            }
            catch (ServiceNotRegisteredException ex)
            {
                throw new ServiceInjectionException(dependant, field.DeclaringType, field.Name, field.FieldType, ex);
            }
        }

        static void InjectMethod(object dependant, MethodBase method, IServiceProvider serviceProvider)
        {
            var parameters = method.GetParameters();
            var values = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                try
                {
                    values[i] = serviceProvider.GetService(parameters[i].ParameterType);
                }
                catch (ServiceNotRegisteredException ex)
                {
                    string memberName = $"{method.Name}(parameter {i}: '{parameters[i].Name}')";
                    throw new ServiceInjectionException(dependant, method.DeclaringType, memberName,
                        parameters[i].ParameterType, ex);
                }
            }
            method.Invoke(dependant, values);
        }

        static void TryInject(object dependant, IServiceProvider serviceProvider, List<Exception> failures)
        {
            try
            {
                Inject(dependant, serviceProvider);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }
}
