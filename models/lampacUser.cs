using System.Text.Json.Serialization;

namespace QRAuth.Models
{
    /// <summary>Read-only shape of the shared users.json record. The bot no longer creates
    /// or edits these — access is managed by hand, this is just for GetByTgId lookups.</summary>
    public class LampacUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("tg_id")]
        public long TgId { get; set; } = 0;

        [JsonPropertyName("group")]
        public int Group { get; set; } = 1;

        [JsonPropertyName("expires")]
        public string Expires { get; set; } = "";

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = "";

        [JsonPropertyName("params")]
        public LampacUserParams Params { get; set; } = new();
    }

    public class LampacUserParams
    {
        [JsonPropertyName("adult")]
        public bool Adult { get; set; } = false;

        [JsonPropertyName("admin")]
        public bool Admin { get; set; } = false;
    }
}
