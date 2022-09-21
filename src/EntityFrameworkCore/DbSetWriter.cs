using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;

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

        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            await _context.AddAsync(item, cancellationToken);
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