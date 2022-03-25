using System;
using System.Configuration;
using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using EtraabERP.Database.Entities;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.ViewModels;
using MongoDB.Driver;
using NEventSocket.Channels;
using NAudio.Wave;
using System.IO;
using System.Threading;

namespace FreeswitchListenerServer.Class
{

    internal static partial class CallLog
    {
        private const string CL_Collection = "CallLog",
            CLIvr_Collection = "CallLog_IVR",
            CLDetail_Collection = "CallLog_Detail";

        private static string MongoDbName = "EtraabActivityLogs";
        private static readonly string MongoDbHost = ConfigurationManager.AppSettings["MongoDbHost"];
        private static readonly string MongoDbPort = ConfigurationManager.AppSettings["MongoDbPort"];
        private static MongoDb _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);

        public static async void SaveCallLog_SQL(this Channel channel)
        {
            try
            {
                var caller = Caller.GetCallerInfo(channel);
                using (var db = new ErpContainer())
                {
                    var newCallLog = new ComFreeswitchCallLog()
                    {
                        ComFreeswitchCallLogPID = Guid.NewGuid(),
                        DestinationNumber = channel.GetDesNumber(),
                        CallUuid = channel.UUID,
                        CallerNumber = caller.CallerNumber,
                        CallStrart = DateTime.Now,
                        CallEnd = DateTime.Now,
                        IsBridged = false,
                        HangupCauseId = (short)(channel.HangupCause ?? 0)
                    };

                    db.ComFreeswitchCallLog.Add(newCallLog);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void MakeCallBridged(this Channel channel)
        {
            try
            {
                var uuid = channel.UUID;
                using (var db = new ErpContainer())
                {
                    var log = await db.ComFreeswitchCallLog.FirstOrDefaultAsync(c => c.CallUuid == uuid);
                    if (log != null && !log.IsBridged)
                    {
                        log.IsBridged = true;
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void UpdateEndCall_SQL(this Channel channel)
        {
            try
            {
                var uuid = channel.UUID;
                using (var db = new ErpContainer())
                {
                    var log = await db.ComFreeswitchCallLog.FirstOrDefaultAsync(c => c.CallUuid == uuid);
                    if (log != null)
                    {
                        log.CallEnd = DateTime.Now;
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void AddLog(this Channel channel)
        {
            if (_mongoDb == null)
                _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);

            try
            {
                var dateTime = DateTime.UtcNow;
                var uuid = channel.UUID;
                var collection = _mongoDb.GetCollection<CallLogVm>(CL_Collection);
                await _mongoDb.Insert(collection, new CallLogVm()
                {
                    CallUuid = uuid,
                    Start = dateTime,
                    DialedNumber = channel.GetDesNumber(),
                    End = dateTime,
                    Data = null,
                    IsBridged = false,
                    TelephoneNumber = channel.GetCallerInfo().CallerNumber
                });
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on AddLog -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on AddLog TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void AddLogDetail(this Channel channel, string agent, string state,string brId = "")
        {
            if (_mongoDb == null)
                _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);
            try
            {
                var dateTime = DateTime.UtcNow;
                var uuid = channel.UUID;
                var collection = _mongoDb.GetCollection<CallLogDetailVm>(CLDetail_Collection);
                await _mongoDb.Insert(collection, new CallLogDetailVm()
                {
                    CallUuid = uuid,
                    BridgeId = brId,
                    Start = dateTime,
                    AgentNumber = agent,
                    End = dateTime,
                    TransferedFrom = "",
                    State = state,
                    Data = null
                });
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on AddLogDetail -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on AddLogDetail TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void AddLogIvr(this Channel channel, Guid appId, string input, bool success = true)
        {
            if (_mongoDb == null)
                _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);

            try
            {
                var uuid = channel.UUID;
                var collection = _mongoDb.GetCollection<CallLogIvrVm>(CLIvr_Collection);
                await _mongoDb.Insert(collection, new CallLogIvrVm()
                {
                    CallUuid = uuid,
                    AppId = appId,
                    Input = input,
                    Success = success
                });
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on AddLogIvr -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on AddLogIvr TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void SetEndToCallLog(this Channel channel)
        {
            try
            {
                var dateTime = DateTime.UtcNow;
                var collection = _mongoDb.GetCollection<CallLogVm>(CL_Collection);
                if (collection != null)
                {
                    var updated = Builders<CallLogVm>.Update.Set(m => m.End, dateTime);
                    await collection.UpdateManyAsync(detail => detail.CallUuid == channel.UUID, updated);
                }
                else
                {
                    LogHelper.LogDarkRed($"collection {CL_Collection} not found.");
                }
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on SetEndToCallLog -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on SetEndToCallLog TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void UpdateCallLog(this Channel channel, bool isBridged = false)
        {
            if (_mongoDb == null)
                _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);

            try
            {
                var collection = _mongoDb.GetCollection<CallLogVm>(CL_Collection);
                if (collection != null)
                {
                    var updated = Builders<CallLogVm>.Update.Set(m => m.IsBridged, isBridged);
                    await collection.UpdateManyAsync(detail => detail.CallUuid == channel.UUID, updated);
                }
                else
                {
                    LogHelper.LogDarkRed($"collection {CL_Collection} not found.");
                }
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on UpdateCallLog -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on UpdateCallLog TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void AddCallLogDetailByTr(this Channel channel, string agent = "", string trFrom = "", string state = "")
        {
            if (_mongoDb == null)
                _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);

            try
            {
                var dateTime = DateTime.UtcNow;
                var uuid = channel.UUID;
                var collection = _mongoDb.GetCollection<CallLogDetailVm>(CLIvr_Collection);
                if (collection != null)
                {
                    await _mongoDb.Insert(collection, new CallLogDetailVm()
                    {
                        CallUuid = uuid,
                        AgentNumber = agent,
                        TransferedFrom = trFrom,
                        Start = dateTime,
                        End = dateTime,
                        State = state
                    });
                }
                else
                {
                    LogHelper.LogDarkRed($"collection {CLIvr_Collection} not found.");
                }
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on AddCallLogDetailByTr -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on AddCallLogDetailByTr TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static async void UpdateCallLogIvr(this Channel channel, string input, bool success)
        {
            if (_mongoDb == null)
                _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);

            try
            {
                var collection = _mongoDb.GetCollection<CallLogIvrVm>(CLIvr_Collection);
                if (collection != null)
                {
                    var updated = Builders<CallLogIvrVm>.Update.Set(m => m.Input, input).Set(m => m.Success, success);
                    await collection.UpdateManyAsync(detail => detail.CallUuid == channel.UUID, updated);
                }
                else
                {
                    LogHelper.LogDarkRed($"collection {CLIvr_Collection} not found.");
                }
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on UpdateCallLogIvr -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on UpdateCallLogIvr TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }

        public static string GetLastCallFromLog(string voipNumber)
        {
            return "";
            //return CallLogList.Where(l => l.CallerNumber == voipNumber)
            //    .OrderByDescending(p => p.CallStrart).Select(l => l.DestinationNumber).FirstOrDefault();
        }
    }

    internal partial class CallLog
    {
        internal static async void SendCallDetailLog(string callUuid,string callerId)
        {
            if (!String.IsNullOrWhiteSpace(callUuid))
            {
                var collection = _mongoDb.GetCollection<CallLogDetailVm>(CLDetail_Collection);

                var query = await collection.FindAsync(u => u.CallUuid == callUuid);
                var detailList = await query.ToListAsync();
                detailList = detailList.OrderBy(p=>p.Start).ToList();

                SignalrClient.SendCallDetail(new ErpMessageVm()
                {
                    CallerId = callerId,
                    Data = detailList
                });
            }
        }

        internal static void SaveRecFileDataInDb(string callUuid,string brId)
        {
            Task.Run(() =>
            {
                Thread.Sleep(100);
                var fullPath = FreeswitchWorker.FsSoundDirectory + $@"rec\{brId}.wav";
                if (File.Exists(fullPath))
                {
                    try
                    {
                        using (WaveFileReader reader = new WaveFileReader(fullPath))
                        {
                            byte[] buffer = new byte[reader.Length];
                            int read = reader.Read(buffer, 0, buffer.Length);
                            SaveData(callUuid, brId, buffer);
                        }
                    }
                    catch
                    {
                    }
                }
            });
        }


        private static async void SaveData(string callUuid, string brId,byte[] data)
        {
            if (_mongoDb == null)
                _mongoDb = new MongoDb(MongoDbName, MongoDbHost, MongoDbPort);

            try
            {
                var dateTime = DateTime.UtcNow;
                var collection = _mongoDb.GetCollection<CallLogDetailVm>(CLDetail_Collection);
                if (collection != null)
                {
                    var updated = Builders<CallLogDetailVm>.Update.Set(m => m.End, dateTime).Set(m=>m.Data,data);
                    await collection.UpdateManyAsync(detail => detail.CallUuid == callUuid && detail.BridgeId == brId,
                        updated);
                }
                else
                {
                    LogHelper.LogDarkRed($"collection {CLDetail_Collection} not found.");
                }
            }
            catch (MongoException me)
            {
                LogHelper.LogDarkRed($"Error occurred on SetEndToCallLogDetail -> {me.Message}");
            }
            catch (TaskCanceledException te)
            {
                LogHelper.LogDarkRed($"Error occurred on SetEndToCallLogDetail TE-ERROR -> {te.Message}");
            }
            catch (Exception e)
            {
                LogHelper.WriteExceptionLog(e);
            }
        }
    }
}
