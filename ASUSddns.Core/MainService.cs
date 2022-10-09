namespace ASUSddns.Core
{
    public class MainService
    {
        public async Task MainMethod(MainServiceArgs args, CancellationToken _cancellation = default)
        {

        BEGIN:
            try
            {
                await UnsafeMainMethod(args, args.Options, _cancellation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                args.OnCallback?.Invoke(new AppLog
                {
                    MacAddress = args.Options.MacAddress,
                    Subdomain = args.Options.Subdomain,
                    WpsCode = args.Options.WpsCode,
                    Error = ex.Message,
                });
            }

            if (args.Repeat && !_cancellation.IsCancellationRequested)
            {
                await Task.Delay(args.Options.IntervalInMilliseconds, _cancellation).ConfigureAwait(false);
                goto BEGIN;
            }
        }

        public async Task UnsafeMainMethod(MainServiceArgs args, MainServiceOptions options, CancellationToken _cancellation = default)
        {            
            using var ctx = new LogDbContext();
            var lastSuccessfulUpdate = await ctx.Init(args.SqliteDirectory, options.LoggingTimeInHours).ConfigureAwait(false);            

            var log = new AppLog
            {
                MacAddress = options.MacAddress,
                Subdomain = options.Subdomain,
                WpsCode = options.WpsCode,
            };
            
            ctx.Logs.Add(log);
            await ctx.SaveChangesAsync().ConfigureAwait(false);

            string host = $"{options.Subdomain}.asuscomm.com";
            string user = options.MacAddress ?? string.Empty;
            string key = options.WpsCode ?? string.Empty;

            log.WanIp = (await ApiClient.GetWanIp().ConfigureAwait(false))?.ToString();

            if(log.WanIp == null)
            {
                log.WanIpStatus = WanIpStatus.Unavailable;
            }
            else if (log.WanIp != lastSuccessfulUpdate?.WanIp) // first time or ip Changed
            {
                var resolvedHostIps = await ApiClient.GetHostIps(host).ConfigureAwait(false); 

                if (resolvedHostIps.Contains(log.WanIp)) // first time
                {
                    log.WanIpStatus = WanIpStatus.Skipped; // already on host but not in logs
                }
                else // ip Changed
                {
                    log.WanIpStatus = WanIpStatus.Changed;
                }
            }

            await ctx.SaveChangesAsync().ConfigureAwait(false);
            args.OnCallback?.Invoke(log);

            if (log.WanIpStatus == WanIpStatus.Changed)
            {
                log.UpdateStatus = await ApiClient.Execute("update", user, key, host, log.WanIp!).ConfigureAwait(false); 
                
                await ctx.SaveChangesAsync().ConfigureAwait(false);
                args.OnCallback?.Invoke(log);

                if (log.UpdateStatus == UpdateStatus.Success)
                {
                    await Task.Delay(options.DelayAfterDdnsUpdateInMilliseconds, _cancellation).ConfigureAwait(false);
                }
            }
            
            await ctx.EndLog(log).ConfigureAwait(false);
            args.OnCallback?.Invoke(log);
        }
        
    }
}