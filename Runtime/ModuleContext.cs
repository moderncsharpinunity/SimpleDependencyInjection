using System;
using System.Threading;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// Passed to every <see cref="IModule.Configure"/>/<see cref="IModule.Init"/> call so
    /// a module can report its own sub-progress and observe cancellation (Feature 6,
    /// 0.3.0), without needing a reference back to the <see cref="ServiceContext"/> that's
    /// building it.
    /// </summary>
    public readonly struct ModuleContext
    {
        public ModuleContext(IProgress<float> progress, CancellationToken cancellationToken)
        {
            Progress = progress;
            CancellationToken = cancellationToken;
        }

        /// <summary>Report 0..1 for whatever this module's current phase is doing. May be
        /// a no-op sink — always safe to call, never null.</summary>
        public IProgress<float> Progress { get; }

        public CancellationToken CancellationToken { get; }
    }
}
