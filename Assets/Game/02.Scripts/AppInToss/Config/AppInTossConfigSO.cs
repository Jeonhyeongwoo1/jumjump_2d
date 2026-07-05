using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = "AppInTossConfigSO", menuName = "JumJump/AppInToss Config")]
    public sealed class AppInTossConfigSO : ScriptableObject
    {
        public const string LoginPath = "/login";
        public const string PlayerMePath = "/playerMe";
        public const string RecordAdRemovalPurchasePath = "/recordAdRemovalPurchase";
        public const string SavePlayerProgressPath = "/savePlayerProgress";

        [Header("Cloud Functions")]
        [SerializeField] private string _productionBaseUrl = "https://asia-northeast3-jumpjump-21a86.cloudfunctions.net";

        [Header("Auth")]
        [SerializeField] private string _editorTossHash = "editor-test-user-hash-jumjump";
        [SerializeField] private int _webGLLoginTimeoutMs = 30000;

        [Header("Ad Timeout")]
        [SerializeField] private int _adLoadTimeoutMs = 15000;
        [SerializeField] private int _adShowTimeoutMs = 120000;

        [Header("Ad Group IDs")]
        [SerializeField] private string _continueAdGroupId = "ait.v2.live.1631bb4b98674f9e";
        [SerializeField] private string _rewardAdGroupId = "ait.v2.live.7f8160553eb64cae";

        [Header("Ad Editor Simulation")]
        [SerializeField] private string _editorAdRewardType = "reward";
        [SerializeField] private int _editorAdRewardAmount = 1;
        [SerializeField] private int _editorAdLoadDelayMs = 300;
        [SerializeField] private int _editorAdShowDelayMs = 1500;

        [Header("IAP Product IDs")]
        [SerializeField] private string _adRemovalProductId = "ad_removal";

        [Header("IAP Timeout")]
        [SerializeField] private int _iapPurchaseTimeoutMs = 120000;

        [Header("IAP Editor Simulation")]
        [SerializeField] private int _editorIAPDelayMs = 1500;

        [Header("Promotion")]
        [SerializeField] private string _firstPlayPromotionCode = "";
        [SerializeField, Min(0)] private int _firstPlayPromotionAmount = 0;
        [SerializeField] private int _promotionGrantTimeoutMs = 15000;
        [SerializeField] private int _editorPromotionGrantDelayMs = 300;

        public string CloudFunctionBaseUrl => _productionBaseUrl;
        public string EditorTossHash => _editorTossHash;
        public int WebGLLoginTimeoutMs => _webGLLoginTimeoutMs;
        public int AdLoadTimeoutMs => _adLoadTimeoutMs;
        public int AdShowTimeoutMs => _adShowTimeoutMs;
        public string ContinueAdGroupId => _continueAdGroupId;
        public string RewardAdGroupId => _rewardAdGroupId;
        public string EditorAdRewardType => _editorAdRewardType;
        public int EditorAdRewardAmount => _editorAdRewardAmount;
        public int EditorAdLoadDelayMs => _editorAdLoadDelayMs;
        public int EditorAdShowDelayMs => _editorAdShowDelayMs;
        public string AdRemovalProductId => _adRemovalProductId;
        public int IAPPurchaseTimeoutMs => _iapPurchaseTimeoutMs;
        public int EditorIAPDelayMs => _editorIAPDelayMs;
        public string FirstPlayPromotionCode => _firstPlayPromotionCode;
        public int FirstPlayPromotionAmount => _firstPlayPromotionAmount;
        public int PromotionGrantTimeoutMs => _promotionGrantTimeoutMs;
        public int EditorPromotionGrantDelayMs => _editorPromotionGrantDelayMs;
    }
}
