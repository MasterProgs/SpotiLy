using Newtonsoft.Json;

namespace SpotiLy.Api
{
    public class AccountApi
    {
        public int status { get; set; }
        public string country { get; set; }
        [JsonProperty("dmca-radio")]
        public bool DmcaRadio { get; set; }
        [JsonProperty("shuffle-restricted")]
        public bool ShuffleRestricted { get; set; }
        public string username { get; set; }
        public bool can_accept_licenses_in_one_step { get; set; }
        public bool requires_marketing_opt_in { get; set; }
        public bool requires_marketing_opt_in_text { get; set; }
        public int minimum_age { get; set; }
        public string country_group { get; set; }
        public bool specific_licenses { get; set; }
        public bool pretick_eula { get; set; }
        public bool show_collect_personal_info { get; set; }
        public bool use_all_genders { get; set; }
        public int date_endianness { get; set; }
        public bool is_country_launched { get; set; }
        public string login_token { get; set; }
    }
}
