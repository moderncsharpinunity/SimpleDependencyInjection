using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleDependencyInjection
{
    public class DependenciesProvider : IServiceProvider
    {
        private Dictionary<Type, Dependency> dependencies = new Dictionary<Type, Dependency>();
        private Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        public DependenciesProvider(DependenciesCollection dependencies)
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
            if (dependency.IsSingleton)
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

        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        public object Inject(object dependant)
        {
            Type type = dependant.GetType();
            while (type != null)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InjectFieldAttribute>(false) == null) { continue; }

                    field.SetValue(dependant, GetService(field.FieldType));
                }
                type = type.BaseType;
            }
            return dependant;
        }
    }
}
