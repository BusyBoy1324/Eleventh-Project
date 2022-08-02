using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Eleventh_Project
{
    public class PrivatBankModels
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("exchangeRate")]
        public List<ExchangeRateModels> ExchangeRate { get; set; }

    }
}
