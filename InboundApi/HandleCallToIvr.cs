using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtraabERP.Core.Helpers;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.Helper;
using NEventSocket;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.Api
{
    public class HandleCallToIvr
    {

        public static async Task HandleCallToAppIvr(string desNumber, OutboundSocket connection,
            vwComChannelShiftApp shiftApp, ComFreeswitchApp application)
        {
            var conUuid = connection.ChannelData.UUID;
            var fileId = application.VoiceFileId;

            if (fileId.HasGuidValue())
            {
                //Get sound file of the IVR (PromptAudioFile)
                var file = Enumerable.FirstOrDefault(ErpContainerDataHelper.MessageFileList, p => p.PubDataStoragePID == fileId);
                if (file != null)
                {
                    var soundFilePath = SoundPlayerHelper.SaveWavFile(file);
                    var playGetDigits = await connection.PlayGetDigits(conUuid, new PlayGetDigitsOptions()
                    {
                        MinDigits = 4,
                        MaxDigits = 8,
                        MaxTries = 3,
                        TimeoutMs = 4000,
                        TerminatorDigits = "#",
                        PromptAudioFile = soundFilePath,
                        BadInputAudioFile = "ivr/ivr-that_was_an_invalid_entry.wav",
                        DigitTimeoutMs = 2000,
                    });

                    if (playGetDigits.Success)
                    {
                        var enteredDigigts = playGetDigits.Digits;
                    }
                }
            }
        }

    }
}
