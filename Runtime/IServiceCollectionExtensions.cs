namespace SimpleDependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceDescriptorHelperAs<T> Register<T>(this IServiceCollection serviceCollection) where T : class
        {
            return new ServiceDescriptorHelper<T>(serviceCollection);
        }
    }
}
