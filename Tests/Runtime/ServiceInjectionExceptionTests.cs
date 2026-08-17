using NUnit.Framework;

namespace SimpleDependencyInjection
{
    /// <summary>Feature 8 (0.3.0) — a failed resolution names the scope, the object, the
    /// member and the service involved, instead of a bare "type is not a dependency".</summary>
    public class ServiceInjectionExceptionTests
    {
        interface IMissing { }

        class HasInjectField : UnityEngine.MonoBehaviour
        {
            [Inject] IMissing dependency = null;
        }

        [Test]
        public void Given_UnregisteredService_When_GetService_Then_ExceptionIsInstanceOfArgumentException()
        {
            // Existing consumers (e.g. Given_NonExistingDependency_...) assert
            // Throws.ArgumentException — ServiceNotRegisteredException must stay a
            // subclass so that keeps working unmodified.
            var provider = new ServiceProvider(new ServiceCollection());

            Assert.That(() => provider.GetService<IMissing>(), Throws.InstanceOf<System.ArgumentException>());
        }

        interface IOtherService { }
        class OtherServiceImpl : IOtherService { }

        [Test]
        public void Given_UnregisteredService_When_GetService_Then_ExceptionNamesTheServiceTypeAndScope()
        {
            var services = new ServiceCollection();
            services.Register<IOtherService>().AsTransient().FromNew<OtherServiceImpl>(); // gives the provider SOMETHING registered
            var provider = new ServiceProvider(services, name: "TestScope");

            var ex = Assert.Throws<ServiceNotRegisteredException>(() => provider.GetService<IMissing>());

            Assert.That(ex.ServiceType, Is.EqualTo(typeof(IMissing)));
            Assert.That(ex.ScopeChain, Does.Contain("TestScope"));
            Assert.That(ex.Message, Does.Contain(nameof(IMissing)));
            Assert.That(ex.Message, Does.Contain("TestScope"));
        }

        [Test]
        public void Given_FieldInjectionFailsBecauseServiceIsUnregistered_When_Injected_Then_ThrowsServiceInjectionExceptionNamingTheField()
        {
            var provider = new ServiceProvider(new ServiceCollection(), name: "SceneScope");
            var go = new UnityEngine.GameObject("TheGameObject");
            try
            {
                var target = go.AddComponent<HasInjectField>();

                var ex = Assert.Throws<ServiceInjectionException>(() => ServiceInjector.Inject(target, provider));

                Assert.That(ex.DependantType, Is.EqualTo(typeof(HasInjectField)));
                Assert.That(ex.ServiceType, Is.EqualTo(typeof(IMissing)));
                Assert.That(ex.MemberName, Does.Contain("dependency"));
                Assert.That(ex.Message, Does.Contain("TheGameObject"), "must name the failing GameObject for MonoBehaviours");
                Assert.That(ex.InnerException, Is.InstanceOf<ServiceNotRegisteredException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Given_TwoChildrenBothMissingADependency_When_InjectRecursively_Then_AggregatesBothFailuresInsteadOfStoppingAtTheFirst()
        {
            var root = new UnityEngine.GameObject("Root");
            try
            {
                var rootBehaviour = root.AddComponent<Marker>();

                var childA = new UnityEngine.GameObject("ChildA");
                childA.transform.SetParent(root.transform);
                childA.AddComponent<HasInjectField>();

                var childB = new UnityEngine.GameObject("ChildB");
                childB.transform.SetParent(root.transform);
                childB.AddComponent<HasInjectField>();

                var provider = new ServiceProvider(new ServiceCollection(), name: "Fallback");

                var ex = Assert.Throws<System.AggregateException>(
                    () => ServiceInjector.InjectRecursively(rootBehaviour, provider));

                Assert.That(ex.InnerExceptions.Count, Is.EqualTo(2),
                    "a scene with two missing registrations should cost one play-mode entry to diagnose, not two");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        class Marker : UnityEngine.MonoBehaviour { }
    }
}
