using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeswitchListenerServer.Class;
using FreeswitchListenerServer.deifintions;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.InboundApi;
using FreeswitchListenerServer.OutboundApi.NumberingPlan.VoiceMail;
using FreeswitchListenerServer.ViewModels;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;

namespace FreeswitchListenerServer.OutboundApi
{
    internal class VoiceMailPlayer : ErpContainerDataHelper
    {
        public static async Task<bool> Play(Channel channel, string desNumber)
        {
            var voiceMailFile = GetVoiceMail(desNumber);
            if (voiceMailFile != null && voiceMailFile.IsEnabled)
            {
                if (string.IsNullOrWhiteSpace(voiceMailFile.StopKey))
                    voiceMailFile.StopKey = "#";

                await channel.Play(new List<string>()
                {
                    FilePathKeeper.PleaseEnterVoiceAfterBoogh,
                    $"EtIvr/{voiceMailFile.StopKey}.wav",
                    FilePathKeeper.FinishIt
                });

                await channel.Play("EtIvr/beep.wav");
                //if (string.IsNullOrWhiteSpace(voiceMailFile.FileName))
                //{
                //    await channel.Play(new List<string>()
                //    {
                //        "EtIvr/PleaseEnterVoiceAfterBoogh.wav",
                //        $"EtIvr/{voiceMailFile.StopKey}.wav",
                //        "EtIvr/FinishIt.wav"
                //    });
                //}
                //else
                //{
                //    await channel.Play(voiceMailFile.FileName);
                //}

                return true;
            }
            return false;
        }

        public static async Task PlayFileList(Channel channel, short fileTypeId)
        {
            var caller = Caller.GetCallerInfo(channel);
            var fileList = GetVoiceMailList(caller.CallerNumber, fileTypeId);
            if (fileList.Count > 0)
            {
                await channel.Play(FilePathKeeper.Messages);
                //play all voice mails one by one
                for (int i = 0; i < fileList.Count; i++)
                {
                    var number = i + 1;
                    var file = fileList[i];
                    var ttsFileList = EtraabTts.GetFileNames(number.ToString()).ToList();
                    if (ttsFileList.Any())
                    {
                        await channel.Play(ttsFileList);
                    }

                    var persianDateTime = file.CreationTime.ToPersianDateTime();
                    await PlayFileDateTime(channel, persianDateTime);
                    await channel.Play(file.FullName);

                    if (fileTypeId == (short)EnVoiceFileType.NotPlayed)
                        SoundFileHelper.MoveFile(file.FullName, $"{VoipCallVar.PlayedVoiceMailDirector}{file.Name}.{file.Extension}");
                }
            }
            else if (fileList.Count == 0)
            {
                await channel.Play(FilePathKeeper.YourVoiceMailIsEmty);
                await new VoiceMailIvr().PlayAndGetVoiceMailFileType(channel);
            }
        }

        public static async Task PlayFileListByFilter(Channel channel, short fileTypeId, List<List<string>> dateList)
        {
            var caller = Caller.GetCallerInfo(channel);
            var fileList = GetVoiceMailListByFilter(caller.CallerNumber, fileTypeId, dateList);
            if (fileList.Count > 0)
            {
                await channel.Play(FilePathKeeper.Messages);
                //play all voice mails one by one
                for (int i = 0; i < fileList.Count; i++)
                {
                    var number = i + 1;
                    var file = fileList[i];
                    var ttsFileList = EtraabTts.GetFileNames(number.ToString()).ToList();
                    if (ttsFileList.Any())
                    {
                        await channel.Play(ttsFileList);
                    }

                    var persianDateTime = file.CreationTime.ToPersianDateTime();
                    await PlayFileDateTime(channel, persianDateTime);
                    await channel.Play(file.FullName);

                    if (fileTypeId == (short)EnVoiceFileType.NotPlayed)
                        SoundFileHelper.MoveFile(file.FullName, $"{VoipCallVar.PlayedVoiceMailDirector}{file.Name}.{file.Extension}");
                }
            }
            else if (fileList.Count == 0)
            {
                await channel.Play(FilePathKeeper.VoiceMailIsEmptyInDate);
                await new VoiceMailIvr().PlayAndGetVoiceMailFileType(channel);
            }
        }

