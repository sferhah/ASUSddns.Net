using Microsoft.EntityFrameworkCore;

namespace ASUSddns.Core
{
    public class LogDbContext : DbContext
    {
        internal static string? path;
        public LogDbContext() { }
        public DbSet<AppLog> Logs => Set<AppLog>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string path = LogDbContext.path == null ? "logs.db3" : (LogDbContext.path + "/" + "logs.db3");
            optionsBuilder.UseSqlite($"Filename={path}");
        }
    }

    public static class LogDbContextExt
    {
        public static async Task<AppLog?> Init(this LogDbContext ctx, string? path, int maxHours)
        {
            LogDbContext.path = path;
            AppLog? lastSuccessfulWanIp = null;

            if (!await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false))
            {
                try
                {
                    lastSuccessfulWanIp = await ctx.Logs
                        .FirstOrDefaultAsync(x => x.WanIp != null && (x.UpdateStatus == UpdateStatus.Success || x.WanIpStatus == WanIpStatus.Skipped))
                        .ConfigureAwait(false);
                }
                catch // model has changed, recreate database
                {
                    await ctx.Database.EnsureDeletedAsync().ConfigureAwait(false);
                    await ctx.Database.EnsureCreatedAsync().ConfigureAwait(false);
                }
            }

            await ctx.DeleteLogsBefore(DateTime.UtcNow.AddHours(-maxHours)).ConfigureAwait(false);
            return lastSuccessfulWanIp;
        }

        public static async Task DeleteLogsBefore(this LogDbContext ctx, DateTime dateTime)
        {
            var logs = await ctx.Logs.Where(x => x.StartDate < dateTime).ToListAsync().ConfigureAwait(false);

            foreach (var log in logs)
            {
                ctx.Logs.Remove(log);
            }

            await ctx.SaveChangesAsync().ConfigureAwait(false);
        }

        public static async Task EndLog(this LogDbContext ctx, AppLog log, bool cancelled = false)
        {
            log.EndDate = DateTime.UtcNow;
            log.State = cancelled ? LogState.Cancelled : LogState.Done;
            await ctx.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}