using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SimpleDependencyInjection.Tests", AllInternalsVisible = true)]

namespace SimpleDependencyInjection
{
    internal class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, ServiceDescriptor> dependencies = new Dictionary<Type, ServiceDescriptor>();
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();
        private readonly ServiceProvider parentServiceProvider;

        public ServiceProvider(ServiceCollection dependencies, IServiceProvider parentServiceProvider = null)
        {
            foreach (var dependency in dependencies)
            {
                this.dependencies.Add(dependency.Type, dependency);
            }

            this.parentServiceProvider = (ServiceProvider)parentServiceProvider;
        }

        private ServiceDescriptor GetServiceDescriptor(Type type)
        {
            if (!dependencies.TryGetValue(type, out var dependency))
            {
                if (parentServiceProvider != null)
                {
                    return parentServiceProvider.GetServiceDescriptor(type);
                }
                else
                {
                    return null;
                }
            }
            return dependency;
        }

        public object GetService(Type type)
        {
            var dependency = GetServiceDescriptor(type);
            if (dependency == null)
            {
                throw new ArgumentException("Type is not a dependency: " + type.FullName);
            }
            bool dependencyIsMine = dependencies.ContainsKey(type);

            if (dependency.Lifetime == ServiceLifetime.Singleton)
            {
                if (!dependencyIsMine)
                {
                    return parentServiceProvider.GetService(type);
                }

                if (!singletons.ContainsKey(type))
                {
                    singletons.Add(type, dependency.Factory(this));
                }
                return singletons[type];
            }
            else if (dependency.Lifetime == ServiceLifetime.Scoped)
            {
                if (!singletons.ContainsKey(type))
                {
                    singletons.Add(type, dependency.Factory(this));
                }
                return singletons[type];
            }
            else
            {
                return dependency.Factory(this);
            }
        }

        public T GetService<T>() where T : class
        {
            return (T)GetService(typeof(T));
        }
    }
}
