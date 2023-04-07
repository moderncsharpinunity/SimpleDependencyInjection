using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDependencyInjection
{
    [DefaultExecutionOrder(-1)]
    public abstract class AppServiceScope : MonoBehaviour
    {
        protected readonly ServiceContextAsync serviceContext = new ServiceContextAsync();
        private readonly Stack<IServiceProvider> serviceProviders = new Stack<IServiceProvider>();

        public IServiceProvider ServiceProvider { get; private set; }

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);

            serviceContext.AddModule((services) => services.Register<AppServiceScope>().AsSingleton().FromInstance(this));

            Setup();

            gameObject.SetActive(false);

            await serviceContext.Build();

            ServiceProvider = serviceContext.ServiceProvider;

            ServiceInjector.InjectRecursively(this, ServiceProvider);

            gameObject.SetActive(true);
        }

        protected abstract void Setup();

        public void PushServiceProvider(IServiceProvider serviceProvider)
        {
            serviceProviders.Push(ServiceProvider);
            ServiceProvider = serviceProvider;
        }

        public void PopServiceProvider()
        {
            ServiceProvider = serviceProviders.Pop();
        }
    }
}
