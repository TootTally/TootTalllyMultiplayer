using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TootTallyMultiplayer.MultiplayerCore
{
    public class MultiplayerLiveScoreController : MonoBehaviour
    {
        private Dictionary<int, MultiplayerLiveScore> _idToLiveScoreDict;
        private bool _isInitialized;
        private float _timer;

        public void Awake()
        {
            _idToLiveScoreDict = new Dictionary<int, MultiplayerLiveScore>();
            _isInitialized = true;
        }

        public void Update()
        {
            if (!_isInitialized) return;

            _timer += Time.deltaTime;
            if (_timer > 1)
            {
                _timer = 0;
                _idToLiveScoreDict.OrderByDescending(x => x.Value.score);
                for (int i = 0; i < _idToLiveScoreDict.Count; i++)
                    _idToLiveScoreDict[i].SetPosition(i + 1);

            }
            //Rainbow Animation mayhaps?
        }

        public void UpdateLiveScore(int id, int score, int health, int combo)
        {
            if (!_idToLiveScoreDict.ContainsKey(id))
            {
                _idToLiveScoreDict.Add(id, new MultiplayerLiveScore(this));
            }
            _idToLiveScoreDict[id].UpdateScore(score, health, combo);
        }
    }
}
