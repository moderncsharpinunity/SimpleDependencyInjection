using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SimpleDependencyInjection
{
    [DefaultExecutionOrder(-1)]
    public abstract class ServiceContext : MonoBehaviour
    {
        protected ServiceCollection dependenciesCollection = new ServiceCollection();
        private ServiceProvider dependenciesProvider;


        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Setup();

            dependenciesProvider = new ServiceProvider(dependenciesCollection);

            var children = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var child in children)
            {
                ServiceInjector.Inject(child, dependenciesProvider);
            }

            Configure();
        }

        protected abstract void Setup();

        protected abstract void Configure();

    }
}