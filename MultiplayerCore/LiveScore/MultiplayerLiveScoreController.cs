using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
                var ordered = _idToLiveScoreDict.OrderByDescending(x => x.Value.GetScore);
                for (int i = 0; i < ordered.Count(); i++)
                    ordered.ElementAt(i).Value.SetPosition(i + 1, _idToLiveScoreDict.Count);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Plugin.Instance.ShowLiveScore.Value = !Plugin.Instance.ShowLiveScore.Value;
                foreach (var liveScore in _idToLiveScoreDict.Values)
                    liveScore.SetIsVisible(Plugin.Instance.ShowLiveScore.Value);
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
                liveScore.SetIsVisible(Plugin.Instance.ShowLiveScore.Value, false);
                _idToLiveScoreDict.Add(id, liveScore);
            }
            _idToLiveScoreDict[id].UpdateScore(score, combo, health);
        }


        public void OnUserQuit(int id)
        {
            if (!_idToLiveScoreDict.ContainsKey(id)) return;

            _idToLiveScoreDict[id].SetQuitUI();
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
