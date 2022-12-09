using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITASK
using Task = Cysharp.Threading.Tasks.UniTask;
#else
using Task = System.Threading.Tasks.Task;
#endif

namespace SimpleDependencyInjection
{
    [DefaultExecutionOrder(-1)]
    public abstract class ServiceContext : MonoBehaviour
    {
        private List<IModuleAsync> modules = new List<IModuleAsync>();
        private ServiceCollection serviceCollection = new ServiceCollection();
        private ServiceProvider serviceProvider;


        private void Awake()
        {
            Init();
        }

        private async void Init()
        {
            DontDestroyOnLoad(gameObject);
            Setup();

            foreach (var module in modules)
            {
                await module.Configure(serviceCollection);
            }

            serviceProvider = new ServiceProvider(serviceCollection);

            foreach (var module in modules)
            {
                await module.Init(serviceProvider);
            }

            var children = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var child in children)
            {
                ServiceInjector.Inject(child, serviceProvider);
            }            
        }

        protected abstract void Setup();

        protected void AddModule(IModule module)
        {
            modules.Add(new AsyncModuleConverter(module));
        }

        protected void AddModule(IModuleAsync module)
        {
            modules.Add(module);
        }

        protected void AddModule(Action configure, Action init = null)
        {
            modules.Add(new SimpleModule(configure, init));
        }

        private class SimpleModule : IModuleAsync
        {
            private readonly Action configure;
            private readonly Action init;

            public SimpleModule(Action configure, Action init)
            {
                this.configure = configure;
                this.init = init;
            }

            public Task Configure(IServiceCollection serviceCollection)
            {
                configure?.Invoke();
                return Task.CompletedTask;
            }

            public Task Init(IServiceProvider serviceProvider)
            {
                init?.Invoke();
                return Task.CompletedTask;
            }
        }

        private class AsyncModuleConverter : IModuleAsync
        {
            private readonly IModule module;

            public AsyncModuleConverter(IModule module)
            {
                this.module = module;
            }

            public Task Configure(IServiceCollection serviceCollection)
            {
                module.Configure(serviceCollection);
                return Task.CompletedTask;
            }

            public Task Init(IServiceProvider serviceProvider)
            {
                module.Init(serviceProvider);
                return Task.CompletedTask;
            }
        }
    }
}