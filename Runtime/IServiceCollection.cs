namespace SimpleDependencyInjection
{
    public interface IServiceCollection
    {
        void Add(ServiceDescriptor dependency);
    }
}