using System;
using UnityEngine;

namespace JumJump.Data
{
    [Serializable]
    public struct BackgroundDepthLayer
    {
        [SerializeField] private float _startHeight;
        [SerializeField] private Sprite[] _sprites;

        public float StartHeight => _startHeight;
        public Sprite[] Sprites => _sprites;
    }
}
