using Cysharp.Threading.Tasks;
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

        public UniTask Configure(IServiceCollection serviceCollection, ModuleContext context)
        {
            serviceCollection.Register<ExampleDependencyMonoBehaviour>().AsSingleton().FromGameObject(exampleDependency);

            serviceCollection.Register<ExampleDependencyPlainClass>().AsTransient().FromSame();

            serviceCollection.Register<ExampleDependencyNested>().AsSingleton().FromPrefab(exampleDependencyNestedPrefab);

            return UniTask.CompletedTask;
        }

        public UniTask Init(IServiceProvider serviceProvider, ModuleContext context)
        {
            return UniTask.CompletedTask;
        }
    }
}