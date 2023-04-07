using System;
using Cysharp.Threading.Tasks;

namespace SimpleDependencyInjection
{
    public interface IModuleAsync
    {
        UniTask Configure(IServiceCollection serviceCollection);

        UniTask Init(IServiceProvider serviceProvider);
    }
}
