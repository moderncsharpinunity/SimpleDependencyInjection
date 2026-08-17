using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace SimpleDependencyInjection
{
    /// <summary>Fix 1 (0.3.0) — field and method injection share one base-to-derived
    /// traversal per class hierarchy, so a base class's [Inject] field resolves (it used
    /// to be silently skipped if private) and a base class's [Inject] method completes
    /// before a derived class's runs.</summary>
    public class ServiceInjectorTraversalTests
    {
        interface IMarker { }
        class Marker : IMarker { }

        static List<string> Log;

        class Level1Mono : UnityEngine.MonoBehaviour
        {
            [Inject] IMarker _fieldA = null;

            [Inject]
            void ConstructLevel1(IMarker m)
            {
                Assert.That(_fieldA, Is.Not.Null, "Level1's own field must already be set when Level1's own method runs.");
                Log.Add("Level1.field");
                Log.Add("Level1.method");
            }
        }

        class Level2Mono : Level1Mono
        {
            [Inject] IMarker _fieldB = null;

            [Inject]
            void ConstructLevel2(IMarker m)
            {
                Assert.That(_fieldB, Is.Not.Null);
                Log.Add("Level2.field");
                Log.Add("Level2.method");
            }
        }

        class Level3Mono : Level2Mono
        {
            [Inject] IMarker _fieldC = null;

            [Inject]
            void ConstructLevel3(IMarker m)
            {
                Assert.That(_fieldC, Is.Not.Null);
                Log.Add("Level3.field");
                Log.Add("Level3.method");
            }
        }

        [SetUp]
        public void SetUp() => Log = new List<string>();

        static ServiceProvider BuildProviderWithMarker()
        {
            var services = new ServiceCollection();
            services.Register<IMarker>().AsSingleton().FromNew<Marker>();
            return new ServiceProvider(services);
        }

        [Test]
        public void Given_ThreeLevelMonoBehaviourHierarchy_When_Injected_Then_EachLevelsFieldsAndMethodCompleteBeforeTheNextLevelStarts()
        {
            var provider = BuildProviderWithMarker();
            var go = new UnityEngine.GameObject(nameof(Given_ThreeLevelMonoBehaviourHierarchy_When_Injected_Then_EachLevelsFieldsAndMethodCompleteBeforeTheNextLevelStarts));
            try
            {
                var target = go.AddComponent<Level3Mono>();
                ServiceInjector.Inject(target, provider);

                Assert.That(Log, Is.EqualTo(new[]
                {
                    "Level1.field", "Level1.method",
                    "Level2.field", "Level2.method",
                    "Level3.field", "Level3.method",
                }), "A private [Inject] field on a base class must resolve (Fix 1), and a base " +
                    "level's field+method must fully complete before the derived level starts.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        abstract class BasePlain
        {
            [Inject] protected IMarker BaseField;
        }

        class DerivedPlain : BasePlain
        {
            [Inject] IMarker _derivedField = null;

            public bool BothFieldsWereSetBeforeConstructorRan;

            public DerivedPlain()
            {
                // FromNew<T> injects fields before invoking the constructor — base AND
                // derived fields must both already be set here (Fix 1 fixes the base one).
                BothFieldsWereSetBeforeConstructorRan = BaseField != null && _derivedField != null;
            }
        }

        [Test]
        public void Given_PlainClassWithPrivateBaseField_When_ResolvedViaFromNew_Then_BaseFieldIsSetBeforeConstructorRuns()
        {
            var services = new ServiceCollection();
            services.Register<IMarker>().AsSingleton().FromNew<Marker>();
            services.Register<DerivedPlain>().AsTransient().FromSame();
            var provider = new ServiceProvider(services);

            var instance = provider.GetService<DerivedPlain>();

            Assert.That(instance.BothFieldsWereSetBeforeConstructorRan, Is.True);
        }

        class NoPublicConstructor
        {
            NoPublicConstructor() { }
            public static NoPublicConstructor Create() => new NoPublicConstructor();
        }

        [Test]
        public void Given_TypeWithNoPublicConstructor_When_Injected_Then_ThrowsWithTypeName()
        {
            var provider = new ServiceProvider(new ServiceCollection());

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ServiceInjector.Inject(NoPublicConstructor.Create(), provider));

            Assert.That(ex.Message, Does.Contain(nameof(NoPublicConstructor)));
        }

        class TwoInjectMethods : UnityEngine.MonoBehaviour
        {
            [Inject] void First(IMarker m) { }
            [Inject] void Second(IMarker m) { }
        }

        [Test]
        public void Given_MonoBehaviourWithTwoInjectMethodsOnTheSameClass_When_Injected_Then_Throws()
        {
            var go = new UnityEngine.GameObject(nameof(Given_MonoBehaviourWithTwoInjectMethodsOnTheSameClass_When_Injected_Then_Throws));
            try
            {
                var target = go.AddComponent<TwoInjectMethods>();
                var provider = BuildProviderWithMarker();

                Assert.Throws<InvalidOperationException>(() => ServiceInjector.Inject(target, provider));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
