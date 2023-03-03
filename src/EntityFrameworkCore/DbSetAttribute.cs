using System;
using Microsoft.Azure.WebJobs.Description;

namespace YC.Azure.WebJobs.Extensions.EntityFrameworkCore
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public class DbSetAttribute : Attribute
    {
        public DbSetAttribute(Type contextType)
        {
            ContextType = contextType;
        }

        public Type ContextType { get; set; }
    }
}