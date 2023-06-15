using Newtonsoft.Json.Linq;

namespace bot.Models
{
    public class InitialSequentialCard
    {
        public Action action { get; set; }
        public string trigger { get; set; }
    }
    public class Action
    {
        public string type { get; set; }
        public string title { get; set; }
        public JObject data { get; set; }
        public string verb { get; set; }
    }
}
