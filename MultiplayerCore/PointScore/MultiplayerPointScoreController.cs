using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TootTallyMultiplayer.MultiplayerCore.PointScore
{
    public class MultiplayerPointScoreController : MonoBehaviour
    {
        private static Dictionary<int, MultiplayerPointScore> _idToPointScoreDict;
        private static List<SavedPointScore> _savedPointScoreList;
        private static GameObject _gameObject;
        private static bool _isInitialized;

        public void Awake()
        {
            _gameObject = gameObject;
            _idToPointScoreDict = new Dictionary<int, MultiplayerPointScore>();
            if (_savedPointScoreList != null)
            {
                _savedPointScoreList.ForEach(InitPointScore);
                _savedPointScoreList.Clear();
            }
            _isInitialized = true;
        }

        public static void AddScoreDebug() => AddScore(UnityEngine.Random.Range(1, 3000), UnityEngine.Random.Range(1, 1000000), UnityEngine.Random.value, UnityEngine.Random.Range(0, 1000), new int[] {0,1,2,3,4});

        public static void AddScore(int id, int score, float percent, int maxCombo, int[] noteTally)
        {
            if (_isInitialized)
                InitPointScore(id, score, percent, maxCombo, noteTally);
            else
            {
                _savedPointScoreList ??= new List<SavedPointScore>();
                _savedPointScoreList.Add(new SavedPointScore(id, score, percent, maxCombo, noteTally));
            }
        }

        private static void InitPointScore(SavedPointScore pointScore) => InitPointScore(pointScore.id, pointScore.score, pointScore.percent, pointScore.maxCombo, pointScore.noteTally);

        private static void InitPointScore(int id, int score, float percent, int maxCombo, int[] noteTally)
        {
            if (!_idToPointScoreDict.ContainsKey(id))
            {
                var user = MultiplayerController.GetUserFromLobby(id);
                if (user == null) return;

                var pointScore = MultiplayerGameObjectFactory.CreatePointScoreCard(_gameObject.transform, new Vector2(-250, 32 * _idToPointScoreDict.Count), $"{id}PointScore").AddComponent<MultiplayerPointScore>();
                pointScore.Initialize(id, user.username, score, percent, maxCombo, noteTally);
                _idToPointScoreDict.Add(id, pointScore);

                var ordered = _idToPointScoreDict.Select(x => x.Value).OrderByDescending(x => x.GetScore).ToArray();
                for (int i = 0; i < ordered.Length; i++)
                    ordered[i].SetPosition(i + 1, _idToPointScoreDict.Count);
            }
        }

        public static void ClearSavedScores() => _savedPointScoreList?.Clear();

        public void OnDestroy()
        {
            _idToPointScoreDict = null;
            _isInitialized = false;
        }

        public class SavedPointScore
        {
            public int id, score, maxCombo;
            public int[] noteTally;
            public float percent;

            public SavedPointScore(int id, int score, float percent, int maxCombo, int[] noteTally)
            {
                this.id = id;
                this.score = score;
                this.percent = percent;
                this.maxCombo = maxCombo;
                this.noteTally = noteTally;
            }
        }
    }
}
