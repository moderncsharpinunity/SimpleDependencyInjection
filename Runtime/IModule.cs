using System;

namespace SimpleDependencyInjection
{
    public interface IModule
    {
        void Configure(IServiceCollection serviceCollection);

        void Init(IServiceProvider serviceProvider);
    }
}
