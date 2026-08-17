using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// A dependency injection scope tied to a GameObject's lifetime — the intended use is
    /// a scene's root "Scene Context" object, so scene-lifetime services live and die with
    /// it, parented to <see cref="AppServiceScope.Current"/> so they can still see
    /// everything the app registered.
    ///
    /// 0.3.0 (Feature 5) makes this self-bootstrapping: subclass it, override
    /// <see cref="Setup"/> to register the scene's modules, and it builds and injects
    /// itself in its own <see cref="Awake"/> — no ancestor's
    /// <see cref="ServiceInjector.InjectRecursively"/> call is needed to discover it,
    /// which matters because a scene-root object is never a descendant of the
    /// <c>DontDestroyOnLoad</c> app scope. The pre-existing passive path (an ancestor
    /// building an as-yet-unbuilt scope it finds along the way) still works for a scope
    /// that genuinely is nested under something else's hierarchy.
    /// </summary>
    public class GameObjectServiceScope : MonoBehaviour
    {
        protected readonly ServiceContext serviceContext = new ServiceContext();

        readonly CancellationTokenSource cts = new CancellationTokenSource();

        public IServiceProvider ServiceProvider => serviceContext.ServiceProvider;

        /// <summary>
        /// True once this scope has claimed responsibility for building and injecting its
        /// own subtree — set as the very first thing whichever path gets there first does,
        /// specifically so the OTHER path can tell (by checking this) that it must not
        /// also do the work (0.3.0, Fix 2). Set synchronously, before any <c>await</c>, so
        /// there is no window where two callers both see <c>false</c>.
        /// </summary>
        public bool Injected { get; private set; }

        public void Build(IServiceProvider parentServiceProvider = null)
        {
            serviceContext.Name = GetType().Name;
            BuildAsync(parentServiceProvider, cts.Token).Forget();
        }

        public UniTask BuildAsync(IServiceProvider parentServiceProvider, CancellationToken ct)
        {
            serviceContext.Name = GetType().Name;
            return serviceContext.Build(parentServiceProvider, ct: ct);
        }

        /// <summary>Subclasses register this scene's modules here. Always called from
        /// <see cref="Awake"/>, regardless of which path (self-bootstrap or an ancestor's
        /// passive discovery) ends up actually building this scope's context — module
        /// registration has to happen before EITHER can call <see cref="ServiceContext.Build"/>.</summary>
        protected virtual void Setup() { }

        protected virtual void Awake()
        {
            Setup();
            BootstrapAsync().Forget();
        }

        async UniTaskVoid BootstrapAsync()
        {
            if (Injected) return; // an ancestor's InjectRecursively already claimed us
            Injected = true;

            var app = AppServiceScope.Current
                ?? throw new ServiceScopeNotReadyException(this,
                    $"'{GetType().FullName}' on GameObject '{name}' woke up before " +
                    $"{nameof(AppServiceScope)}.{nameof(AppServiceScope.Current)} was set. A scene carrying a " +
                    $"{nameof(GameObjectServiceScope)} must load after the app scope has finished building — " +
                    "see the boot-order rule in CLAUDE.md.");

            // The scope's own [Inject] members belong to whatever contains it, not to
            // the context it's about to build.
            ServiceInjector.Inject(this, ServiceInjector.ResolveEnclosingProvider(this, app.ServiceProvider));

            serviceContext.Name = GetType().Name;
            gameObject.SetActive(false); // defers descendants' Awake until we're done, same idiom as AppServiceScope

            try
            {
                await serviceContext.Build(app.ServiceProvider, ct: cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // torn down mid-build — OnDestroy already ran
            }

            ServiceInjector.InjectRecursively(this, ServiceProvider);
            gameObject.SetActive(true);
        }

        protected virtual void OnDestroy()
        {
            cts.Cancel();
            cts.Dispose();
            serviceContext.Dispose();
        }
    }
}
