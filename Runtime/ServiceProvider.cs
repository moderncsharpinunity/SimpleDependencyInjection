using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleDependencyInjection
{
    internal class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, ServiceDescriptor> dependencies = new Dictionary<Type, ServiceDescriptor>();
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();
        private readonly IServiceProvider parentServiceProvider;

        public ServiceProvider(ServiceCollection dependencies, IServiceProvider parentServiceProvider = null)
        {
            foreach (var dependency in dependencies)
            {
                this.dependencies.Add(dependency.Type, dependency);
            }

            this.parentServiceProvider = parentServiceProvider;
        }

        public object GetService(Type type)
        {
            if (!dependencies.ContainsKey(type))
            {
                if (parentServiceProvider != null)
                {
                    return parentServiceProvider.GetService(type);
                }

                throw new ArgumentException("Type is not a dependency: " + type.FullName);
            }

            var dependency = dependencies[type];
            if (dependency.Lifetime == ServiceLifetime.Singleton)
            {
                if (parentServiceProvider != null)
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
    }
}
