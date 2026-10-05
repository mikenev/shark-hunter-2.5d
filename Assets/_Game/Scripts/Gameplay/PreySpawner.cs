using System;
using UnityEngine;

namespace SharkHunter
{
    /// <summary>Keeps a target number of each prey type alive, spawning replacements away from the shark.</summary>
    public class PreySpawner : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public PreyFish prefab;
            public int targetCount = 6;
        }

        public Entry[] entries;
        public Transform shark;
        public PlayArea area;
        public float minDistanceFromShark = 12f;
        public float checkInterval = 1f;

        float timer;

        void Start()
        {
            if (area == null) area = FindFirstObjectByType<PlayArea>();
            if (shark == null) { var s = FindFirstObjectByType<SharkController>(); if (s != null) shark = s.transform; }
            foreach (var e in entries) for (int i = 0; i < e.targetCount; i++) Spawn(e.prefab, 6f);
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = checkInterval;

            foreach (var e in entries)
            {
                int alive = 0;
                foreach (var p in FindObjectsByType<PreyFish>(FindObjectsSortMode.None))
                    if (p.definition == e.prefab.definition) alive++;
                if (alive < e.targetCount) Spawn(e.prefab, minDistanceFromShark);
            }
        }

        void Spawn(PreyFish prefab, float minDistance)
        {
            Vector2 pos = default;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                pos = new Vector2(UnityEngine.Random.Range(area.min.x + 1f, area.max.x - 1f),
                                  UnityEngine.Random.Range(area.min.y + 0.8f, area.max.y - 0.8f));
                if (shark == null || Vector2.Distance(pos, shark.position) >= minDistance) break;
            }
            var fish = Instantiate(prefab, new Vector3(pos.x, pos.y, area.PlaneZ), Quaternion.identity, transform);
            fish.Initialize(shark, area);
        }
    }
}
