namespace ASUSddns.Core
{
    public class MainServiceOptions
    {
        public virtual string? MacAddress { get; set; }
        public virtual string? Subdomain { get; set; }        
        public virtual string? WpsCode { get; set; }

        public virtual int IntervalInMilliseconds { get; set; } = 1000;        
        public virtual int DelayAfterDdnsUpdateInMilliseconds { get; set; } = 45000;        
        public virtual int LoggingTimeInHours { get; set; } = 24;
    }

    public class MainServiceArgs
    {   
        public string? SqliteDirectory { get; set; }
        public Action<AppLog>? OnCallback { get; set; }
        public bool Repeat { get; set; }
        public MainServiceOptions Options { get; set; } = new MainServiceOptions();
    }
}