using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Eleventh_Project
{
    public class ExchangeRateModels
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; }
        [JsonPropertyName("saleRateNB")]
        public double SaleRateNB { get; set; }
        [JsonPropertyName("purchaseRateNB")]
        public double PurchaseRateNB { get; set; }
        [JsonPropertyName("saleRate")]
        public double SaleRate { get; set; }
        [JsonPropertyName("purchaseRate")]
        public double PurchaseRate { get; set; }
    }
}
