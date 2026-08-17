using Cysharp.Threading.Tasks;

namespace SimpleDependencyInjection
{
    /// <summary>
    /// A unit of registration + initialisation for a <see cref="ServiceContext"/>.
    ///
    /// 0.3.0 merges what was <c>IModule</c> (synchronous) and <c>IModuleAsync</c> into
    /// this single async interface — the container has exactly one build path now, so
    /// there is exactly one module shape (Change 5). Async work (an Addressables load, a
    /// save-file read) belongs in <see cref="Init"/>, since <see cref="Configure"/> runs
    /// before the <see cref="System.IServiceProvider"/> exists to do anything useful with.
    /// </summary>
    public interface IModule
    {
        UniTask Configure(IServiceCollection serviceCollection, ModuleContext context);

        UniTask Init(System.IServiceProvider serviceProvider, ModuleContext context);
    }
}
