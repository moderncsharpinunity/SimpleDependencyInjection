using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// Builds one <see cref="ServiceProvider"/> from a list of <see cref="IModule"/>s:
    /// every module's <see cref="IModule.Configure"/> runs first, then the provider is
    /// constructed, then every module's <see cref="IModule.Init"/> runs — so a module can
    /// resolve what an earlier module registered, but never see a later module's
    /// registrations during its own Configure.
    ///
    /// 0.3.0 (Change 5): this is now the ONLY context — the old synchronous
    /// <c>ServiceContext</c> and <c>IModule</c> are gone, and what was
    /// <c>ServiceContextAsync</c>/<c>IModuleAsync</c> lost the "Async" suffix. One build
    /// path, one module shape.
    /// </summary>
    public class ServiceContext : IDisposable
    {
        readonly List<IModule> modules = new List<IModule>();

        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>A name for this context's provider — shows up in
        /// <see cref="ServiceNotRegisteredException"/>'s scope chain. Defaults to the
        /// concrete scope type name by whichever <see cref="AppServiceScope"/> or
        /// <see cref="GameObjectServiceScope"/> owns this context.</summary>
        public string Name { get; set; }

        public async UniTask Build(IServiceProvider parentServiceProvider = null,
            IProgress<BuildProgress> progress = null, CancellationToken ct = default)
        {
            var serviceCollection = new ServiceCollection();

            for (int i = 0; i < modules.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var moduleProgress = ModuleProgress(i, modules.Count, "Configure", progress);
                await modules[i].Configure(serviceCollection, new ModuleContext(moduleProgress, ct));
            }

            ct.ThrowIfCancellationRequested();
            ServiceProvider = new ServiceProvider(serviceCollection, parentServiceProvider, Name);

            for (int i = 0; i < modules.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var moduleProgress = ModuleProgress(modules.Count + i, modules.Count * 2, "Init", progress);
                await modules[i].Init(ServiceProvider, new ModuleContext(moduleProgress, ct));
            }

            progress?.Report(new BuildProgress(1f, null, "Init", 1f));
        }

        static IProgress<float> ModuleProgress(int stepIndex, int totalSteps, string phase, IProgress<BuildProgress> overall)
        {
            if (overall == null) return NullProgress.Instance;
            return new Progress<float>(fraction =>
            {
                float overallFraction = totalSteps == 0 ? 1f : (stepIndex + fraction) / (totalSteps * 2f);
                overall.Report(new BuildProgress(overallFraction, null, phase, fraction));
            });
        }

        public void AddModule(IModule module)
        {
            Assert.IsNotNull(module, "Modules cannot be null");
            modules.Add(module);
        }

        /// <summary>Convenience for a module that only registers things, synchronously —
        /// the common case, and one that shouldn't need `async`/`UniTask` ceremony.</summary>
        public void AddModule(Action<IServiceCollection> configure, Action<IServiceProvider> init = null)
        {
            Assert.IsNotNull(configure, "Module's configure cannot be null");
            modules.Add(new SimpleModule(configure, init));
        }

        public void Dispose() => (ServiceProvider as IDisposable)?.Dispose();

        sealed class NullProgress : IProgress<float>
        {
            public static readonly NullProgress Instance = new NullProgress();
            public void Report(float value) { }
        }

        sealed class SimpleModule : IModule
        {
            readonly Action<IServiceCollection> configure;
            readonly Action<IServiceProvider> init;

            public SimpleModule(Action<IServiceCollection> configure, Action<IServiceProvider> init)
            {
                this.configure = configure;
                this.init = init;
            }

            public UniTask Configure(IServiceCollection serviceCollection, ModuleContext context)
            {
                configure?.Invoke(serviceCollection);
                return UniTask.CompletedTask;
            }

            public UniTask Init(IServiceProvider serviceProvider, ModuleContext context)
            {
                init?.Invoke(serviceProvider);
                return UniTask.CompletedTask;
            }
        }
    }
}
