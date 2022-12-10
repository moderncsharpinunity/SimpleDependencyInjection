using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SimpleDependencyInjection
{
    public class PerformanceTests
    {
        private class TestMonoBehaviour : MonoBehaviour
        {
        }

        private class TestServiceContext : ServiceContext
        {
            protected override void Setup()
            {
            }
        }

        [Test]
        //[Explicit]
        public void CheckInjectIterationPerformance()
        {
            int totalChildrenCount = 0;
            void CreateChildren(GameObject parent, int childrenCount, int level)
            {
                if (level >= 6) return;

                level++;
                for (int i = 0; i < childrenCount; i++)
                {
                    totalChildrenCount++;
                    var child = new GameObject($"child{level}.{i}");
                    child.transform.SetParent(parent.transform, false);
                    child.AddComponent<TestMonoBehaviour>();
                    if (totalChildrenCount % 100 == 0)
                    {
                        child.AddComponent<ServiceScope>();
                    }
                    CreateChildren(child, childrenCount, level);
                }
            }

            void IterateChildren(Transform transform, IServiceProvider serviceProvider)
            {
                var serviceScope = transform.GetComponent<ServiceScope>();
                if (serviceScope != null)
                {
                    serviceProvider = serviceScope.ServiceProvider;
                }

                foreach (var monobehaviour in transform.GetComponents<MonoBehaviour>())
                {

                }

                foreach (Transform child in transform)
                {
                    IterateChildren(child, serviceProvider);
                }
            }

            var stopWatch = new Stopwatch();

            stopWatch.Start();
            var root = new GameObject("root");
            var serviceContext = root.AddComponent<TestServiceContext>();
            CreateChildren(root, 10, 1);
            stopWatch.Stop();
            Debug.Log($"Creating {totalChildrenCount} children took {stopWatch.Elapsed.TotalMilliseconds}");
            stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();
                var serviceScopeType = typeof(ServiceScope);
                var children = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var child in children)
                {
                    if (serviceScopeType.IsAssignableFrom(child.GetType())) continue;
                }
                stopWatch.Stop();
            }
            Debug.Log($"Traversing children took {stopWatch.Elapsed.TotalMilliseconds / 10}");
            stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();
                var serviceScopeType = typeof(ServiceScope);
                var children = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var child in children)
                {
                    if (serviceScopeType.IsAssignableFrom(child.GetType())) continue;

                    var serviceScope = (ServiceScope)child.GetComponentInParent(serviceScopeType);
                }
                stopWatch.Stop();
            }
            Debug.Log($"Traversing children (with scope) took {stopWatch.Elapsed.TotalMilliseconds / 10}");
            stopWatch.Reset();

            for (int i = 0; i < 10; i++)
            {
                stopWatch.Start();
                ServiceInjector.InjectRecursively(serviceContext, serviceContext.ServiceProvider);
                stopWatch.Stop();
            }
            Debug.Log($"Traversing and injecting children took {stopWatch.Elapsed.TotalMilliseconds / 10}");
            stopWatch.Reset();

            stopWatch.Start();
            IterateChildren(root.transform, serviceContext.ServiceProvider);
            stopWatch.Stop();
            Debug.Log($"Alternative traversing children took {stopWatch.Elapsed.TotalMilliseconds}");
            stopWatch.Reset();
        }
    }
}
