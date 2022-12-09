using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDependencyInjection
{
    public interface IModule
    {
        void Configure(IServiceCollection serviceCollection);

        void Init(IServiceProvider serviceProvider);
    }
}
