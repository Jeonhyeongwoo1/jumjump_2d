using JumJump.Controller;
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
        public string JumpBoxBoostFXAddressableKey => _jumpBoxBoostFXAddressableKey;
        public string JumpBoxBoostFXPoolKey => _jumpBoxBoostFXPoolKey;
        public int JumpBoxBoostFXPoolCount => _jumpBoxBoostFXPoolCount;
        public Vector3 JumpBoxBoostFXOffset => _jumpBoxBoostFXOffset;
        public string CoinCollectFXAddressableKey => _coinCollectFXAddressableKey;
        public string CoinCollectFXPoolKey => _coinCollectFXPoolKey;
        public int CoinCollectFXPoolCount => _coinCollectFXPoolCount;
        public Vector3 CoinCollectFXOffset => _coinCollectFXOffset;
        public BestScoreBreakFX BestScoreBreakFXPrefab => _bestScoreBreakFXPrefab;
        public string BestScoreBreakFXPoolKey => _bestScoreBreakFXPoolKey;
        public int BestScoreBreakFXPoolCount => _bestScoreBreakFXPoolCount;
        public Vector3 BestScoreBreakFXOffset => _bestScoreBreakFXOffset;
        public Sprite ComboPlatformAuraSprite => _comboPlatformAuraSprite;
        public Sprite ComboPlatformPulseSprite => _comboPlatformPulseSprite;
        public Sprite ComboPlatformSparkleSprite => _comboPlatformSparkleSprite;
        public string ComboPlatformAuraFXPoolKey => _comboPlatformAuraFXPoolKey;
        public string ComboPlatformBurstFXPoolKey => _comboPlatformBurstFXPoolKey;
        public int ComboPlatformAuraFXPoolCount => _comboPlatformAuraFXPoolCount;
        public int ComboPlatformBurstFXPoolCount => _comboPlatformBurstFXPoolCount;
        public int ComboPlatformAuraMaxActiveCount => _comboPlatformAuraMaxActiveCount;
        public Vector3 ComboPlatformFXOffset => _comboPlatformFXOffset;

        [Header("Hay Landing FX")]
        [SerializeField] private HayLandingFX _hayLandingFXPrefab;
        [SerializeField] private string _hayLandingFXPoolKey = "FX_HayLanding";
        [SerializeField] private int _hayLandingFXPoolCount = 8;
        [SerializeField] private Vector3 _hayLandingFXOffset = new Vector3(0f, 0.1f, 0f);

        [Header("Jump Box Boost FX")]
        [SerializeField] private string _jumpBoxBoostFXAddressableKey = "JumpBoxBoostFX";
        [SerializeField] private string _jumpBoxBoostFXPoolKey = "FX_JumpBoxBoost";
        [SerializeField] private int _jumpBoxBoostFXPoolCount = 4;
        [SerializeField] private Vector3 _jumpBoxBoostFXOffset = new Vector3(0f, 0.1f, 0f);

        [Header("Coin Collect FX")]
        [SerializeField] private string _coinCollectFXAddressableKey = "PF_CoinCollectFX";
        [SerializeField] private string _coinCollectFXPoolKey = "FX_CoinCollect";
        [SerializeField] private int _coinCollectFXPoolCount = 4;
        [SerializeField] private Vector3 _coinCollectFXOffset = new Vector3(0f, 0.66f, 0f);

        [Header("Best Score Break FX")]
        [SerializeField] private BestScoreBreakFX _bestScoreBreakFXPrefab;
        [SerializeField] private string _bestScoreBreakFXPoolKey = "FX_BestScoreBreak";
        [SerializeField] private int _bestScoreBreakFXPoolCount = 2;
        [SerializeField] private Vector3 _bestScoreBreakFXOffset = new Vector3(0f, 1.05f, 0f);

        [Header("Combo Platform FX")]
        [SerializeField] private Sprite _comboPlatformAuraSprite;
        [SerializeField] private Sprite _comboPlatformPulseSprite;
        [SerializeField] private Sprite _comboPlatformSparkleSprite;
        [SerializeField] private string _comboPlatformAuraFXPoolKey = "FX_ComboPlatformAura";
        [SerializeField] private string _comboPlatformBurstFXPoolKey = "FX_ComboPlatformBurst";
        [SerializeField] private int _comboPlatformAuraFXPoolCount = 12;
        [SerializeField] private int _comboPlatformBurstFXPoolCount = 6;
        [SerializeField] private int _comboPlatformAuraMaxActiveCount = 16;
        [SerializeField] private Vector3 _comboPlatformFXOffset = new Vector3(0f, 0.08f, 0f);
    }
}
