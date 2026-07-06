mergeInto(LibraryManager.library, {
  $AITPromotion_ResolveSdk: function () {
    var ait = window.AppsInToss;
    if (!ait) {
      return null;
    }

    if (typeof ait.grantPromotionRewardForGame === 'function') {
      return {
        source: 'grantPromotionRewardForGame',
        grant: ait.grantPromotionRewardForGame,
      };
    }

    if (typeof ait.grantPromotionReward === 'function') {
      return {
        source: 'grantPromotionReward',
        grant: ait.grantPromotionReward,
      };
    }

    return null;
  },

  $AITPromotion_ToErrorMessage: function (error) {
    if (!error) {
      return 'unknown_promotion_error';
    }

    if (error.message) {
      return error.message;
    }

    try {
      return JSON.stringify(error);
    } catch (e) {
      return String(error);
    }
  },

  $AITPromotion_WaitForSdk: function (onReady, onFailed) {
    var startedAt = Date.now();
    var timeoutMs = 3000;

    function check() {
      var sdk = AITPromotion_ResolveSdk();
      if (sdk) {
        onReady(sdk);
        return;
      }

      if (Date.now() - startedAt >= timeoutMs) {
        onFailed('apps_in_toss_promotion_sdk_not_ready');
        return;
      }

      setTimeout(check, 100);
    }

    check();
  },

  AITPromotion_Grant__deps: ['$AITPromotion_ResolveSdk', '$AITPromotion_ToErrorMessage', '$AITPromotion_WaitForSdk'],
  AITPromotion_Grant: function (promotionCodePtr, amount) {
    var promotionCode = UTF8ToString(promotionCodePtr).trim();

    window.__aitPromotionState = {
      completed: false,
      resultJson: '',
      error: '',
    };

    var state = window.__aitPromotionState;

    if (!promotionCode) {
      state.completed = true;
      state.error = 'missing_promotion_code';
      return;
    }

    if (amount <= 0) {
      state.completed = true;
      state.error = 'invalid_promotion_amount';
      return;
    }

    AITPromotion_WaitForSdk(function (sdk) {
      try {
        console.log('[AITPromotion] grant requested:', sdk.source, promotionCode, amount);
        Promise.resolve(sdk.grant({
          params: {
            promotionCode: promotionCode,
            amount: amount,
          },
        })).then(function (result) {
          if (!result) {
            state.resultJson = JSON.stringify({
              success: false,
              unsupported: true,
            });
            state.completed = true;
            return;
          }

          if (result === 'ERROR') {
            state.resultJson = JSON.stringify({
              success: false,
              errorCode: 'ERROR',
              message: 'unknown_promotion_error',
            });
            state.completed = true;
            return;
          }

          if (result.key) {
            state.resultJson = JSON.stringify({
              success: true,
              key: result.key,
            });
            state.completed = true;
            return;
          }

          if (result.errorCode) {
            state.resultJson = JSON.stringify({
              success: false,
              errorCode: result.errorCode,
              message: result.message || '',
            });
            state.completed = true;
            return;
          }

          state.error = 'invalid_promotion_response';
          state.completed = true;
        }).catch(function (error) {
          state.error = AITPromotion_ToErrorMessage(error);
          state.completed = true;
          console.warn('[AITPromotion] grant error:', state.error);
        });
      } catch (error) {
        state.error = AITPromotion_ToErrorMessage(error);
        state.completed = true;
        console.warn('[AITPromotion] grant exception:', state.error);
      }
    }, function (error) {
      state.error = error;
      state.completed = true;
      console.warn('[AITPromotion] sdk not ready:', error);
    });
  },

  AITPromotion_IsGrantCompleted: function () {
    var state = window.__aitPromotionState;
    return (state && state.completed) ? 1 : 0;
  },

  AITPromotion_GetGrantResultJson: function () {
    var state = window.__aitPromotionState;
    return stringToNewUTF8(state ? (state.resultJson || '') : '');
  },

  AITPromotion_GetGrantError: function () {
    var state = window.__aitPromotionState;
    return stringToNewUTF8(state ? (state.error || '') : '');
  },
});
