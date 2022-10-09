using System.Net.Http.Headers;
using System.Net;
using DnsClient;
using System.Security.Cryptography;
using System.Text;

namespace ASUSddns.Core
{
    public class ApiClient
    {
        public static async Task<IEnumerable<string>> GetHostIps(string host)
        {
            IPAddress? ns1AsusIp;

            try
            {
                ns1AsusIp = (await new LookupClient().GetHostEntryAsync("ns1.asuscomm.com").ConfigureAwait(false)).AddressList.FirstOrDefault();
            }
            catch
            {
                ns1AsusIp = null;
            }

            try
            {
                return (await (ns1AsusIp == null ? new LookupClient() : new LookupClient(ns1AsusIp)).GetHostEntryAsync(host).ConfigureAwait(false))
                .AddressList.Select(x => x.ToString());
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        public static async Task<IPAddress?> GetWanIp()
        {
            try
            {
                
                return IPAddress.Parse(await new HttpClient().GetStringAsync("http://api.ipify.org/").ConfigureAwait(false));
            }
            catch
            {
                return null;
            }

        }

        public static async Task<UpdateStatus> Execute(string action, string user, string key, string host, string wanIp)
        {
            try
            {
                var password = ComputePassword(host, wanIp, key);
                user = StripDotsAndColons(user);

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.Clear();
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ez-update-3.0.11b5 unknown [] (by Angus Mackay)");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}")));
                var response = await httpClient.GetAsync($"http://ns1.asuscomm.com/ddns/{action}.jsp?hostname={host}&myip={wanIp}").ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return UpdateStatus.Success;
                }
                else if ((int)response.StatusCode == 298)
                {
                    return UpdateStatus.InvalidDomain;
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return UpdateStatus.InvalidPassword;
                }
                else
                {
                    return UpdateStatus.Unavailable;
                }
            }
            catch
            {
                return UpdateStatus.Unavailable;
            }
        }

        static string ComputePassword(string host, string wanIP, string key)
        {
            var stripped_host = StripDotsAndColons(host);
            var stripped_wanIP = StripDotsAndColons(wanIP);
            return HashWithHMACMD5(stripped_host + stripped_wanIP, key);
        }

        static string StripDotsAndColons(string input) => input.Replace(".", "").Replace(":", "");

        static string HashWithHMACMD5(string input, string key)
        {
            using var algo = new HMACMD5(Encoding.UTF8.GetBytes(key));
            return BitConverter.ToString(algo.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "");
        }


    }
}