        private static async Task PlayFileDateTime(Channel channel, PersianDateTime persianDateTime)
        {
            var year = persianDateTime.Year.ToString();
            var month = persianDateTime.Month;
            var day = persianDateTime.Day;
            var hour = persianDateTime.Hour.ToString();
            var minute = persianDateTime.Minute.ToString();
            var files = new List<string> { FilePathKeeper.Date };
            files.AddRange(EtraabTts.GetFileNamesOfDate(year, month, day).ToList());
            files.AddRange(EtraabTts.GetFileNamesOfTime(hour, minute));

            await channel.Play(files);

        }

        public static void MoveVoiceMailFile(string fileName)
        {
            var orgDir = VoipCallVar.NotPlayedVoiceMailDirector + fileName;
            var newDes = VoipCallVar.PlayedVoiceMailDirector + fileName;
            if (!File.Exists(newDes))
            {
                SoundFileHelper.MoveFile(orgDir, newDes);
            }
        }

        private static List<FileInfo> GetVoiceMailList(string voipNumber, short fileTypeId)
        {
            var directory = (fileTypeId == (short)EnVoiceFileType.NotPlayed)
                ? VoipCallVar.NotPlayedVoiceMailDirector
                : VoipCallVar.PlayedVoiceMailDirector;

            var fileList = SoundFileHelper
                .GetFileListWithStart($"VoiceMail[{voipNumber}]", directory).ToList();

            return fileList;
        }

        private static List<FileInfo> GetVoiceMailListByFilter(string voipNumber, short fileTypeId, List<List<string>> dateList)
        {
            var directory = (fileTypeId == (short)EnVoiceFileType.NotPlayed)
                ? VoipCallVar.NotPlayedVoiceMailDirector
                : VoipCallVar.PlayedVoiceMailDirector;

            var fileList = SoundFileHelper
                .GetFileListWithStartByDate($"VoiceMail[{voipNumber}]", directory, dateList);

            return fileList;
        }

        public static VoiceMailVm GetVoiceMail(string voipNumber)
        {
            return VoiceMailSettingsList.FirstOrDefault(p => p.VoipNumber == voipNumber);
        }

        public static async Task StartVoiceMailEventHandler(Channel channel, VoiceMailVm voiceMail)
        {
            if (string.IsNullOrWhiteSpace(voiceMail.StopKey))
                voiceMail.StopKey = "#";

            var uuid = channel.UUID;
            var callerNum = Caller.GetCallerInfo(channel).CallerNumber;
            var dateString = DateTime.Now.ToPersianDateTimeString();
            var recordingPath =
                $"voicemail/Etraab/notPlayed/VoiceMail[{voiceMail.VoipNumber}]caller{callerNum}-{SoundFileHelper.FilenameFromTitle(dateString)}.wav";

            await channel.StartRecording(recordingPath, voiceMail.Max);
            channel.Events.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf)
                .Subscribe(
                async (e) =>
                {
                    try
                    {
                        var dtmf = e.Headers[HeaderNames.DtmfDigit];
                        if (dtmf == voiceMail.StopKey)
                        {
                            await channel.StopRecording();
                            await channel.Hangup();
                            await SignalrClient.SendMessage(FresswitchConstVariables.ReloadVoiceMailData);
                        }
                        else if (dtmf == voiceMail.MaskKey)
                        {
                            await channel.MaskRecording();
                        }
                        else if (dtmf == voiceMail.UnMaskKey)
                        {
                            await channel.UnMaskRecording();
                        }

                    }
                    catch (Exception exception)
                    {
                        await channel.StopRecording();
                        await channel.Hangup();
                    }
                });
        }

        public static async Task RecordNoAnswerVoiceMail(Channel channel)
        {
            //todo:voicemail must go to specific extension not just Mehdi
            //todo:301 must be dynamic
            var voiceMail = GetVoiceMail("301");
            if (voiceMail != null && voiceMail.IsEnabled)
            {
                var isSuccess = await Play(channel, "301");
                if (isSuccess)
                {
                    await StartVoiceMailEventHandler(channel, voiceMail);
                }
                else
                {
                    await channel.Hangup();
                }
            }
            else
            {
                await channel.Hangup(HangupCause.NoAnswer);
            }
        }
    }
}
