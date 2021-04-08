using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.EntityFrameworkCore;

namespace YC.Azure.WebJobs.Extensions.EntityFrameworkCore
{
    internal class EntityFrameworkExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly INameResolver _nameResolver;
        private readonly IServiceProvider _serviceProvider;
        public EntityFrameworkExtensionConfigProvider(INameResolver nameResolver, IServiceProvider serviceProvider)
        {
            _nameResolver = nameResolver ?? throw new ArgumentNullException(nameof(nameResolver));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var binding = context.AddBindingRule<DbSetAttribute>();

            binding.BindToCollector(BuildFromDbAttribute);
        }

        private IAsyncCollector<object> BuildFromDbAttribute(DbSetAttribute arg)
        {
            var contextType = arg.ContextType;
            var dbContext = _serviceProvider.GetService(contextType) as DbContext;
            return new DbSetWriter<object>(dbContext);
        }
    }
}