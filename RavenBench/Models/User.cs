using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RavenBench.Models
{
    [Flags]
    public enum UserLevel
    {
        User = 0x0,
        MonexAdmin = 0x100,
        InsightSiteAdmin = 0x1000
    }

    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public bool IsUser { get; set; }
        public bool HasMarginAccount { get; set; }
        public string Subdomain { get; set; }
        public string RequestId { get; set; }
        public string SessionId { get; set; }
        public int UserLevel { get; set; }
        public IList<Marketboard> Marketboards { get; set; }
        public int NextMarketboardId { get; set; }
        public HashSet<string> UsageMonths { get; set; }


        [Raven.Imports.Newtonsoft.Json.JsonIgnore]
        public IEnumerable<Marketboard> UserMarketboards
        {
            get
            {
                return this.Marketboards.Where(m => m.Type == MarketboardType.User);
            }
        }


        public User()
        {
            this.Marketboards = new List<Marketboard>();
            this.UsageMonths = new HashSet<string>();
        }

        public override string ToString()
        {
            return String.Format("<User {0} ({1})>", this.Name, this.Key);
        }
    }

}
