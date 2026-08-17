using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace SimpleDependencyInjection
{
    /// <summary>Fix 4 (0.3.0) — ServiceProvider disposes the Singleton/Scoped instances IT
    /// created, in reverse creation order, and never touches a parent's or a child's.</summary>
    public class ServiceProviderDisposalTests
    {
        class Tracked : IDisposable
        {
            readonly List<string> log;
            readonly string name;
            public bool Disposed { get; private set; }

            public Tracked(List<string> log, string name)
            {
                this.log = log;
                this.name = name;
            }

            public void Dispose()
            {
                Disposed = true;
                log.Add(name);
            }
        }

        [Test]
        public void Given_TwoSingletonDisposables_When_ProviderDisposed_Then_BothDisposedInReverseCreationOrder()
        {
            var log = new List<string>();
            // AsSingleton keys by concrete registered type; use two distinct types to get
            // two independent singletons in one provider.
            var provider = BuildTwoSingletons(log);

            provider.GetService<TrackedA>();
            provider.GetService<TrackedB>();
            provider.Dispose();

            Assert.That(log, Is.EqualTo(new[] { "B", "A" }), "must dispose in reverse of creation order");
        }

        class TrackedA : Tracked { public TrackedA(List<string> log) : base(log, "A") { } }
        class TrackedB : Tracked { public TrackedB(List<string> log) : base(log, "B") { } }

        static ServiceProvider BuildTwoSingletons(List<string> log)
        {
            var services = new ServiceCollection();
            services.Register<TrackedA>().AsSingleton().From<TrackedA>(sp => new TrackedA(log));
            services.Register<TrackedB>().AsSingleton().From<TrackedB>(sp => new TrackedB(log));
            return new ServiceProvider(services);
        }

        [Test]
        public void Given_TransientDisposable_When_ProviderDisposed_Then_NotDisposed()
        {
            var log = new List<string>();
            var services = new ServiceCollection();
            services.Register<Tracked>().AsTransient().From<Tracked>(sp => new Tracked(log, "transient"));
            var provider = new ServiceProvider(services);

            var instance = provider.GetService<Tracked>();
            provider.Dispose();

            Assert.That(instance.Disposed, Is.False, "transients are caller-owned, not tracked for disposal");
        }

        [Test]
        public void Given_NestedProviders_When_ParentDisposed_Then_ChildsOwnInstancesAreUntouched()
        {
            var log = new List<string>();
            var services = new ServiceCollection();
            services.Register<Tracked>().AsScoped().From<Tracked>(sp => new Tracked(log, "parent-scoped"));
            var parent = new ServiceProvider(services);

            var childServices = new ServiceCollection();
            childServices.Register<Tracked>().AsScoped().From<Tracked>(sp => new Tracked(log, "child-scoped"));
            var child = new ServiceProvider(childServices, parent);

            var childInstance = child.GetService<Tracked>();
            parent.Dispose();

            Assert.That(childInstance.Disposed, Is.False, "a parent's Dispose must not reach into a child provider");
            Assert.That(log, Is.Empty, "the parent never created a Tracked itself, so it has nothing of its own to dispose");
        }
    }
}
