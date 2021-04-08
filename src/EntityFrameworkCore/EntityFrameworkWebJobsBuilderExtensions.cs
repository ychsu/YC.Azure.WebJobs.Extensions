using Microsoft.Azure.WebJobs;

namespace YC.Azure.WebJobs.Extensions.EntityFrameworkCore
{
    public static class EntityFrameworkWebJobsBuilderExtensions 
    {
        public static IWebJobsBuilder AddEntityFrameworkCore(this IWebJobsBuilder builder)
        {
            builder.AddExtension<EntityFrameworkExtensionConfigProvider>();
			
            return builder;
        }
    }
}