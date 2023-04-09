using SimpleDependencyInjection;
using System;
using UnityEngine;

namespace Example
{
    public class ExampleDependenciesAppScope : AppServiceScope
    {
        [SerializeField]
        private ExampleDependencyModule exampleDependencyModule;

        protected override void Setup()
        {
            serviceContext.AddModule(exampleDependencyModule);
        }
    }
}
