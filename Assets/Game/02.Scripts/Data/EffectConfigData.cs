using JumJump.Service;
using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(EffectConfigData), menuName = "JumJump/Effect Config Data")]
    public sealed class EffectConfigData : ScriptableObject
    {
        public HayLandingFX HayLandingFXPrefab => _hayLandingFXPrefab;
        public string HayLandingFXPoolKey => _hayLandingFXPoolKey;
        public int HayLandingFXPoolCount => _hayLandingFXPoolCount;
        public Vector3 HayLandingFXOffset => _hayLandingFXOffset;

        [Header("Hay Landing FX")]
        [SerializeField] private HayLandingFX _hayLandingFXPrefab;
        [SerializeField] private string _hayLandingFXPoolKey = "FX_HayLanding";
        [SerializeField] private int _hayLandingFXPoolCount = 8;
        [SerializeField] private Vector3 _hayLandingFXOffset = new Vector3(0f, 0.1f, 0f);
    }
}
