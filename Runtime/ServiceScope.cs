using System;
using System.Reflection;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public class ServiceScope : MonoBehaviour, IModule
    {
        private ServiceProvider serviceProvider;

        public IServiceProvider ServiceProvider => serviceProvider;

        public void Setup(IServiceProvider parentServiceProvider)
        {
            if (serviceProvider != null) throw new InvalidOperationException("ServiceScope cannot be initialized twice");

            var serviceCollection = new ServiceCollection();

            Configure(serviceCollection);

            serviceProvider = new ServiceProvider(serviceCollection, parentServiceProvider);

            Init(serviceProvider);
        }

        public virtual void Configure(IServiceCollection serviceCollection)
        {

        }

        public virtual void Init(IServiceProvider serviceProvider)
        {

        }
    }
}
