using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public class ServiceCollection : IEnumerable<ServiceDescriptor>, IServiceCollection
    {
        private List<ServiceDescriptor> dependencies = new List<ServiceDescriptor>();

        public void Add(ServiceDescriptor dependency) => dependencies.Add(dependency);

        public IEnumerator<ServiceDescriptor> GetEnumerator() => dependencies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => dependencies.GetEnumerator();
    }
}