using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptionEdge.API.FlatTrade.Samples
{
    public class Settings
    {
        public string ApiKey { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string TOTPSecret { get; set; }
        public string AccountId { get; set; }
        public string ApiSecret { get; set; }
        public string PAN { get; set; }
        public string DOB { get; set; }
        public bool EnableLogging { get; set; } = true;
    }
}
