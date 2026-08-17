using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SimpleDependencyInjection.Tests", AllInternalsVisible = true)]

namespace SimpleDependencyInjection
{
    internal class ServiceProvider : IServiceProvider, IDisposable
    {
        readonly Dictionary<Type, ServiceDescriptor> dependencies = new Dictionary<Type, ServiceDescriptor>();
        readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        /// <summary>Singleton/scoped instances THIS provider created, in creation order —
        /// disposed in reverse on <see cref="Dispose"/> (Fix 4, 0.3.0). Never a parent's;
        /// each provider owns and disposes only what it itself built. Transients are
        /// deliberately not tracked here — see the package docs.</summary>
        readonly List<IDisposable> disposables = new List<IDisposable>();

        readonly ServiceProvider parentServiceProvider;

        /// <summary>Shows up in a <see cref="ServiceNotRegisteredException"/>'s scope
        /// chain (Feature 8, 0.3.0). Defaults to a generic label if the owning
        /// <see cref="ServiceContext"/> didn't set one.</summary>
        public string Name { get; }

        public ServiceProvider(ServiceCollection dependencies, IServiceProvider parentServiceProvider = null, string name = null)
        {
            foreach (var dependency in dependencies)
            {
                this.dependencies.Add(dependency.Type, dependency);
            }

            this.parentServiceProvider = (ServiceProvider)parentServiceProvider;
            Name = string.IsNullOrEmpty(name) ? "(unnamed scope)" : name;
        }

        IEnumerable<string> ScopeChain()
        {
            for (var p = this; p != null; p = p.parentServiceProvider)
                yield return p.Name;
        }

        ServiceDescriptor GetServiceDescriptor(Type type)
        {
            if (!dependencies.TryGetValue(type, out var dependency))
            {
                return parentServiceProvider != null ? parentServiceProvider.GetServiceDescriptor(type) : null;
            }
            return dependency;
        }

        public object GetService(Type type)
        {
            var dependency = GetServiceDescriptor(type);
            if (dependency == null)
            {
                throw new ServiceNotRegisteredException(type, ScopeChain().ToList(),
                    dependencies.Keys.Select(t => t.FullName).ToList());
            }

            bool dependencyIsMine = dependencies.ContainsKey(type);

            if (dependency.Lifetime == ServiceLifetime.Singleton)
            {
                if (!dependencyIsMine)
                {
                    return parentServiceProvider.GetService(type);
                }
                return GetOrCreateTracked(type, dependency);
            }
            else if (dependency.Lifetime == ServiceLifetime.Scoped)
            {
                return GetOrCreateTracked(type, dependency);
            }
            else
            {
                // Transient — not tracked for disposal. Owning a transient's lifetime,
                // including disposing it, is the caller's responsibility.
                return dependency.Factory(this);
            }
        }

        object GetOrCreateTracked(Type type, ServiceDescriptor dependency)
        {
            if (singletons.TryGetValue(type, out var existing)) return existing;

            var instance = dependency.Factory(this);
            singletons.Add(type, instance);
            if (instance is IDisposable disposable) disposables.Add(disposable);
            return instance;
        }

        public T GetService<T>() where T : class
        {
            return (T)GetService(typeof(T));
        }

        /// <summary>Disposes every <see cref="IDisposable"/> singleton/scoped instance
        /// THIS provider created, in reverse creation order — never a parent's or a
        /// child's. Never called automatically by the container (Fix 4, 0.3.0); the
        /// owning scope (<see cref="AppServiceScope"/>, <see cref="GameObjectServiceScope"/>,
        /// <see cref="GlobalServiceScope"/>) calls this from its own teardown.</summary>
        public void Dispose()
        {
            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
            disposables.Clear();
        }
    }
}
