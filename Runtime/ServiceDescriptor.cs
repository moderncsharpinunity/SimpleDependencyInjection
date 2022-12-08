using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public class ServiceDescriptor
    {
        public ServiceDescriptor(Type type, ServiceLifetime lifetime, ServiceFactoryDelegate factory) 
        {
            Type = type;
            Lifetime = lifetime;
            Factory = factory;
        }

        public Type Type { get; }
        public ServiceLifetime Lifetime { get; }
        public ServiceFactoryDelegate Factory { get; }
    }

    public enum ServiceLifetime
    {
        Singleton,
        Transient,
        Scoped,
    }

    public delegate object ServiceFactoryDelegate(IServiceProvider dependencies);
    public delegate T ServiceFactoryDelegate<out T>(IServiceProvider dependencies) where T : class;
}