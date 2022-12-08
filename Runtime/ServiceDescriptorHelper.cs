using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public interface IServiceDescriptorHelperAs<T> where T : class
    {
        IServiceDescriptorHelperAsSingleton<T> AsSingleton();
        IServiceDescriptorHelperAsLifetime<T> AsTransient();
        IServiceDescriptorHelperAsLifetime<T> AsScoped();
    }

    public interface IServiceDescriptorHelperAsLifetime<T> where T : class
    {
        void From<C>(ServiceFactoryDelegate<T> factory) where C : class, T;
        void FromSame();
        void FromNew<C>() where C : class, T;
        void FromPrefab<C>(C prefab) where C : MonoBehaviour, T;
        void FromNewGameObject<C>() where C : MonoBehaviour, T;
    }

    public interface IServiceDescriptorHelperAsSingleton<T> : IServiceDescriptorHelperAsLifetime<T> where T : class
    {
        void FromInstance(T instance);
        void FromGameObject<C>(C gameObject) where C : MonoBehaviour, T;
    }

    internal class ServiceDescriptorHelper<T> : IServiceDescriptorHelperAs<T>, IServiceDescriptorHelperAsLifetime<T>, IServiceDescriptorHelperAsSingleton<T> where T : class
    {
        private IServiceCollection serviceCollection;
        private ServiceLifetime lifetime;
        private ServiceFactoryDelegate<T> factory;

        public ServiceDescriptorHelper(IServiceCollection serviceCollection)
        {
            this.serviceCollection = serviceCollection;
        }

        public IServiceDescriptorHelperAsSingleton<T> AsSingleton()
        {
            lifetime = ServiceLifetime.Singleton;
            return this;
        }

        public IServiceDescriptorHelperAsLifetime<T> AsTransient()
        {
            lifetime = ServiceLifetime.Transient;
            return this;
        }

        public IServiceDescriptorHelperAsLifetime<T> AsScoped()
        {
            lifetime = ServiceLifetime.Scoped;
            return this;
        }

        public void Build()
        {
            serviceCollection.Register(new ServiceDescriptor(typeof(T), lifetime, (serviceProvider) => factory(serviceProvider)));
        }

        public void From<C>(ServiceFactoryDelegate<T> factory) where C : class, T
        {
            this.factory = factory;
            Build();
        }

        public void FromSame()
        {
            FromNew<T>();
        }

        public void FromNew<C>() where C : class, T
        {
            From<C>(ServiceFactory.FromNew<C>());
        }

        public void FromPrefab<C>(C prefab) where C : MonoBehaviour, T
        {
            From<C>(ServiceFactory.FromPrefab(prefab));
        }

        public void FromNewGameObject<C>() where C : MonoBehaviour, T
        {
            From<C>(ServiceFactory.FromNewGameObject<C>());
        }

        public void FromInstance(T instance)
        {
            From<T>((serviceProvider) => instance);
        }

        public void FromGameObject<C>(C gameObject) where C : MonoBehaviour, T
        {
            From<T>(ServiceFactory.FromGameObject<C>(gameObject));
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceDescriptorHelperAs<T> Register<T>(this IServiceCollection serviceCollection) where T : class
        {
            return new ServiceDescriptorHelper<T>(serviceCollection);
        }
    }
}
