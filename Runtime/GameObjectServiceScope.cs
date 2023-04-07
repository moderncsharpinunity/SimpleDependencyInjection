using System;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public class GameObjectServiceScope : MonoBehaviour
    {
        protected readonly ServiceContext serviceContext  = new ServiceContext();

        public IServiceProvider ServiceProvider => serviceContext.ServiceProvider;

        public void Build(IServiceProvider parentServiceProvider = null)
        {
            serviceContext.Build(parentServiceProvider);
        }
    }
}
