using System;
#if UNITASK
using Task = Cysharp.Threading.Tasks.UniTask;
#else
using Task = System.Threading.Tasks.Task;
#endif

namespace SimpleDependencyInjection
{
    public interface IModuleAsync
    {
        Task Configure(IServiceCollection serviceCollection);

        Task Init(IServiceProvider serviceProvider);
    }
}
