using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace SimpleDependencyInjection
{
    /// <summary>The <see cref="AppServiceScope.OnBuilt"/> hook — added so a subclass (e.g.
    /// a splash-screen entry point) can act once the container exists and this object's own
    /// subtree is injected, without polling <see cref="AppServiceScope.Current"/> itself.</summary>
    public class AppServiceScopeTests
    {
        class RecordingAppScope : AppServiceScope
        {
            public bool OnBuiltCalled;
            public bool CurrentWasSetWhenOnBuiltRan;

            protected override void Setup() { }

            protected override UniTask OnBuilt()
            {
                OnBuiltCalled = true;
                CurrentWasSetWhenOnBuiltRan = Current == this;
                return UniTask.CompletedTask;
            }
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
        public IEnumerator Given_AnAppScope_When_ItFinishesBuilding_Then_OnBuiltRunsExactlyOnceAfterCurrentIsSet()
        {
            appRoot = new GameObject(nameof(Given_AnAppScope_When_ItFinishesBuilding_Then_OnBuiltRunsExactlyOnceAfterCurrentIsSet) + ".App");
            var scope = appRoot.AddComponent<RecordingAppScope>();

            yield return WaitUntilOrFail(() => scope.OnBuiltCalled, "OnBuilt was never called");

            Assert.That(scope.CurrentWasSetWhenOnBuiltRan, Is.True,
                "OnBuilt must run after Current is set, so it can safely resolve services");
        }

        class RecordingSceneScope : GameObjectServiceScope
        {
            public bool OnBuiltCalled;
            public bool ServiceProviderWasSetWhenOnBuiltRan;

            protected override UniTask OnBuilt()
            {
                OnBuiltCalled = true;
                ServiceProviderWasSetWhenOnBuiltRan = ServiceProvider != null;
                return UniTask.CompletedTask;
            }
        }

        [UnityTest]
        public IEnumerator Given_ASceneScope_When_ItFinishesBuilding_Then_OnBuiltRunsExactlyOnceWithServiceProviderReady()
        {
            appRoot = new GameObject(nameof(Given_ASceneScope_When_ItFinishesBuilding_Then_OnBuiltRunsExactlyOnceWithServiceProviderReady) + ".App");
            appRoot.AddComponent<RecordingAppScope>();
            yield return WaitUntilOrFail(() => AppServiceScope.Current != null, "AppServiceScope never became ready");

            var sceneRoot = new GameObject("SceneRoot");
            try
            {
                var scope = sceneRoot.AddComponent<RecordingSceneScope>();

                yield return WaitUntilOrFail(() => scope.OnBuiltCalled, "OnBuilt was never called");

                Assert.That(scope.ServiceProviderWasSetWhenOnBuiltRan, Is.True,
                    "OnBuilt must run after ServiceProvider is set, so it can safely resolve services");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sceneRoot);
            }
        }
    }
}
