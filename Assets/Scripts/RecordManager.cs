using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VRAimLab
{
    [System.Serializable]
    public class GameRecord
    {
        public string mode;
        public string difficulty;
        public int score;
        public int hits;
        public int shots;
        public float accuracy;
        public int maxCombo;
        public string date;
    }

    [System.Serializable]
    public class RecordList
    {
        public List<GameRecord> records = new List<GameRecord>();
    }

    public class RecordManager : MonoBehaviour
    {
        public static RecordManager Instance;
        private const string PREFS_KEY = "VRAimLab_Records_v1";
        private RecordList data = new RecordList();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            LoadRecords();
        }

        public void AddRecord(string mode, string difficulty, int score, int hits, int shots, int maxCombo)
        {
            GameRecord r = new GameRecord();
            r.mode = mode;
            r.difficulty = difficulty;
            r.score = score;
            r.hits = hits;
            r.shots = shots;
            r.accuracy = shots > 0 ? (hits / (float)shots) * 100f : 0f;
            r.maxCombo = maxCombo;
            r.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            data.records.Add(r);
            SaveRecords();
            Debug.Log($"[RecordManager] 记录已保存: {mode} / {difficulty} / Score={score}");
        }

        public List<GameRecord> GetRecords(string mode, string difficulty, int maxCount = 20)
        {
            var list = data.records.FindAll(r => r.mode == mode && r.difficulty == difficulty);
            if (list.Count > maxCount)
                list = list.GetRange(list.Count - maxCount, maxCount);
            return list;
        }

        public bool HasRecords(string mode, string difficulty)
        {
            return data.records.Exists(r => r.mode == mode && r.difficulty == difficulty);
        }

        void SaveRecords()
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }

        void LoadRecords()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                data = JsonUtility.FromJson<RecordList>(json);
                if (data == null) data = new RecordList();
                if (data.records == null) data.records = new List<GameRecord>();
            }
        }
    }
}
