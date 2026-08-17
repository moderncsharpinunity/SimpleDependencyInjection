using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace SimpleDependencyInjection
{
    /// <summary>Feature 5 (self-bootstrap) and Fix 2 (no double injection), 0.3.0.</summary>
    public class GameObjectServiceScopeTests
    {
        interface IMarker { }
        class Marker : IMarker { }

        class TestAppScope : AppServiceScope
        {
            protected override void Setup() { }
        }

        class TestSceneScope : GameObjectServiceScope
        {
            protected override void Setup()
                => serviceContext.AddModule(services => services.Register<IMarker>().AsSingleton().FromNew<Marker>());
        }

        class CountingInject : MonoBehaviour
        {
            public int InjectCount;
            [Inject] IMarker marker = null;
            [Inject] void OnInjected(IMarker m) => InjectCount++;
        }

        GameObject appRoot;

        [TearDown]
        public void TearDown()
        {
            if (appRoot != null) UnityEngine.Object.DestroyImmediate(appRoot);
        }

        static IEnumerator WaitUntilOrFail(System.Func<bool> condition, string failureMessage, int maxFrames = 300)
        {
            for (int i = 0; i < maxFrames; i++)
            {
                if (condition()) yield break;
                yield return null;
            }
            Assert.Fail(failureMessage);
        }

        [UnityTest]
        public IEnumerator Given_SceneRootScope_When_AppScopeIsReady_Then_ItSelfBootstrapsAndInjectsItsSubtree()
        {
            appRoot = new GameObject(nameof(Given_SceneRootScope_When_AppScopeIsReady_Then_ItSelfBootstrapsAndInjectsItsSubtree) + ".App");
            appRoot.AddComponent<TestAppScope>();
            yield return WaitUntilOrFail(() => AppServiceScope.Current != null, "AppServiceScope never became ready");

            var sceneRoot = new GameObject("SceneRoot");
            var scope = sceneRoot.AddComponent<TestSceneScope>();
            var childGo = new GameObject("Child");
            childGo.transform.SetParent(sceneRoot.transform, false);
            var counting = childGo.AddComponent<CountingInject>();

            try
            {
                yield return WaitUntilOrFail(() => scope.Injected, "scene-root scope never self-bootstrapped");

                Assert.That(scope.ServiceProvider, Is.Not.Null);
                Assert.That(scope.ServiceProvider.GetService<IMarker>(), Is.Not.Null,
                    "the app scope's registrations must NOT be visible where they shouldn't be, but the scope's " +
                    "own module's registration must resolve");
                Assert.That(counting.InjectCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneRoot);
            }
        }

        [UnityTest]
        public IEnumerator Given_AlreadySelfBootstrappedScope_When_InjectRecursivelyWalksItsSubtreeAgain_Then_ItIsANoOp()
        {
            appRoot = new GameObject(nameof(Given_AlreadySelfBootstrappedScope_When_InjectRecursivelyWalksItsSubtreeAgain_Then_ItIsANoOp) + ".App");
            appRoot.AddComponent<TestAppScope>();
            yield return WaitUntilOrFail(() => AppServiceScope.Current != null, "AppServiceScope never became ready");

            var sceneRoot = new GameObject("SceneRoot");
            var scope = sceneRoot.AddComponent<TestSceneScope>();
            var childGo = new GameObject("Child");
            childGo.transform.SetParent(sceneRoot.transform, false);
            var counting = childGo.AddComponent<CountingInject>();

            try
            {
                yield return WaitUntilOrFail(() => scope.Injected, "scene-root scope never self-bootstrapped");
                Assert.That(counting.InjectCount, Is.EqualTo(1));

                // Simulate an ancestor's InjectRecursively re-walking over this already-
                // claimed scope's subtree — must be a no-op, not a second injection.
                var otherProvider = new ServiceProvider(new ServiceCollection(), name: "SomeOtherScope");
                ServiceInjector.InjectRecursively(sceneRoot.AddComponent<PassThrough>(), otherProvider);

                Assert.That(counting.InjectCount, Is.EqualTo(1), "an already-Injected scope's subtree must not be re-injected");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneRoot);
            }
        }

        class PassThrough : MonoBehaviour { }
    }
}
