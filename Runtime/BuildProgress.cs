namespace SimpleDependencyInjection
{
    /// <summary>
    /// What <see cref="ServiceContext.Build"/> reports as it works through its modules —
    /// enough for a loading screen to show real progress instead of a timer (Feature 6,
    /// 0.3.0). <see cref="Overall"/> is derived from module index and phase, refined by
    /// whatever the current module reports via its <see cref="ModuleContext.Progress"/>,
    /// so a module doing real async work (e.g. an Addressables download) can drive a
    /// smooth bar without <see cref="ServiceContext"/> knowing anything about it.
    /// </summary>
    public readonly struct BuildProgress
    {
        public BuildProgress(float overall, string moduleName, string phase, float moduleFraction)
        {
            Overall = overall;
            ModuleName = moduleName;
            Phase = phase;
            ModuleFraction = moduleFraction;
        }

        /// <summary>0..1 across every module and both phases.</summary>
        public float Overall { get; }

        /// <summary>The module currently running, by its concrete type name.</summary>
        public string ModuleName { get; }

        /// <summary><see cref="BuildPhase.Configure"/> or <see cref="BuildPhase.Init"/>.</summary>
        public string Phase { get; }

        /// <summary>What the current module last reported for its own phase, 0..1.</summary>
        public float ModuleFraction { get; }
    }
}
