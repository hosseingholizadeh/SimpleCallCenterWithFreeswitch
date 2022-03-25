using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using FreeswitchListenerServer.deifintions;
using FreeswitchListenerServer.FreeswitchApiClass;
using FreeswitchListenerServer.Helper;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;

namespace FreeswitchListenerServer.Class
{
    internal struct MessageVm
    {
        public short OpTypeId { get; set; }
        public dynamic Model { get; set; }
    }

    internal struct ErpMessageVm
    {
        public string CallerId;
        public dynamic Data;
    }

    internal class RokhCallCenterMessageVm
    {
        public string AgentNumber { get; set; }
        public string TelephoneNumber { get; set; }
    }

    internal class SignalrClient
    {
        private static HubConnection _connection;
        private static IHubProxy _wsClient;

        internal static async Task<bool> Start()
        {
            bool connected = false;
            try
            {
                _connection = new HubConnection(ConfigurationManager.AppSettings["SignalrIpPort"]);
                _wsClient = _connection.CreateHubProxy("etHub");
                await _connection.Start();

                if (_connection.State == ConnectionState.Connected)
                {
                    connected = true;
                    LogHelper.LogGreen("Signalr connected.");
                    _connection.Closed += Connection_Closed;
                    _wsClient.On<string>("newListenerMessage", OnMessage);
                    //await SendMessage(FresswitchConstVariables.OutboundListenerCSharpClient);
                    await ClientConnectedMessage();
                }
                return connected;
            }
            catch (HubException he)
            {
                LogHelper.LogDarkRed($"Hub Exception occurred when starting signalr connection -> {he.Message}.");
                return false;
            }
            catch (Exception e)
            {
                LogHelper.LogDarkRed($"Exception occurred when starting signalr connection -> {e.Message}.");
                return false;
            }
        }

        private static async void Connection_Closed()
        {
            LogHelper.LogDarkRed("Signalr connetion is lost.");
            LogHelper.Log("Signalr reconnecting.");

            // specify a retry duration
            TimeSpan retryDuration = TimeSpan.FromSeconds(30);
            DateTime retryTill = DateTime.UtcNow.Add(retryDuration);

            while (DateTime.UtcNow < retryTill)
            {
                bool connected = await Start();
                if (connected)
                    return;
            }
            LogHelper.LogMagenta("Signalr connetion fully closed.");
        }

        internal static async Task SendMessage(string message)
        {
            try
            {
                if (_wsClient != null && _connection.State == ConnectionState.Connected)
                    await _wsClient.Invoke<string>("Send", message).ContinueWith(task1 =>
                    {
                        if (task1.IsFaulted)
                        {
                            LogHelper.LogRed($"There was an error calling send: {task1.Exception?.GetBaseException()}");
                        }
                    });
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        internal static async Task ClientConnectedMessage()
        {
            try
            {
                if (_wsClient != null && _connection.State == ConnectionState.Connected)
                    await _wsClient.Invoke<string>("ListenerClientConnected").ContinueWith(task1 =>
                    {
                        if (task1.IsFaulted)
                        {
                            LogHelper.LogRed($"There was an error calling send: {task1.Exception?.GetBaseException()}");
                        }
                    });
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        internal static async void SendNewCallConnectedMessage(RokhCallCenterMessageVm message)
        {
            try
            {
                if (_wsClient != null && _connection.State == ConnectionState.Connected)
                {
                    var bsonData = BsonHelper.ToBson(message);
                    await _wsClient.Invoke<string>("NewCallConnectedToRokhCallCenter", bsonData).ContinueWith(task1 =>
                    {
                        if (task1.IsFaulted)
                        {
                            LogHelper.LogRed($"There was an error calling send: {task1.Exception?.GetBaseException()}");
                        }
                    });
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        internal static async void SendCallDetail(ErpMessageVm message)
        {
            try
            {
                if (_wsClient != null && _connection.State == ConnectionState.Connected)
                {
                    var bsonData = BsonHelper.ToBson(message);
                    await _wsClient.Invoke<string>("SetCallLogDetail", bsonData).ContinueWith(task1 =>
                    {
                        if (task1.IsFaulted)
                        {
                            LogHelper.LogRed($"There was an error calling send: {task1.Exception?.GetBaseException()}");
                        }
                    });
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        internal static void Stop()
        {
            _connection?.Stop();
        }

        /// <summary>
        /// Incoming messages
        /// Message will be a Bson
        /// </summary>
        /// <param name="message"></param>
        private static async void OnMessage(string message)
        {
            try
            {
                var data = BsonHelper.FromBson<MessageVm>(message);
                if (data.OpTypeId == (short)SignalrOperationType.GetChannelRealTimeVars)
                {
                    //get real time data of channels for seeing their status 
                    //this function will be affect on ChannelIndex view grid in Erp
                    Task<string> channelDataTaskStr = FreeswitchApi.ShowChannels();
                    channelDataTaskStr.Wait();
                    var channelData = new List<IDictionary<string, string>>();
                    var result = channelDataTaskStr.Result;

                    if (!string.IsNullOrWhiteSpace(result) && channelDataTaskStr.IsCompleted)
                    {
                        //Get channel realtime data 
                        channelData = FreeswitchHelper.GetRealTimeVars(result);
                        //customize received data as own required model
                        channelData = FreeswitchHelper.CustomizeData(channelData);
                    }

                    if (channelData.Count > 0)
                    {
                        var serializedData = JsonConvert.SerializeObject(channelData);
                        await SendMessage(FresswitchConstVariables.CallRealTimeData + "#" + serializedData);
                    }
                    else
                    {
                        await SendMessage(FresswitchConstVariables.CallRealTimeData + "#0");
                    }

                }
                else if (data.OpTypeId == (short)SignalrOperationType.ReloadData)
                {
                    Task ts = ErpContainerDataHelper.ReloadAllData();
                }
                else if (data.OpTypeId == (short)SignalrOperationType.ReloadXml)
                {
                    Task ts = FreeswitchApi.ReloadXml();
                }
                else if (message.StartsWith(FresswitchConstVariables.Transfer))
                {
                    message = message.Substring(FresswitchConstVariables.Transfer.Length);
                    var obj = JsonConvert.DeserializeObject<TransferVm>(message);
                    LogHelper.Log($"channel id for transfer is {obj.Cid} to {obj.Target}.");

                    await new Transfer().Start(obj.Cid, obj.Target);
                }
                else if (!string.IsNullOrWhiteSpace(message) && message.StartsWith("MSM"))
                {
                    var obj = JsonConvert.DeserializeObject<MessageVm>(message);
                    if (obj.OpTypeId == (short)SignalrOperationType.RemoveBusyLine)
                    {
                        var number = obj.Model;
                        BusyLine.Remove(number);
                    }
                }
                else if (data.OpTypeId == (short)SignalrOperationType.GetCallLogDetail)
                {
                    CallLog.SendCallDetailLog(data.Model.CallUuid.ToString(), data.Model.ClientId.ToString());
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

    }
}
