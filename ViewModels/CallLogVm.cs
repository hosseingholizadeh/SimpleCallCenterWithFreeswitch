using System;
using FreeswitchListenerServer.Helper;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreeswitchListenerServer.ViewModels
{
    internal class CallLogVm
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string CallUuid { get; set; }
        public string TelephoneNumber { get; set; }
        public string DialedNumber { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public byte[] Data { get; set; }
        public bool IsBridged { get; set; }
    }

    internal class CallLogIvrVm
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string CallUuid { get; set; }
        public Guid AppId { get; set; }
        public string Input { get; set; }
        public bool Success { get; set; }
    }

    internal class CallLogDetailVm
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string CallUuid { get; set; }
        public string BridgeId { get; set; }
        public string AgentNumber { get; set; }
        public string TransferedFrom { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string CallStart => Start.ToLocalTime().ToPersianDateTimeString();
        public string CallEnd => End.ToLocalTime().ToPersianDateTimeString();
        public string State { get; set; }
        public byte[] Data { get; set; }
    }
}
