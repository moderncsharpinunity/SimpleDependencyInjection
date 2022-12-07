using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public struct ServiceDescriptor
    {
        public Type Type { get; set; }
        public ServiceFactoryDelegate Factory { get; set; }
        public bool IsSingleton { get; set; }
    }

    public delegate object ServiceFactoryDelegate(IServiceProvider dependencies);
}