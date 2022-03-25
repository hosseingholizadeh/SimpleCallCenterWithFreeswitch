using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ColoredConsole;
using FreeswitchListenerServer.Helper;
using FreeswitchListenerServer.InboundApi;

namespace FreeswitchListenerServer.Class
{
    public struct TabResult
    {
        public string Result { get; set; }
        public bool IsRead { get; set; }
    }
    public class ConsoleMenu
    {
        private static readonly string[] TabKeyList = new[] { "reload-data", "clear", "exit", "reconnect", "reload-db" ,"getusers"};
        private static List<TabResult> _tabResList = new List<TabResult>();

        public static void HandleInputs()
        {
            var running = true;
            var input = new StringBuilder();
            while (running)
            {
                var result = Console.ReadKey();
                switch (result.Key)
                {
                    case ConsoleKey.Enter:
                        ExecuteCommand(input.ToString(),out running);
                        if (_tabResList.Count > 0)
                        {
                            _tabResList.Clear();
                        }
                        input.Clear();
                        break;
                    case ConsoleKey.Tab:
                        HandleTabInput(input);
                        break;
                    default:
                        HandleKeyInput(input, result);
                        if (_tabResList.Count > 0)
                        {
                            _tabResList.Clear();
                        }

                        break;
                }
            }

        }

        private static void ExecuteCommand(string command,out bool running)
        {
            running = true;
            switch (command)
            {
                case "reconnect":
                    var connected = SignalrClient.Start();
                    Console.WriteLine();
                    break;
                case "clear":
                    Console.Clear();
                    if (FreeswitchInboundSocketApi.ListenerIsStarted)
                    {
                        LogHelper.LogListenerStarted();
                    }
                    break;

                case "reload-data":
                    Task ts = ErpContainerDataHelper.ReloadAllData();
                    Console.WriteLine();
                    break;
                case "getusers":
                    GetUsers();
                    break;
                case "exit":
                    running = false;
                    Console.WriteLine();
                    break;
                default:
                    ColorConsole.WriteLine("unknown command.".Red());
                    break;

            }
        }

        private static void GetUsers()
        {
            ColorConsole.WriteLine("users:");
            ColorConsole.WriteLine("------------------------------");
            ColorConsole.WriteLine("count " + ErpContainerDataHelper.VoipNumberList.Count);
            ErpContainerDataHelper.VoipNumberList.CustomeForEach((number, i )=>
            {
                ColorConsole.WriteLine(((i + 1) + ":" + number).Magenta());
            });
            ColorConsole.WriteLine("------------------------------");
        }

        private static void ClearCurrentLine()
        {
            var currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLine);
        }

        private static void HandleTabInput(StringBuilder builder)
        {
            if (_tabResList != null && _tabResList.Count > 0)
            {
                ClearCurrentLine();
                builder.Clear();
                var tabRes = _tabResList.Cast<TabResult?>().FirstOrDefault(p => p != null && !p.Value.IsRead);
                if (tabRes == null)
                {
                    _tabResList = _tabResList.Select(p => new TabResult()
                    {
                        Result = p.Result,
                        IsRead = false
                    }).ToList();

                    if (_tabResList.Count == 0)
                    {
                        return;
                    }

                    ClearCurrentLine();
                    builder.Clear();

                    var tabResult = _tabResList[0];
                    Console.Write(tabResult.Result);
                    builder.Append(tabResult.Result);
                    _tabResList[0] = new TabResult()
                    {
                        Result = tabResult.Result,
                        IsRead = true
                    };
                }
                else
                {
                    ClearCurrentLine();
                    builder.Clear();

                    var tabResVal = tabRes.Value;
                    Console.Write(tabResVal.Result);
                    builder.Append(tabResVal.Result);
                    var index = _tabResList.IndexOf(tabResVal);
                    _tabResList[index] = new TabResult()
                    {
                        Result = tabResVal.Result,
                        IsRead = true
                    };
                }
            }
            else
            {
                var currentInput = builder.ToString();
                _tabResList = TabKeyList.Where(item =>
                    item != currentInput && item.StartsWith(currentInput, true, CultureInfo.InvariantCulture)).Select(
                    p => new TabResult()
                    {
                        Result = p,
                        IsRead = false
                    }).ToList();

                if (_tabResList.Count == 0)
                {
                    return;
                }

                ClearCurrentLine();
                builder.Clear();

                var tabResult = _tabResList[0];
                Console.Write(tabResult.Result);
                builder.Append(tabResult.Result);
                _tabResList[0] = new TabResult()
                {
                    Result = tabResult.Result,
                    IsRead = true
                };
            }
        }

        private static void HandleKeyInput(StringBuilder builder, ConsoleKeyInfo input)
        {
            var currentInput = builder.ToString();
            if (input.Key == ConsoleKey.Backspace && currentInput.Length > 0)
            {
                builder.Remove(builder.Length - 1, 1);
                ClearCurrentLine();

                currentInput = currentInput.Remove(currentInput.Length - 1);
                Console.Write(currentInput);
            }
            else
            {
                var key = input.KeyChar;
                builder.Append(key);
            }
        }
    }
}
