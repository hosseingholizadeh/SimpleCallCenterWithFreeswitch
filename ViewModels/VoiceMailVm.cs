using System;

namespace FreeswitchListenerServer.ViewModels
{
    public class VoiceMailVm
    {
        public Guid Id { get; set; }
        public string VoipNumber { get; set; }
        public string FileName { get; set; }
        public int Max { get; set; }
        public string StopKey { get; set; }
        public string MaskKey { get; set; }
        public string UnMaskKey { get; set; }
        public short ExecuteTypeId { get; set; }
        public bool IsEnabled { get; set; }
    }
}
