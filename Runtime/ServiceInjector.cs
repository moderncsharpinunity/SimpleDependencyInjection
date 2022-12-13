using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public class ServiceInjector
    {
        private struct TypeCache
        {
            public TypeCache(Type type, IEnumerable<FieldInfo> fields, MethodBase constructor)
            {
                Type = type;
                Fields = fields;
                Constructor = constructor;
                Methods = null;
            }

            public TypeCache(Type type, IEnumerable<FieldInfo> fields, IEnumerable<MethodBase> methods)
            {
                Type = type;
                Fields = fields;
                Methods = methods;
                Constructor = null;
            }

            public Type Type { get; }
            public IEnumerable<FieldInfo> Fields { get; }
            public IEnumerable<MethodBase> Methods { get; }
            public MethodBase Constructor { get; }
        }

        private static readonly Dictionary<Type, TypeCache> typesCache = new Dictionary<Type, TypeCache>();
        private static readonly Type unityObjectType = typeof(UnityEngine.Object);

        public static void InjectRecursively(MonoBehaviour monoBehaviour, IServiceProvider serviceProvider)
        {
            var serviceScopeType = typeof(ServiceScope);
            var children = monoBehaviour.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var child in children)
            {
                if (serviceScopeType.IsAssignableFrom(child.GetType())) continue;

                var serviceScope = (ServiceScope)child.GetComponentInParent(serviceScopeType);
                if (serviceScope != null && serviceScope.ServiceProvider == null)
                {
                    serviceScope.Setup(serviceProvider);
                }

                Inject(child, serviceScope != null ? serviceScope.ServiceProvider : serviceProvider);
            }
        }


        public static void Inject(object dependant, IServiceProvider serviceProvider)
        {
            Type type = dependant.GetType();
            if (!typesCache.TryGetValue(type, out TypeCache cache))
            {
                cache = typesCache[type] = CalculateType(type);
            }

            if (cache.Fields != null)
            {
                foreach (var field in cache.Fields)
                {
                    InjectField(dependant, serviceProvider, field);
                }
            }

            if (cache.Methods != null)
            {
                foreach (var method in cache.Methods)
                {
                    InjectMethod(dependant, method, serviceProvider);
                }
            }

            if (cache.Constructor != null)
            {
                InjectMethod(dependant, cache.Constructor, serviceProvider);
            }
        }

        private static TypeCache CalculateType(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f => f.GetCustomAttribute<InjectAttribute>(false) != null);

            if (unityObjectType.IsAssignableFrom(type))
            {
                var methods = new List<MethodBase>();
                var currentType = type;
                while (currentType != null && currentType != unityObjectType)
                {
                    var currentTypeMethods = currentType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance).Where(m => m.GetCustomAttribute<InjectAttribute>(false) != null);
                    if (currentTypeMethods.Count() > 1)
                    {
                        throw new InvalidOperationException($"Type {type.FullName} has more than one injected method. There can only be one per class.");
                    }
                    if (currentTypeMethods.Any())
                    {
                        var method = currentTypeMethods.First();
                        if (method.ReturnType != typeof(void))
                        {
                            throw new InvalidOperationException($"Type {type.FullName} has an invalid injected method. They can only return void.");
                        }
                        methods.Add(currentTypeMethods.First());
                    }
                    currentType = currentType.BaseType;
                }

                return new TypeCache(type, fields, methods);
            }
            else
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                if (constructors.Count() > 1)
                {
                    constructors = constructors.Where(c => c.GetCustomAttribute<InjectAttribute>(false) != null).ToArray();

                    if (constructors.Count() > 1)
                    {
                        throw new InvalidOperationException($"Type {type.FullName} has more than one constructor. There can only be one per class or if there are many, only one with the inject attribute.");
                    }
                }

                return new TypeCache(type, fields, constructors.First());
            }
        }

        private static void InjectField(object dependant, IServiceProvider serviceProvider, FieldInfo field)
        {
            field.SetValue(dependant, serviceProvider.GetService(field.FieldType));
        }

        private static void InjectMethod(object dependant, MethodBase method, IServiceProvider serviceProvider)
        {
            var parameters = method.GetParameters();
            var values = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                values[i] = serviceProvider.GetService(parameters[i].ParameterType);
            }
            method.Invoke(dependant, values);
        }
    }
}