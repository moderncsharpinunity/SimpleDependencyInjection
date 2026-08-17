using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// The app's composition root and entry point — one instance, in the first scene that
    /// loads, <c>DontDestroyOnLoad</c>. <see cref="DefaultExecutionOrderAttribute"/> of -1
    /// makes its <see cref="Awake"/> the first thing to run.
    ///
    /// There is deliberately no "ready" signal to await: nothing runs before this does
    /// (nothing else should carry behaviour in the same scene — see the boot-order rule
    /// this implies), so <see cref="Current"/> is simply null until it's genuinely usable,
    /// and non-null means fully built. <see cref="Current"/> is set as the LAST step of
    /// <see cref="Awake"/> — after <see cref="ServiceProvider"/> is populated and this
    /// scope's own subtree is injected — specifically so "Current is non-null" implies
    /// "safe to read ServiceProvider", with no separate readiness check needed anywhere.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public abstract class AppServiceScope : MonoBehaviour
    {
        protected readonly ServiceContext serviceContext = new ServiceContext();
        readonly Stack<IServiceProvider> serviceProviders = new Stack<IServiceProvider>();
        readonly CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>Non-null only once the app scope has fully finished building. The
        /// only mutable static state in this package — see <see cref="ResetCurrent"/> for
        /// why it needs one.</summary>
        public static AppServiceScope Current { get; private set; }

        /// <summary>The current provider — may be a <see cref="GlobalServiceScope"/>'s if
        /// one is pushed (see <see cref="PushServiceProvider"/>).</summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>Always this scope's own provider, never a pushed
        /// <see cref="GlobalServiceScope"/>'s — what a <see cref="GlobalServiceScope"/>
        /// parents itself to (Fix 9, 0.3.0), so activating a second global scope while one
        /// is already pushed doesn't chain onto the first one's provider by accident.</summary>
        public IServiceProvider RootServiceProvider { get; private set; }

        /// <summary>Fires as the build progresses — a loading screen in the same scene
        /// can drive a real meter off this instead of a timer (Feature 6, 0.3.0). Because
        /// <see cref="DefaultExecutionOrderAttribute"/> makes this scope's own
        /// <see cref="Awake"/> run first, a listener subscribing from its OWN default-order
        /// <c>Awake</c>/<c>OnEnable</c> will still catch every report — nothing observes
        /// this before this scope's Awake has started. <see cref="LastProgress"/> exists
        /// for a listener that subscribes even later and needs to catch up.</summary>
        public event Action<BuildProgress> Progress;

        public BuildProgress? LastProgress { get; private set; }

        // Domain reload can be disabled in Editor play mode, which would otherwise leave
        // Current pointing at a destroyed instance from a previous play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCurrent() => Current = null;

        async void Awake()
        {
            DontDestroyOnLoad(gameObject);

            serviceContext.Name = GetType().Name;
            serviceContext.AddModule((services) => services.Register<AppServiceScope>().AsSingleton().FromInstance(this));

            Setup();

            gameObject.SetActive(false);

            var progress = new Progress<BuildProgress>(p => { LastProgress = p; Progress?.Invoke(p); });

            try
            {
                await serviceContext.Build(null, progress, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // destroyed mid-build
            }

            ServiceProvider = serviceContext.ServiceProvider;
            RootServiceProvider = ServiceProvider;

            ServiceInjector.InjectRecursively(this, ServiceProvider);

            gameObject.SetActive(true);

            Current = this; // last — see the class doc for why

            await OnBuilt();
        }

        protected abstract void Setup();

        /// <summary>Called once, after the scope has fully finished building and
        /// <see cref="Current"/> is set — the hook a subclass overrides to do whatever
        /// should happen once the container exists and this object's own subtree is
        /// injected (e.g. navigate away from a splash/loading scene via an injected
        /// <c>ISceneRouter</c>). The default implementation does nothing.</summary>
        protected virtual UniTask OnBuilt() => UniTask.CompletedTask;

        public void PushServiceProvider(IServiceProvider serviceProvider)
        {
            serviceProviders.Push(ServiceProvider);
            ServiceProvider = serviceProvider;
        }

        public void PopServiceProvider()
        {
            ServiceProvider = serviceProviders.Pop();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            cts.Cancel();
            cts.Dispose();
            serviceContext.Dispose();
        }
    }
}
