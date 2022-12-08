namespace SimpleDependencyInjection
{
    public interface IServiceCollection
    {
        void Register(ServiceDescriptor service);
    }
}