using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public class ServiceCollection : IEnumerable<ServiceDescriptor>, IServiceCollection
    {
        private List<ServiceDescriptor> services = new List<ServiceDescriptor>();

        public void Register(ServiceDescriptor service) => services.Add(service);

        public IEnumerator<ServiceDescriptor> GetEnumerator() => services.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => services.GetEnumerator();
    }
}