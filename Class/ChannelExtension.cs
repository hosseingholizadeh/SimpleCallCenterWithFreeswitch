using System;
using System.Threading;
using System.Threading.Tasks;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.InboundApi.OperatorApp;
using FreeswitchListenerServer.OutboundApi;
using FreeswitchListenerServer.ViewModels;
using NEventSocket;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;
using NEventSocket.Util;

namespace FreeswitchListenerServer.Class
{
    public static class ConnectionExtension
    {
        /// <summary>
        ///caceling all medias that are playing in the connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static async Task CancelMedia(this OutboundSocket connection)
        {
            await connection.Api("uuid_break", connection.ChannelData.UUID);
        }

        public static async Task<IDisposable> PlayUntilCancelled(this OutboundSocket connection, string file)
        {
            // essentially, we'll do a playback application call without waiting for the ChannelExecuteComplete event
            // the caller can .Dispose() the returned token to do a uuid_break on the channel to kill audio.
            await connection.SendCommand($"sendmsg {connection.ChannelData.UUID}\ncall-command: execute\nexecute-app-name: playback\nexecute-app-arg:{file}\nloops:-1");

            var cancellation = new DisposableAction(
                async () =>
                {
                    try
                    {
                        await connection.CancelMedia();
                    }
                    catch (Exception ex)
                    {
                    }
                });

            return cancellation;
        }

        public static async Task<ApiResponse> StartRecording(this OutboundSocket connection, int? maxSeconds = null, string voipNumber = "")
        {
            try
            {
                var uuid = connection.ChannelData.UUID;
                var recordingPath = $"voicemail/Etraab/notPlayed/VoiceMail[{voipNumber}]-{uuid}.wav";
                var result = await connection.SendApi("uuid_record {0} start {1} {2}".Fmt(uuid, recordingPath, maxSeconds * 60));
                return result;
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
                return null;
            }
        }

        public static async Task<ApiResponse> StopRecording(this OutboundSocket connection, string voipNumber = "")
        {
            var uuid = connection.ChannelData.UUID;
            var recordingPath = $"voicemail/Etraab/notPlayed/VoiceMail[{voipNumber}]-{uuid}.wav";
            var result = await connection.SendApi("uuid_record {0} stop {1}".Fmt(uuid, recordingPath));
            return result;
        }

        public static async Task<ApiResponse> MaskRecording(this OutboundSocket connection, string voipNumber = "")
        {
            var uuid = connection.ChannelData.UUID;
            var recordingPath = $"voicemail/Etraab/notPlayed/VoiceMail[{voipNumber}]-{uuid}.wav";
            var result = await connection.SendApi("uuid_record {0} mask {1}".Fmt(uuid, recordingPath));
            return result;
        }

        public static async Task<ApiResponse> UnMaskRecording(this OutboundSocket connection, string voipNumber = "")
        {
            var uuid = connection.ChannelData.UUID;
            var recordingPath = $"voicemail/Etraab/notPlayed/VoiceMail[{voipNumber}]-{uuid}.wav";
            var result = await connection.SendApi("uuid_record {0} unmask {1}".Fmt(uuid, recordingPath));
            return result;
        }

        public static async Task HoldToggle(this OutboundSocket connection)
        {
            await connection.SendApi("uuid_hold toggle " + connection.ChannelData.UUID);
        }

        public static async Task HoldOn(this OutboundSocket connection)
        {
            await connection.SendApi("uuid_hold " + connection.ChannelData.UUID);
        }

        public static async Task HoldOff(this OutboundSocket connection)
        {
            await connection.SendApi("uuid_hold off " + connection.ChannelData.UUID);
        }

        public static async Task Park(this OutboundSocket connection)
        {
            await connection.ExecuteApplication(connection.ChannelData.UUID, "park");
        }

        public static Task RingReady(this OutboundSocket connection)
        {
            return connection.ExecuteApplication(connection.ChannelData.UUID, "ring_ready");
        }

        public static Task Answer(this OutboundSocket connection)
        {
            return connection.ExecuteApplication(connection.ChannelData.UUID, "answer");
        }

        public static string GetDesNumber(this OutboundSocket connection)
        {
            return connection.ChannelData.Headers[FresswitchConstVariables.CallerDestinationNumber];
        }



    }

    public static class ChannelExtension
    {
        /// <summary>
        ///canceling all medias that are playing in the channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static async Task CancelMedia(this Channel channel)
        {
            if (channel.IsAnswered)
                await channel.Socket.Api("uuid_break", channel.UUID);
        }

