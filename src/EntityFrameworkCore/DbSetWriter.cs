using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;

namespace YC.Azure.WebJobs.Extensions.EntityFrameworkCore
{
    internal class DbSetWriter<T> : IAsyncCollector<T>
        where T : class
    {
        private readonly DbContext _context;

        public DbSetWriter(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            var set = Set(item.GetType());
            var method = set.GetType().GetMethod("Add") ?? throw new MissingMethodException();
            method.Invoke(set, new object[] {item});
            return Task.CompletedTask;
        }

        [SuppressMessage("ReSharper", "EF1001")]
        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private object Set(Type modelType)
        {
            return ((IDbSetCache) _context).GetOrAddSet(
                ((IDbContextDependencies) _context).SetSource, modelType);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}