using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SimpleDependencyInjection
{
    public class ServiceContext
    {
        private readonly List<IModule> modules = new List<IModule>();

        public IServiceProvider ServiceProvider { get; private set; }

        public void Build(IServiceProvider parentServiceProvider = null)
        {
            var serviceCollection = new ServiceCollection();
            foreach (var module in modules)
            {
                module.Configure(serviceCollection);
            }

            ServiceProvider = new ServiceProvider(serviceCollection, parentServiceProvider);

            foreach (var module in modules)
            {
                module.Init(ServiceProvider);
            }
        }

        public void AddModule(IModule module)
        {
            Assert.IsNotNull(module, "Modules cannot be null");
            modules.Add(module);
        }

        public void AddModule(Action<IServiceCollection> configure, Action<IServiceProvider> init = null)
        {
            Assert.IsNotNull(configure, "Module's configure cannot be null");
            modules.Add(new SimpleModule(configure, init));
        }

        private class SimpleModule : IModule
        {
            private readonly Action<IServiceCollection> configure;
            private readonly Action<IServiceProvider> init;

            public SimpleModule(Action<IServiceCollection> configure, Action<IServiceProvider> init)
            {
                this.configure = configure;
                this.init = init;
            }

            public void Configure(IServiceCollection serviceCollection)
            {
                configure?.Invoke(serviceCollection);
            }

            public void Init(IServiceProvider serviceProvider)
            {
                init?.Invoke(serviceProvider);
            }
        }
    }
}