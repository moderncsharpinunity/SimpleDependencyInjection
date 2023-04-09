using SimpleDependencyInjection;
using System;
using UnityEngine;

namespace Example
{
    public class ExampleDependencyModule : MonoBehaviour, IModule
    {
        [SerializeField]
        private ExampleDependencyMonoBehaviour exampleDependency;
        [SerializeField]
        private ExampleDependencyNested exampleDependencyNestedPrefab;

        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.Register<ExampleDependencyMonoBehaviour>().AsSingleton().FromGameObject(exampleDependency);

            serviceCollection.Register<ExampleDependencyPlainClass>().AsTransient().FromSame();

            serviceCollection.Register<ExampleDependencyNested>().AsSingleton().FromPrefab(exampleDependencyNestedPrefab);
        }

        public void Init(IServiceProvider serviceProvider)
        {
        }
    }
}