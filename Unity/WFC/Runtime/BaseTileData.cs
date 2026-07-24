using System;
using UnityEngine;

namespace steph.Unity.WFC.Runtime
{
    [Serializable]
    public class BaseTileData : TileData
    {
        public bool Manual;
        [Range(0f,1f)] public float Probability = 1f;
        public GameObject Prefab;
    }
}
