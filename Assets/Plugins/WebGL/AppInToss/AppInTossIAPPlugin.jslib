mergeInto(LibraryManager.library, {

  // ─── Purchase ────────────────────────────────────────────────────────────────
  // sku 에 해당하는 상품 일회성 결제를 시작합니다.
  // IsPurchaseCompleted()로 완료 여부를 폴링합니다.
  //
  // 공식 API: window.AppsInToss.IAP.createOneTimePurchaseOrder(...)
  // 참고: https://developers-apps-in-toss.toss.im/iap/develop.html
  AITIA_Purchase: function (skuPtr) {
    var sku = UTF8ToString(skuPtr);

    window.__aitIapState = {
      completed: false,
      resultJson: '',
      error: '',
      _cleanup: null
    };

    var state = window.__aitIapState;
    var sdk = window.AppsInToss && window.AppsInToss.IAP;

    if (!sdk || typeof sdk.createOneTimePurchaseOrder !== 'function') {
      state.error = 'apps_in_toss_iap_not_supported';
      state.completed = true;
      return;
    }

    state._cleanup = sdk.createOneTimePurchaseOrder({
      options: {
        sku: sku,
        processProductGrant: function (result) {
          // 결제 승인 후 상품 지급 처리 — orderId를 저장하고 true 반환
          state.resultJson = JSON.stringify({
            orderId: result.orderId,
            productId: sku,
            purchasedAtMillis: Date.now()
          });
          return true;
        }
      },
      onEvent: function (event) {
        // processProductGrant 완료 후 발생 — 결제 흐름 종료
        state.completed = true;
        if (state._cleanup) {
          state._cleanup();
          state._cleanup = null;
        }
      },
      onError: function (error) {
        if (error && error.code === 'USER_CANCEL') {
          state.error = 'cancelled';
        } else {
          state.error = error && error.message ? error.message : String(error);
        }
        state.completed = true;
        if (state._cleanup) {
          state._cleanup();
          state._cleanup = null;
        }
      }
    });
  },

  AITIA_IsPurchaseCompleted: function () {
    var state = window.__aitIapState;
    return (state && state.completed) ? 1 : 0;
  },

  AITIA_GetPurchaseResultJson: function () {
    var state = window.__aitIapState;
    return stringToNewUTF8(state ? (state.resultJson || '') : '');
  },

  AITIA_GetPurchaseError: function () {
    var state = window.__aitIapState;
    return stringToNewUTF8(state ? (state.error || '') : '');
  }
});
