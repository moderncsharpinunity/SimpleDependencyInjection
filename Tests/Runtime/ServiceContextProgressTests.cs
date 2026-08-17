using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace SimpleDependencyInjection
{
    /// <summary>Feature 6 (0.3.0) — Build reports progress across every module and both
    /// phases, ending at exactly 1, so a loading screen can show real progress instead of
    /// a timer.</summary>
    public class ServiceContextProgressTests
    {
        class RegisterOnlyModule : IModule
        {
            public UniTask Configure(IServiceCollection serviceCollection, ModuleContext context) => UniTask.CompletedTask;
            public UniTask Init(System.IServiceProvider serviceProvider, ModuleContext context) => UniTask.CompletedTask;
        }

        [UnityTest]
        public IEnumerator Given_TwoSyncModules_When_Built_Then_ProgressIsMonotonicAndEndsAtOne() => UniTask.ToCoroutine(async () =>
        {
            var context = new ServiceContext();
            context.AddModule(new RegisterOnlyModule());
            context.AddModule(new RegisterOnlyModule());

            var reports = new List<float>();
            var progress = new System.Progress<BuildProgress>(p => reports.Add(p.Overall));

            await context.Build(null, progress);

            Assert.That(reports, Is.Not.Empty);
            for (int i = 1; i < reports.Count; i++)
                Assert.That(reports[i], Is.GreaterThanOrEqualTo(reports[i - 1]), "progress must never go backwards");
            Assert.That(reports[reports.Count - 1], Is.EqualTo(1f), "the last report must be exactly done");
        });

        class CancellingModule : IModule
        {
            public UniTask Configure(IServiceCollection serviceCollection, ModuleContext context)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
            public UniTask Init(System.IServiceProvider serviceProvider, ModuleContext context) => UniTask.CompletedTask;
        }

        [UnityTest]
        public IEnumerator Given_CancelledToken_When_Built_Then_ThrowsOperationCanceled() => UniTask.ToCoroutine(async () =>
        {
            var context = new ServiceContext();
            context.AddModule(new CancellingModule());

            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();

            bool threw = false;
            try { await context.Build(null, null, cts.Token); }
            catch (System.OperationCanceledException) { threw = true; }

            Assert.That(threw, Is.True);
        });
    }
}
