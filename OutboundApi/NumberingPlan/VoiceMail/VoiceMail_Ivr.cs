using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeswitchListenerServer.deifintions;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.OutboundApi.NumberingPlan.VoiceMail
{
    public class VoiceMailIvr
    {
        public async Task PlayAndGetVoiceMailFileType(Channel channel)
        {
            var playGetDigitsResult = await channel.PlayGetDigits(
                new PlayGetDigitsOptions()
                {
                    MinDigits = 1,
                    MaxDigits = 1,
                    MaxTries = 3,
                    TimeoutMs = 5000,
                    DigitTimeoutMs = 3000,
                    ValidDigits = "12",
                    PromptAudioFile = "EtIvr/VoiceMailIvr.wav",
                    BadInputAudioFile = "EtIvr/InvalidEntry.wav"
                });

            if (playGetDigitsResult.Success)
            {
                var enteredDigits = playGetDigitsResult.Digits;

                var fileTypeId = (short)EnVoiceFileType.NotPlayed;
                //played VoiceMails
                if (enteredDigits == "2")
                {
                    fileTypeId = (short)EnVoiceFileType.Played;
                }

                await PlayAndGetDateFilterType(channel, fileTypeId);
            }
            else
            {
                await channel.Hangup();
            }

        }

        private async Task PlayAndGetDateFilterType(Channel channel, short fileTypeId)
        {
            var playGetDigitsResult = await channel.PlayGetDigits(
                new PlayGetDigitsOptions()
                {
                    MinDigits = 1,
                    MaxDigits = 1,
                    MaxTries = 3,
                    TimeoutMs = 5000,
                    DigitTimeoutMs = 5000,
                    ValidDigits = "12",
                    PromptAudioFile = "EtIvr/VoiceMailIvrDateFilter.wav",
                    BadInputAudioFile = "EtIvr/InvalidEntry.wav"
                });

            if (playGetDigitsResult.Success)
            {
                var enteredDigits = playGetDigitsResult.Digits;

                //By date time descending
                if (enteredDigits == "1")
                {
                    await VoiceMailPlayer.PlayFileList(channel, fileTypeId);
                }
                //By date filter
                else if (enteredDigits == "2")
                {
                    await FilterByDateTime(channel, fileTypeId);
                }
            }
            else
            {
                await channel.Hangup();
            }
        }

        private async Task FilterByDateTime(Channel channel, short fileTypeId)
        {
            await channel.Play("EtIvr/StartDate.wav");
            await channel.Play("EtIvr/EnterLikeThisDay&etc.wav");
            var dtmfList = new List<string>();
            var filterDateList = new List<List<string>>();
            channel.Events.Where(x => x.UUID == channel.UUID && (x.EventName == EventName.Dtmf))
                .Subscribe(
                    async (e) =>
                    {
                        if (filterDateList.Count == 2)
                            return;

                        var dtmf = e.Headers[HeaderNames.DtmfDigit];
                        dtmfList.Add(dtmf);
                        if (dtmf == "#" && dtmfList.Count > 0)
                        {
                            var dateList = new List<string>();
                            StringBuilder sb = new StringBuilder();
                            foreach (var entry in dtmfList)
                            {
                                if (entry == "*" || entry == "#")
                                {
                                    dateList.Add(sb.ToString());
                                    sb.Clear();
                                }
                                else
                                {
                                    sb.Append(entry);
                                }
                            }

                            dtmfList = new List<string>();
                            if (dateList.Count == 3)
                            {
                                if (filterDateList.Count == 0)
                                {
                                    filterDateList.Add(dateList);
                                    await channel.Play("EtIvr/EndDate.wav");
                                    await channel.Play("EtIvr/EnterLikeThisDay&etc.wav");

                                }
                                else if (filterDateList.Count == 1)
                                {
                                    filterDateList.Add(dateList);
                                    //now filter voicemail files and play them
                                    await VoiceMailPlayer.PlayFileListByFilter(channel, fileTypeId, filterDateList);
                                }
                               
                            }
                            else
                            {
                                await channel.Play("EtIvr/InvalidEntry.wav");
                            }
                        }
                    });
        }
    }
}
