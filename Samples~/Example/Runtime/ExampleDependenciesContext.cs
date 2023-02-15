using SimpleDependencyInjection;
using System;
using UnityEngine;

namespace Example
{
    public class ExampleDependenciesContext : ServiceContext, IModule
    {
        [SerializeField]
        private ExampleDependencyMonoBehaviour exampleDependency;
        [SerializeField]
        private ExampleDependencyNested exampleDependencyNested;

        protected override void Setup()
        {
            AddModule(this);
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.Register<ExampleDependencyMonoBehaviour>().AsSingleton().FromGameObject(exampleDependency);

            serviceCollection.Register<ExampleDependencyPlainClass>().AsTransient().FromSame();

            serviceCollection.Register<ExampleDependencyNested>().AsSingleton().FromPrefab(exampleDependencyNested);
        }

        public void Init(IServiceProvider serviceProvider)
        {
        }
    }
}
