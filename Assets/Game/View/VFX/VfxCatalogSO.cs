using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.View.VFX
{
    [CreateAssetMenu(fileName = "VfxCatalog", menuName = "Game/VFX/VFX Catalog")]
    public sealed class VfxCatalogSO : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private VfxId id;
            [SerializeField] private GameObject prefab;
            [SerializeField, Min(0)] private int prewarmCount = 1;
            [SerializeField, Min(0f)] private float fadeDelay;
            [SerializeField, Min(0f)] private float fadeDuration;
            [SerializeField, Min(0f)] private float scaleMultiplier = 1f;

            public VfxId Id => id;
            public GameObject Prefab => prefab;
            public int PrewarmCount => prewarmCount;
            public float FadeDelay => fadeDelay;
            public float FadeDuration => fadeDuration;
            public float ScaleMultiplier => scaleMultiplier;
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(VfxId id, out Entry entry)
        {
            if (entries != null)
            {
                foreach (Entry candidate in entries)
                {
                    if (candidate != null && candidate.Id == id)
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }

            entry = null;
            return false;
        }
    }
}
