using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace CustomsQueueBot
{
    struct RecentPulledList
    {
        List<Player> _recent;
        Timer _timer;

        public RecentPulledList(List<Player> players)
        {
            _recent = players;
            _timer  = new Timer(60000);
        }

        public void StartTimer()
        {
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (Player player in _recent)
            {
                var user = player.GuildUser;
                var role = user.Guild.Roles.FirstOrDefault(r => r.Name == "Pulled");
                await user.RemoveRoleAsync(role);
            }


        }
    }
}
