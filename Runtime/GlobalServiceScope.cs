using System;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public abstract class GlobalServiceScope
    {
        protected readonly ServiceContext serviceContext = new ServiceContext();
        private readonly AppServiceScope appServiceScope;

        public IServiceProvider ServiceProvider => serviceContext.ServiceProvider;

        public GlobalServiceScope(AppServiceScope appServiceScope)
        {
            this.appServiceScope = appServiceScope;
        }

        public void Activate()
        {
            if (serviceContext.ServiceProvider == null)
            {
                serviceContext.Build();
            }

            appServiceScope.PushServiceProvider(ServiceProvider);
        }

        public void Deactivate()
        {
            if (appServiceScope.ServiceProvider != ServiceProvider)
            {
                Debug.LogError("Unable to deactivate global service scope, is not currently active");
                return;
            }

            appServiceScope.PopServiceProvider();
        }
    }
}
