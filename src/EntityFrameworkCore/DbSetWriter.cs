using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;

namespace YC.Azure.WebJobs.Extensions.EntityFrameworkCore
{
    internal class DbSetWriter<T> : IAsyncCollector<T>
    {
        private readonly DbContext _context;

        public DbSetWriter(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            _context.Entry(item).State = EntityState.Added;
            return Task.CompletedTask;
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