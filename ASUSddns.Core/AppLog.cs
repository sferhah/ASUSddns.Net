namespace ASUSddns.Core
{
    public class AppLog
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public string? MacAddress { get; set; }
        public string? Subdomain { get; set; }
        public string? WpsCode { get; set; }
        public WanIpStatus WanIpStatus { get; set; }
        public string? WanIp { get; set; }
        public UpdateStatus UpdateStatus { get; set; }
        public string? Error { get; set; }
        public LogState State { get; set; } = LogState.Pending;

        public override string ToString() => string.Join("\n", new string?[]
        {
            Id.ToString(),
            StartDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            EndDate?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            MacAddress + "+" + Subdomain + "+" + WpsCode,
            WanIpStatus.ToString(),
            WanIp,
            UpdateStatus.ToString(),
            State.ToString(),
            Error,
        }.Where(x => x != null));

    }

    public enum LogState
    {
        Pending,
        Done,
        Cancelled,
    }

    public enum WanIpStatus
    {
        None,
        Unavailable,
        Skipped,
        Changed,        
    }

    public enum UpdateStatus
    {
        None,        
        InvalidPassword,
        InvalidDomain,
        Success,
        Unavailable,
    }
}