using NUnit.Framework;

namespace SimpleDependencyInjection
{
    public class ServiceProviderTests
    {
        private class Dependency { }

        [Test]
        public void Given_NonExistingDependency_When_GetService_Then_NullIsReturned()
        {
            var serviceCollection = new ServiceCollection();
            var serviceProvider = new ServiceProvider(serviceCollection);

            Assert.That(() => serviceProvider.GetService<Dependency>(), Throws.ArgumentException);
        }

        [Test]
        public void Given_TransientDependency_When_GetServiceTwice_Then_InstancesAreDifferent()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsTransient().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);

            var instance1 = serviceProvider.GetService<Dependency>();
            var instance2 = serviceProvider.GetService<Dependency>();
            Assert.That(instance1, Is.Not.EqualTo(instance2));
        }

        [Test]
        public void Given_SingletonDependency_When_GetServiceTwice_Then_InstancesAreSame()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsSingleton().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);

            var instance1 = serviceProvider.GetService<Dependency>();
            var instance2 = serviceProvider.GetService<Dependency>();
            Assert.That(instance1, Is.EqualTo(instance2));
        }

        [Test]
        public void Given_NestedProviderAndTransientDependencyInParentProvider_When_GetServiceInChildProvider_Then_InstanceIsReturned()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsTransient().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);
            var nestedProvider = new ServiceProvider(new ServiceCollection(), serviceProvider);

            Assert.That(nestedProvider.GetService<Dependency>(), Is.Not.Null);
        }

        [Test]
        public void Given_NestedProviderAndSingletonDependencyInParentProvider_When_GetServiceInChildProvider_Then_InstanceIsSameAsParentProvider()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsSingleton().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);
            var nestedProvider = new ServiceProvider(new ServiceCollection(), serviceProvider);

            var instance1 = serviceProvider.GetService<Dependency>();
            var instance2 = nestedProvider.GetService<Dependency>();
            Assert.That(instance1, Is.EqualTo(instance2));
        }

        [Test]
        public void Given_NestedProviderAndScopedDependencyInParentProvider_When_GetServiceInChildProvider_Then_InstanceIsDifferent()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsScoped().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);
            var nestedProvider = new ServiceProvider(new ServiceCollection(), serviceProvider);

            var instance1 = serviceProvider.GetService<Dependency>();
            var instance2 = nestedProvider.GetService<Dependency>();
            Assert.That(instance1, Is.Not.EqualTo(instance2));
        }

        [Test]
        public void Given_NestedProviderAndScopedDependencyInParentProvider_When_GetServiceInParentProviderTwice_Then_InstanceIsSame()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsScoped().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);

            var instance1 = serviceProvider.GetService<Dependency>();
            var instance2 = serviceProvider.GetService<Dependency>();
            Assert.That(instance1, Is.EqualTo(instance2));
        }

        [Test]
        public void Given_NestedProviderAndScopedDependencyInParentProvider_When_GetServiceInChildProviderTwice_Then_InstanceIsSame()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsScoped().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);
            var nestedProvider = new ServiceProvider(new ServiceCollection(), serviceProvider);

            var instance1 = nestedProvider.GetService<Dependency>();
            var instance2 = nestedProvider.GetService<Dependency>();
            Assert.That(instance1, Is.EqualTo(instance2));
        }

        [Test]
        public void Given_TripleNestedProviderAndScopedDependencyInParentProvider_When_GetServiceInGrandChildProvider_Then_InstancesAreDifferent()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsScoped().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);
            var nestedProvider1 = new ServiceProvider(new ServiceCollection(), serviceProvider);
            var nestedProvider2 = new ServiceProvider(new ServiceCollection(), nestedProvider1);

            var instance1 = serviceProvider.GetService<Dependency>();
            var instance2 = nestedProvider1.GetService<Dependency>();
            var instance3 = nestedProvider2.GetService<Dependency>();
            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance1, Is.Not.EqualTo(instance3));
        }

        [Test]
        public void Given_TripleNestedProviderAndScopedDependencyInParentProvider_When_GetServiceInGrandChildProviderTwice_Then_InstancesAreSame()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Register<Dependency>().AsScoped().FromSame();
            var serviceProvider = new ServiceProvider(serviceCollection);
            var nestedProvider1 = new ServiceProvider(new ServiceCollection(), serviceProvider);
            var nestedProvider2 = new ServiceProvider(new ServiceCollection(), nestedProvider1);

            var instance1 = nestedProvider2.GetService<Dependency>();
            var instance2 = nestedProvider2.GetService<Dependency>();
            Assert.That(instance1, Is.EqualTo(instance2));
        }
    }
}