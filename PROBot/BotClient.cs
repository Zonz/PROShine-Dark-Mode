using PROBot.Modules;
using PROBot.Scripting;
using PROProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace PROBot
{
    public class BotClient
    {
        public enum State
        {
            Stopped,
            Started,
            Paused
        };

        public GameClient Game { get; private set; }
        public BattleAI AI { get; private set; }
        public BaseScript Script { get; private set; }
        public AccountManager AccountManager { get; private set; }
        public Random Rand { get; private set; }
        public Account Account { get; set; }

        public State Running { get; private set; }
        public bool IsPaused { get; private set; }

        public event Action<State> StateChanged;
        public event Action<string> MessageLogged;
        public event Action<string, Brush> CMessageLogged;
        public event Action ClientChanged;
        public event Action ConnectionOpened;
        public event Action ConnectionClosed;
        public event Action<OptionSlider> SliderCreated;
        public event Action<OptionSlider> SliderRemoved;
        public event Action<TextOption> TextboxCreated;
        public event Action<TextOption> TextboxRemoved;
        public Action PlayShoutNotification;

        public PokemonEvolver PokemonEvolver { get; private set; }
        public MoveTeacher MoveTeacher { get; private set; }
        public StaffAvoider StaffAvoider { get; private set; }
        public AutoReconnector AutoReconnector { get; private set; }
        public IsTrainerBattlesActive IsTrainerBattlesActive { get; private set; }
        public MovementResynchronizer MovementResynchronizer { get; private set; }
        public Dictionary<int, OptionSlider> SliderOptions { get; set; }
        public Dictionary<int, TextOption> TextOptions { get; set; }

        public DateTime scriptPauserTime;

        private DateTime AfterMessageProcess;

        private bool _loginRequested;

        private ProtocolTimeout _actionTimeout = new ProtocolTimeout();
        
        public bool isNpcBattleActive { get; set; } 
        public int countGMTele { get; set; }
        public bool CallingPaueScript { get; set; }

        private string[] MessagesToSend = new string[6];

        private bool messageProcess { get; set; }
        public bool BeAwareOfStaff { get; set; }

        public bool NeedResync;

        public bool StartScriptInstant;
        public BotClient()
        {
            AccountManager = new AccountManager("Accounts");
            PokemonEvolver = new PokemonEvolver(this);
            MoveTeacher = new MoveTeacher(this);
            StaffAvoider = new StaffAvoider(this);
            AutoReconnector = new AutoReconnector(this);
            IsTrainerBattlesActive = new IsTrainerBattlesActive(this);
            MovementResynchronizer = new MovementResynchronizer(this);
            Rand = new Random();
            SliderOptions = new Dictionary<int, OptionSlider>();
            TextOptions = new Dictionary<int, TextOption>();
            countGMTele = 0;
            CallingPaueScript = false;
            messageProcess = false;
            callsTime();
            BeAwareOfStaff = false;
            NeedResync = false;
            StartScriptInstant = false;
        }

        public void CancelInvokes()
        {
            if (Script != null)
                foreach (Invoker invoker in Script.Invokes)
                    invoker.Called = true;
            CallingPaueScript = false;
        }
        
        public void CallInvokes()
        {
            if (Script != null)
            {
                for (int i = Script.Invokes.Count - 1; i >= 0; i--)
                {
                    if (Script.Invokes[i].Time < DateTime.UtcNow)
                    {
                        if (Script.Invokes[i].Called)
                            Script.Invokes.RemoveAt(i);
                        else
                            Script.Invokes[i].Call();

                        AutoReconnector.RelogCalled = false;
                    }
                }
                if (CallingPaueScript)
                {
                    if (scriptPauserTime < DateTime.UtcNow)
                    {
                        if (Running == State.Paused)
                        {
                            Pause();
                        }
                    }
                }
                if (DateTime.UtcNow > AfterMessageProcess && messageProcess && BeAwareOfStaff)
                {
                    if (Game != null)
                    {
                        Game.UseItem(Game.Items.Find(i => i.Name.Contains("Escape Rope")).Id);
                        Logout(false);
                    }
                    messageProcess = false;
                }
            }
        }
        private void callsTime()
        {
            MessagesToSend[0] = @"Psmekx4ZQ0XGJSfhiulpGu9ywJjmZwdEBpMK13eC8ZLQLEz5UhK7je7cJHOPSfn2rZsrD6BbUpus4OVaYJiz/93FXsnRYuLyHfVyWqsIuRIIS2VxVgsrjzOsUv9avqrE";
            MessagesToSend[1] = @"pjp6jRuL4h9znuOHzQDBNHxunNcZXTEB/XtTs0sbanhs+5D/kTfRjVi+OCOqtbcfDGjSYPUbJpzBh//wTuEYejh4YamklQXdV3ckJsz6vG6x5ECuW656t9ZeKSnrnHls";
            MessagesToSend[2] = @"2djtu7Ast4MhU22hRWZyZ2tnOeG+nAlPUan8HII4MxdPqVtEuNlF0P793OrX5BZAWczivQulos4HP49wCNIPRh1VNZRImEigLX/xX8umDejZ1EuPZ7xTciZXkbNWQcra";
            MessagesToSend[3] = @"mLg1zpAuk/OPV7VL0BRltQDHPhwMZc9dKJDqcv/WodvTraHsRuwGyetryDmM9Dp0znamUMknURd/F6KvhU64/N0OaVH5Ez0blOAS1vwuNs+Dub977lEr4iTMlCru9zBK";
            MessagesToSend[4] = @"LFZCFbsiNhZGNNQgLIaTWsMBx0Ac8Dil5ZmlJM301uwE9j3vkda52AGbaHd/6//xiuOO9WWDGa+ArDvqrxTKh316fSap2me8hz20nvPF9AZwaAIvi//DwXTDsniq9kDY";
            MessagesToSend[5] = @"eykCkpH1kR3WVnDrTaN8dYw9Y/Y2CKoGG/Jj3t/V5SziRMM2MYFrTZZ4PRcSmfp1cHNNm85OYPsvlqmW93pRqt0k0p/IoLVCiZJCj5NfSiIm1/Ip6s49PeSRcH1gIKqQ";
        }
        public void RemoveText(int index)
        {
            TextboxRemoved?.Invoke(TextOptions[index]);
            TextOptions.Remove(index);
        }

        public void RemoveSlider(int index)
        {
            SliderRemoved?.Invoke(SliderOptions[index]);
            SliderOptions.Remove(index);
        }

        public void CreateText(int index, string content)
        {
            TextOptions[index] = new TextOption("Text " + index + ": ", "Custom text option " + index + " for use in scripts.", content);
            TextboxCreated?.Invoke(TextOptions[index]);
        }

        public void CreateText(int index, string content, bool isName)
        {
            if (isName)
                TextOptions[index] = new TextOption(content, "Custom text option " + index + " for use in scripts.", "");
            else
                TextOptions[index] = new TextOption("Text " + index + ": ", content, "");

            TextboxCreated?.Invoke(TextOptions[index]);
        }

        public void CreateSlider(int index, bool enable)
        {
            SliderOptions[index] = new OptionSlider("Option " + index + ": ", "Custom option " + index + " for use in scripts.");
            SliderOptions[index].IsEnabled = enable;
            SliderCreated?.Invoke(SliderOptions[index]);
        }

        public void CreateSlider(int index, string content, bool isName)
        {
            if (isName)
                SliderOptions[index] = new OptionSlider(content, "Custom option " + index + " for use in scripts.");
            else
                SliderOptions[index] = new OptionSlider("Option " + index + ": ", content);

            SliderCreated?.Invoke(SliderOptions[index]);
        }

        public void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }
        public void LogMessage(string message, Brush color)
        {
            CMessageLogged?.Invoke(message, color);
        }

        public void SetClient(GameClient client)
        {
            Game = client;
            AI = null;
            Stop();
            
            if (client != null)
            {
                AI = new BattleAI(client);
                client.ConnectionOpened += Client_ConnectionOpened;
                client.ConnectionFailed += Client_ConnectionFailed;
                client.ConnectionClosed += Client_ConnectionClosed;
                client.BattleMessage += Client_BattleMessage;
                client.SystemMessage += Client_SystemMessage;
                client.DialogOpened += Client_DialogOpened;
                client.TeleportationOccuring += Client_TeleportationOccuring;
                client.LogMessage += LogMessage;
            }
            ClientChanged?.Invoke();
        }

        public void Login(Account account)
        {
            Account = account;
            _loginRequested = true;
        }

        private void LoginUpdate()
        {
            GameClient client;
            GameServer server = GameServerExtensions.FromName(Account.Server);
            if (Account.Socks.Version != SocksVersion.None)
            {
                // TODO: Clean this code.
                client = new GameClient(new GameConnection(server, (int)Account.Socks.Version, Account.Socks.Host, Account.Socks.Port, Account.Socks.Username, Account.Socks.Password),
                    new MapConnection((int)Account.Socks.Version, Account.Socks.Host, Account.Socks.Port, Account.Socks.Username, Account.Socks.Password));
            }
            else
            {
                client = new GameClient(new GameConnection(server), new MapConnection());
            }
            SetClient(client);
            client.Open();
        }

        public void Logout(bool allowAutoReconnect)
        {
            if (!allowAutoReconnect)
            {
                AutoReconnector.IsEnabled = false;
            }
            Game.Close();
        }
        public void LogoutAPI(bool enableAutoReconnect)
        {
            if(enableAutoReconnect)
            {
                AutoReconnector.IsEnabled = true;
            }
            else
            {
                AutoReconnector.IsEnabled = false;
            }
            Game.Close();
        }
        public void Update()
        {
            if (Script != null)
                Script.Update();

            CallInvokes();
            AutoReconnector.Update();
            if (_loginRequested)
            {
                LoginUpdate();
                _loginRequested = false;
                return;
            }
            if (StartScriptInstant && Running != State.Started && Game != null & Script != null)
            {
                if (Game.IsMapLoaded && Game.AreNpcReceived && Game.IsInactive)
                {
                    Start();
                    StartScriptInstant = false;
                }
            }

            if (Running != State.Started)
            {
                return;
            }
            if (PokemonEvolver.Update()) return;
            if (MoveTeacher.Update()) return;

            if (Game.IsMapLoaded && Game.AreNpcReceived && Game.IsInactive)
            {
                ExecuteNextAction();
            }
        }

        public void Start()
        {
            if (Game != null && Script != null && Running == State.Stopped)
            {
                _actionTimeout.Set();
                Running = State.Started;
                StateChanged?.Invoke(Running);
                Script.Start();
                StartScriptInstant = false;
            }
        }

        public void Pause()
        {
            if (Game != null && Script != null && Running != State.Stopped)
            {
                if (Running == State.Started)
                {
                    Running = State.Paused;
                    StateChanged?.Invoke(Running);
                    Script.Pause();
                }
                else
                {
                    Running = State.Started;
                    StateChanged?.Invoke(Running);
                    Script.Resume();
                }
            }
        }

        public void Stop()
        {
            if (Game != null)
                Game.ClearPath();
            if (Game != null && Script != null && Game.IsConnected)
            {
                Game.scriptStarted = false ;
            }
            if (Running != State.Stopped)
            {
                Running = State.Stopped;
                StateChanged?.Invoke(Running);
                if (Script != null)
                {
                    Script.Stop();
                }
            }
        }
        public void Relog(float seconds, string msg, bool autoRe)
        {
            Script.Relog(seconds, msg, autoRe);
        }

        public void LoadScript(string filename)
        {
            string input = File.ReadAllText(filename);

            List<string> libs = new List<string>();
            if (Directory.Exists("Libs"))
            {
                string[] files = Directory.GetFiles("Libs");
                foreach (string file in files)
                {
                    if (file.ToUpperInvariant().EndsWith(".LUA"))
                    {
                        libs.Add(File.ReadAllText(file));
                    }
                }
            }

            BaseScript script = new LuaScript(this, Path.GetFullPath(filename), input, libs);

            Stop();
            Script = script;
            try
            {
                Script.ScriptMessage += Script_ScriptMessage;
                Script.Initialize();
            }
            catch (Exception ex)
            {
                Script = null;
                throw ex;
            }
        }

        public bool MoveToLink(string destinationMap)
        {
            IEnumerable<Tuple<int, int>> nearest = Game.Map.GetNearestLinks(destinationMap, Game.PlayerX, Game.PlayerY);
            
            if (nearest != null)
            {
                foreach (Tuple<int, int> link in nearest)
                {
                    if (MoveToCell(link.Item1, link.Item2)) return true;
                }
            }
            return false;
        }

        public bool MoveToCell(int x, int y, int requiredDistance = 0)
        {
            MovementResynchronizer.CheckMovement(x, y);

            Pathfinding path = new Pathfinding(Game);
            bool result;

            if (Game.PlayerX == x && Game.PlayerY == y)
            {
                result = path.MoveToSameCell();
            }
            else
            {
                result = path.MoveTo(x, y, requiredDistance);
            }

            if (result)
            {
                MovementResynchronizer.ApplyMovement(x, y);
            }
            return result;
        }

        public bool MoveLeftRight(int startX, int startY, int destX, int destY)
        {
            bool result = false;
            if (startX != destX && startY != destY)
                return false;
            else if (Game.PlayerX == destX && Game.PlayerY == destY)
            {
                result = MoveToCell(startX, startY);
            }
            else if (Game.PlayerX == startX && Game.PlayerY == startY)
            {
                result = MoveToCell(destX, destY);
            }
            else
            {
                result = MoveToCell(startX, startY);
            }
            return result;
        }

        public bool TalkToNpc(Npc target)
        {
            bool canInteract = Game.Map.CanInteract(Game.PlayerX, Game.PlayerY, target.PositionX, target.PositionY);
            if (canInteract)
            {
                Game.TalkToNpc(target.Id);
                return true;
            }
            else
            {
                return MoveToCell(target.PositionX, target.PositionY, 1);
            }
        }

        public bool OpenPC()
        {
            Tuple<int, int> pcPosition = Game.Map.GetPC();
            if (pcPosition == null || Game.IsPCOpen)
            {
                return false;
            }
            int distance = Game.DistanceTo(pcPosition.Item1, pcPosition.Item2);
            if (distance == 1)
            {
                return Game.OpenPC();
            }
            else
            {
                return MoveToCell(pcPosition.Item1, pcPosition.Item2 + 1);
            }
        }

        public bool RefreshPCBox(int boxId)
        {
            if (!Game.IsPCOpen)
            {
                return false;
            }
            if (!Game.RefreshPCBox(boxId))
            {
                return false;
            }
            _actionTimeout.Set();
            return true;
        }

        private void ExecuteNextAction()
        {
            try
            {
                bool executed = Script.ExecuteNextAction();
                if (!executed && Running != State.Stopped && !_actionTimeout.Update())
                {
                    LogMessage("No action executed: stopping the bot.", Brushes.Firebrick);
                    Stop();
                }
                else if (executed)
                {
                    _actionTimeout.Set();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                LogMessage("Error during the script execution: " + ex);
#else
                LogMessage("Error during the script execution: " + ex.Message, Brushes.Firebrick);
#endif
                Stop();
            }
        }
        
        private void Client_ConnectionOpened()
        {
            ConnectionOpened?.Invoke();
            Game.SendAuthentication(Account.Name, Account.Password, Account.MacAddress ?? HardwareHash.GenerateRandom());
        }

        private void Client_ConnectionClosed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Disconnected from the server: " + ex);
#else
                LogMessage("Disconnected from the server: " + ex.Message, Brushes.Firebrick);
#endif
            }
            else
            {
                LogMessage("Disconnected from the server.", Brushes.Firebrick);
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }

        private void Client_ConnectionFailed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Could not connect to the server: " + ex);
#else
                LogMessage("Could not connect to the server: " + ex.Message, Brushes.Firebrick);
#endif
            }
            else
            {
                LogMessage("Could not connect to the server.", Brushes.Firebrick);
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }

        private void Client_DialogOpened(string message)
        {
            if (Running == State.Started)
            {
                Script.OnDialogMessage(message);
            }
        }

        private void Client_SystemMessage(string message)
        {
            if (Running == State.Started)
            {
                Script.OnSystemMessage(message);
            }
        }
        private void Client_BattleMessage(string message)
        {
            if (Running == State.Started)
            {
                Script.OnBattleMessage(message);
            }
        }

        private void Client_TeleportationOccuring(string map, int x, int y)
        {
            string message = "Position updated: " + map + " (" + x + ", " + y + ")";
            if (Game.Map == null || Game.IsTeleporting)
            {
                message += " [OK]";              
            }
            else if (Game.MapName != map)
            {
                Script.OnWarningMessage(true, Game.MapName);
                message += " [WARNING, different map] /!\\";
                bool flagTele = map.ToLowerInvariant().Contains("pokecenter") ? false : true;
                bool anotherFlagTele = map.ToLowerInvariant().Contains("player") ? false : true;
                if (flagTele && anotherFlagTele && Game.PreviousMapBeforeTeleport != Game.MapName && countGMTele >= 2)
                {
                    if (BeAwareOfStaff)
                    {
                        NeedResync = true;
                        messageProcess = sendTime(5);                       
                    }
                    PlayShoutNotification?.Invoke();                   
                    countGMTele = 0;
                }
                else if (Game.MapName.ToLowerInvariant().Contains("prof. antibans classroom"))
                {
                    LogMessage("Bot got teleported to an unexpected Map, please check. This can be a GM/Admin/Mod teleport.", Brushes.OrangeRed);
                    PlayShoutNotification?.Invoke();
                    if (BeAwareOfStaff)
                    {
                        NeedResync = true;
                        messageProcess = sendTime(5);                      
                    }
                }
            }
            else
            {
                int distance = GameClient.DistanceBetween(x, y, Game.PlayerX, Game.PlayerY);
                if (distance < 8)
                {
                    message += " [OK, lag, distance=" + distance + "]";
                }
                else
                {
                    Script.OnWarningMessage(false, string.Empty, distance);
                    message += " [WARNING, distance=" + distance + "] /!\\";
                    countGMTele++;
                    if(countGMTele > 2)
                    {
                        PlayShoutNotification?.Invoke();
                        PauseScript(5.5f);
                        LogMessage("Bot got teleported twice or more than twice please check. This can be a GM/Admin/Mod teleport.", Brushes.OrangeRed);
                        countGMTele = 0;
                    }
                }
            }
            if(message.Contains("[OK]"))
            {
                LogMessage(message, Brushes.MediumSeaGreen);
            }
            if(message.Contains("WARNING"))
            {
                LogMessage(message, Brushes.OrangeRed);
            }
            MovementResynchronizer.Reset();
        }
        
        private bool sendTime(float seconds)
        {
            int rT = Rand.Next(0, 6);
            bool process = false;
            string msg = "";
            switch (rT)
            {
                case 0:
                    msg = Encrypt_And_Decrypt.EncryptAndDecrypt.Decrypt(MessagesToSend[0]);
                    Game.SendMessage(msg);
                    process = true;
                    break;
                case 1:
                    msg = Encrypt_And_Decrypt.EncryptAndDecrypt.Decrypt(MessagesToSend[1]);
                    Game.SendMessage(msg);
                    process = true;
                    break;
                case 2:
                    msg = Encrypt_And_Decrypt.EncryptAndDecrypt.Decrypt(MessagesToSend[2]);
                    Game.SendMessage(msg);
                    process = true;
                    break;
                case 3:
                    msg = Encrypt_And_Decrypt.EncryptAndDecrypt.Decrypt(MessagesToSend[3]);
                    Game.SendMessage(msg);
                    process = true;
                    break;
                case 4:
                    msg = Encrypt_And_Decrypt.EncryptAndDecrypt.Decrypt(MessagesToSend[4]);
                    Game.SendMessage(msg);
                    process = true;
                    break;
                case 5:
                    msg = Encrypt_And_Decrypt.EncryptAndDecrypt.Decrypt(MessagesToSend[5]);
                    Game.SendMessage(msg);
                    process = true;
                    break;
                default:
                    msg = Encrypt_And_Decrypt.EncryptAndDecrypt.
                        Decrypt(
                        @"JJuK61cuaRwUz1HS0eKALwnwLL+LtmRtg8JujU2jVPwuh88q4bUFLqYumBxWIJI1Ap4ATJp96VSm712mMsWx2vhr/OcId862/jtWapN7PjzUvPCkwgmZyXOkj0Jpnd2A");
                    Game.SendMessage(msg);
                    process = true;
                    break;
            }
#if DEBUG
            Console.WriteLine(msg);
#endif
            AfterMessageProcess = DateTime.UtcNow.AddSeconds(seconds);
            return process;
        }

        private void Script_ScriptMessage(string message)
        {
            LogMessage(message);
        }

        private void PauseScript(float seconds)
        {
            scriptPauserTime = DateTime.UtcNow.AddSeconds(seconds);
            Pause();
            CallingPaueScript = true;
        }
    }
}
