using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace SimpleDependencyInjection
{
    public static class ServiceFactory
    {
        public static ServiceFactoryDelegate<T> FromNew<T>() where T : class
        {
            return (serviceProvider) =>
            {
                var type = typeof(T);
                var obj = FormatterServices.GetUninitializedObject(type);

                ServiceInjector.Inject(obj, serviceProvider);

                return (T)obj;
            };
        }

        public static ServiceFactoryDelegate<T> FromPrefab<T>(T prefab) where T : MonoBehaviour
        {
            return (serviceProvider) =>
            {
                bool wasActive = prefab.gameObject.activeSelf;
                prefab.gameObject.SetActive(false);
                var instance = GameObject.Instantiate(prefab);
                prefab.gameObject.SetActive(wasActive);

                ServiceInjector.InjectRecursively(instance, serviceProvider);

                instance.gameObject.SetActive(wasActive);
                return instance.GetComponent<T>();
            };
        }

        public static ServiceFactoryDelegate<T> FromNewGameObject<T>() where T : MonoBehaviour
        {
            return (serviceProvider) =>
            {
                var type = typeof(T);
                var gameObject = new GameObject(type.Name);
                gameObject.SetActive(false);
                var instance = gameObject.AddComponent(type);

                ServiceInjector.Inject(instance, serviceProvider);

                gameObject.SetActive(true);
                return (T)instance;
            };
        }

        public static ServiceFactoryDelegate<T> FromGameObject<T>(T instance) where T : MonoBehaviour
        {
            return (serviceProvider) =>
            {
                ServiceInjector.InjectRecursively(instance, serviceProvider);
                return instance;
            };
        }
        
    }
}
