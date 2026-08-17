using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// A swappable set of services layered on top of the app scope — for a mode the app
    /// can be in rather than something every scene needs (a level editor, a replay
    /// viewer). Not used for app-wide services; those belong in
    /// <see cref="AppServiceScope"/>'s own modules.
    ///
    /// 0.3.0 (Fix 9): <see cref="Activate"/> now builds with the app scope's
    /// <see cref="AppServiceScope.RootServiceProvider"/> as parent — previously it built
    /// with no parent at all, so nothing the app registered was resolvable from inside a
    /// global scope. Using the ROOT provider specifically (not
    /// <see cref="AppServiceScope.ServiceProvider"/>, which may itself be a previously
    /// pushed global scope's) is what stops activating a second global scope while one is
    /// already active from chaining onto the first instead of the app.
    /// </summary>
    public abstract class GlobalServiceScope : IDisposable
    {
        protected readonly ServiceContext serviceContext = new ServiceContext();
        readonly AppServiceScope appServiceScope;
        readonly CancellationTokenSource cts = new CancellationTokenSource();

        public IServiceProvider ServiceProvider => serviceContext.ServiceProvider;

        public bool IsActive { get; private set; }

        protected GlobalServiceScope(AppServiceScope appServiceScope)
        {
            this.appServiceScope = appServiceScope;
        }

        public async UniTask Activate(IProgress<BuildProgress> progress = null, CancellationToken ct = default)
        {
            if (IsActive)
            {
                throw new InvalidOperationException(
                    $"'{GetType().FullName}' is already active. Deactivate it before activating it again.");
            }

            if (ServiceProvider == null)
            {
                serviceContext.Name = GetType().Name;
                await serviceContext.Build(appServiceScope.RootServiceProvider, progress, ct);
            }

            appServiceScope.PushServiceProvider(ServiceProvider);
            IsActive = true;
        }

        public void Deactivate()
        {
            if (appServiceScope.ServiceProvider != ServiceProvider)
            {
                Debug.LogError($"Unable to deactivate '{GetType().FullName}' — it is not currently active.");
                return;
            }

            appServiceScope.PopServiceProvider();
            IsActive = false;

            // A deactivated mode's services should not outlive it — re-activating rebuilds.
            serviceContext.Dispose();
        }

        public void Dispose()
        {
            cts.Cancel();
            cts.Dispose();
            if (IsActive) Deactivate();
            else serviceContext.Dispose();
        }
    }
}
