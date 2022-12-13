namespace System
{
    public static class IServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return (T)serviceProvider.GetService(typeof(T));
        }
    }
}
