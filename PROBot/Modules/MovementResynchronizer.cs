namespace PROBot.Modules
{
    public class MovementResynchronizer
    {
        private BotClient _bot;
        private int _lastMovementSourceX;
        private int _lastMovementSourceY;
        private int _lastMovementDestinationX;
        private int _lastMovementDestinationY;
        private bool _requestedResync;

        public MovementResynchronizer(BotClient bot)
        {
            _bot = bot;
            _bot.StateChanged += Bot_StateChanged;
            _bot.ClientChanged += Bot_ClientChanged;
        }

        private void Bot_StateChanged(BotClient.State state)
        {
            if (state == BotClient.State.Started)
            {
                Reset();
            }
        }

        private void Bot_ClientChanged()
        {
            if (_bot.Game != null)
            {
                _bot.Game.BattleEnded += Game_BattleEnded;
                _bot.Game.DialogOpened += Game_DialogOpened;
            }
        }

        private void Game_BattleEnded()
        {
            Reset();
        }

        private void Game_DialogOpened(string message)
        {
            Reset();
        }

        public bool CheckMovement(int x, int y)
        {
            if (_lastMovementSourceX == _bot.Game.PlayerX && _lastMovementSourceY == _bot.Game.PlayerY
                && _lastMovementDestinationX == x && _lastMovementDestinationY == y)
            {
                if (_requestedResync)
                {
                    _bot.LogMessage("Bot still stuck.", System.Windows.Media.Brushes.OrangeRed);
                    if(_bot.Account.Socks.Version != SocksVersion.None)
                        _bot.Relog(5, "Relogging since the bot is stuck", true);
                    else
                        _bot.Relog(5, "Relogging since the bot is stuck", false);
                }
                else
                {
                    _bot.LogMessage("Bot stuck, sending resynchronization request.", System.Windows.Media.Brushes.OrangeRed);
                    _requestedResync = true;
                    _bot.Game.RequestResync();
                }
                return false;
            }
            return true;
        }

        public void ApplyMovement(int x, int y)
        {
            _lastMovementSourceX = _bot.Game.PlayerX;
            _lastMovementSourceY = _bot.Game.PlayerY;
            _lastMovementDestinationX = x;
            _lastMovementDestinationY = y;
        }

        public void Reset()
        {
            _requestedResync = false;
            _bot.NeedResync = false;
            _lastMovementSourceX = -1;
        }
    }
}