        /// <summary>
        /// Freeswitch Command : uuid_transfer
        /// </summary>
        /// <returns></returns>
        public static async Task Transfer(this Channel channel, string target)
        {
            if (channel.IsAnswered)
            {
                var result = await channel.Socket.SendApi($"uuid_transfer {channel.UUID} {target}");
                if (result.Success)
                    LogHelper.Log($"transfer response is {result.Success}");

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    LogHelper.Log($"transfer response is {result.ErrorMessage}");
            }
        }

        public static async Task PlayExtIsNotAvailable(this Channel channel)
        {
            if (channel.IsAnswered)
                await channel.Play(FilePathKeeper.ExtIsNotAvailable);
        }

        public static async Task PlayVoiceMail(this Channel channel, string desNumber, VoiceMailVm voiceMail)
        {
            var isSuccess = await VoiceMailPlayer.Play(channel, desNumber);
            if (isSuccess)
            {
                await VoiceMailPlayer.StartVoiceMailEventHandler(channel, voiceMail);
            }
        }


        public static async Task<ApiResponse> UnMaskRecording(this Channel channel, string voipNumber = "")
        {
            try
            {
                var uuid = channel.UUID;
                var recordingPath = $"voicemail/Etraab/notPlayed/VoiceMail[{voipNumber}]-{uuid}.wav";
                var result = await channel.Socket.SendApi("uuid_record {0} unmask {1}".Fmt(uuid, recordingPath));
                return result;
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
                return null;
            }
        }

        public static string GetDesNumber(this Channel channel)
        {
            return new VoipHelper().GetExactDesNumber(channel);
        }

        public static async Task NoOneIsAvailable(this Channel channel)
        {
            //پیغام کسی در دسترس نبود
            LogHelper.Log("channel hangup with no one available.");
            if (channel.IsAnswered)
                await channel.Play("EtIvr/SorryNoExtIsAvailable.wav");

            await VoiceMailPlayer.RecordNoAnswerVoiceMail(channel);
        }

        public static async void CallOperators(this Channel channel, CancellationToken ct)
        {
            if (!ct.IsCancellationRequested && channel.IsAnswered)
            {
                var caller = Caller.GetCallerInfo(channel);
                LogHelper.Log($"{caller.CallerNumber} forwarded to operators.");
                await new HandleCallToOperator().HandleCall(channel, ct);
            }
        }

    }

    internal class DisposableAction : IDisposable
    {
        private readonly InterlockedBoolean disposed = new InterlockedBoolean();

        private readonly Action onDispose;

        public DisposableAction(Action onDispose = null)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            if (disposed != null && !disposed.EnsureCalledOnce())
            {
                if (onDispose != null)
                {
                    onDispose();
                }
            }
        }
    }

    /// <summary>
    /// Interlocked support for boolean values
    /// </summary>
    internal class InterlockedBoolean
    {
        private int _value;

        /// <summary>
        /// Current value
        /// </summary>
        public bool Value
        {
            get { return _value == 1; }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:InterlockedBoolean"/>
        /// </summary>
        /// <param name="initialValue">initial value</param>
        public InterlockedBoolean(bool initialValue = false)
        {
            _value = initialValue ? 1 : 0;
        }

        /// <summary>
        /// Sets a new value
        /// </summary>
        /// <param name="newValue">new value</param>
        /// <returns>the original value before any operation was performed</returns>
        public bool Set(bool newValue)
        {
            var oldValue = Interlocked.Exchange(ref _value, newValue ? 1 : 0);
            return oldValue == 1;
        }

        /// <summary>
        /// Compares the current value and the comparand for equality and, if they are equal, 
        /// replaces the current value with the new value in an atomic/thread-safe operation.
        /// </summary>
        /// <param name="newValue">new value</param>
        /// <param name="comparand">value to compare the current value with</param>
        /// <returns>the original value before any operation was performed</returns>
        public bool CompareExchange(bool newValue, bool comparand)
        {
            var oldValue = Interlocked.CompareExchange(ref _value, newValue ? 1 : 0, comparand ? 1 : 0);
            return oldValue == 1;
        }
    }

    internal static class InterlockedBooleanExtensions
    {
        internal static bool EnsureCalledOnce(this InterlockedBoolean interlockedBoolean)
        {
            return interlockedBoolean.CompareExchange(true, false);
        }
    }
}
