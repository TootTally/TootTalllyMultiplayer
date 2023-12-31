using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TootTallyCore.Graphics;
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
                var ordered = _idToLiveScoreDict.Select(x => x.Value).OrderByDescending(x => x.GetScore).ToArray();
                for (int i = 0; i < ordered.Length; i++)
                    ordered[i].SetPosition(i + 1, _idToLiveScoreDict.Count);
            }
        }

        public void UpdateLiveScore(int id, int score, int combo, int health)
        {
            if (!_isInitialized) return;

            if (!_idToLiveScoreDict.ContainsKey(id))
            {
                var user = MultiplayerController.GetUserFromLobby(id);
                if (user == null) return;

                var liveScore = MultiplayerGameObjectFactory.CreateLiveScoreCard(gameObject.transform, new Vector2(200, 32 * _idToLiveScoreDict.Count), $"{id}LiveScore").AddComponent<MultiplayerLiveScore>();
                liveScore.Initialize(id, user.username, this);
                _idToLiveScoreDict.Add(id, liveScore);
            }
            _idToLiveScoreDict[id].UpdateScore(score, combo, health);
        }
        
        public void OnDestroy()
        {
            _idToLiveScoreDict.Clear();
            _isInitialized = false;
        }

        public void Remove(int id)
        {
            _idToLiveScoreDict.Remove(id);
        }
    }
}
