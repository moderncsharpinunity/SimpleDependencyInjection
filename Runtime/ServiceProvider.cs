using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleDependencyInjection
{
    internal class ServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, ServiceDescriptor> dependencies = new Dictionary<Type, ServiceDescriptor>();
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        public ServiceProvider(ServiceCollection dependencies)
        {
            foreach (var dependency in dependencies)
            {
                this.dependencies.Add(dependency.Type, dependency);
            }
        }

        public object GetService(Type type)
        {
            if (!dependencies.ContainsKey(type))
            {
                throw new ArgumentException("Type is not a dependency: " + type.FullName);
            }

            var dependency = dependencies[type];
            if (dependency.Lifetime == ServiceLifetime.Singleton)
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
