using System;

namespace TiuLib.Model
{
    public class EventLog
    {
        public string Id { get; set; }
        public DateTime TimeStamp{ get; set; }
        public string ClientId { get; set; }
        public string LogMessage { get; set; }
    }
}