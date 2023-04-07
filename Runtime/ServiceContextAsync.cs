using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace SimpleDependencyInjection
{
    public class ServiceContextAsync
    {
        private readonly List<IModuleAsync> modules = new List<IModuleAsync>();

        public IServiceProvider ServiceProvider { get; private set; }

        public async UniTask Build(IServiceProvider parentServiceProvider = null)
        {
            var serviceCollection = new ServiceCollection();
            foreach (var module in modules)
            {
                await module.Configure(serviceCollection);
            }

            ServiceProvider = new ServiceProvider(serviceCollection, parentServiceProvider);

            foreach (var module in modules)
            {
                await module.Init(ServiceProvider);
            }
        }

        public void AddModule(IModuleAsync module)
        {
            Assert.IsNotNull(module, "Modules cannot be null");
            modules.Add(module);
        }

        public void AddModule(IModule module)
        {
            Assert.IsNotNull(module, "Modules cannot be null");
            modules.Add(new AsyncModuleConverter(module));
        }

        public void AddModule(Action<IServiceCollection> configure, Action<IServiceProvider> init = null)
        {
            Assert.IsNotNull(configure, "Module's configure cannot be null");
            modules.Add(new SimpleModule(configure, init));
        }

        private class AsyncModuleConverter : IModuleAsync
        {
            private readonly IModule module;

            public AsyncModuleConverter(IModule module)
            {
                this.module = module;
            }

            public UniTask Configure(IServiceCollection serviceCollection)
            {
                module.Configure(serviceCollection);
                return UniTask.CompletedTask;
            }

            public UniTask Init(IServiceProvider serviceProvider)
            {
                module.Init(serviceProvider);
                return UniTask.CompletedTask;
            }
        }

        private class SimpleModule : IModuleAsync
        {
            private readonly Action<IServiceCollection> configure;
            private readonly Action<IServiceProvider> init;

            public SimpleModule(Action<IServiceCollection> configure, Action<IServiceProvider> init)
            {
                this.configure = configure;
                this.init = init;
            }

            public UniTask Configure(IServiceCollection serviceCollection)
            {
                configure?.Invoke(serviceCollection);
                return UniTask.CompletedTask;
            }

            public UniTask Init(IServiceProvider serviceProvider)
            {
                init?.Invoke(serviceProvider);
                return UniTask.CompletedTask;
            }
        }
    }
}
