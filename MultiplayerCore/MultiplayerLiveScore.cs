using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TootTallyMultiplayer.MultiplayerCore
{
    public class MultiplayerLiveScore
    {
        private MultiplayerLiveScoreController _controller;
        public int score, health, combo;
        public int updateCount;
        private int _position;

        public MultiplayerLiveScore(MultiplayerLiveScoreController controller)
        {
            _controller = controller;
        }

        public void UpdateScore(int score, int health, int combo)
        {
            this.score = score;
            this.health = health;
            this.combo = combo;
            updateCount++;
        }
        
        public void SetPosition(int position)
        {
            _position = position;
        }
    }
}
