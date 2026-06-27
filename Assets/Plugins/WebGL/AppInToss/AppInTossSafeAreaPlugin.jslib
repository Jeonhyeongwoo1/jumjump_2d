mergeInto(LibraryManager.library, {

  // 앱인토스 SDK(@apps-in-toss/web-framework)의 Safe Area 인셋을 읽습니다.
  // SafeAreaInsets.get()은 동기 함수이며 CSS 픽셀 단위의 { top, bottom, left, right }를 반환합니다.
  // Unity의 Screen.width/height는 device 픽셀이므로, canvas 실측 DPR을 곱해 device 픽셀로 변환해 반환합니다.

  // canvas drawing buffer(device px) / CSS 폭 = Unity가 실제 사용한 DPR
  $AITSafeArea_Dpr: function () {
    try {
      var c = (typeof Module !== 'undefined' && Module.canvas)
        ? Module.canvas
        : document.querySelector('#unity-canvas');
      if (c) {
        var rect = c.getBoundingClientRect();
        if (rect && rect.width > 0) {
          return c.width / rect.width;
        }
      }
    } catch (e) {}
    return window.devicePixelRatio || 1;
  },

  $AITSafeArea_Get: function () {
    try {
      var ait = window.AppsInToss;
      if (ait && ait.SafeAreaInsets && typeof ait.SafeAreaInsets.get === 'function') {
        return ait.SafeAreaInsets.get();
      }
    } catch (e) {}
    return null;
  },

  AITSafeArea_IsReady: function () {
    var ait = window.AppsInToss;
    return (ait && ait.SafeAreaInsets && typeof ait.SafeAreaInsets.get === 'function') ? 1 : 0;
  },

  AITSafeArea_GetTop__deps: ['$AITSafeArea_Get', '$AITSafeArea_Dpr'],
  AITSafeArea_GetTop: function () {
    var i = AITSafeArea_Get();
    return i ? (i.top || 0) * AITSafeArea_Dpr() : 0;
  },

  AITSafeArea_GetBottom__deps: ['$AITSafeArea_Get', '$AITSafeArea_Dpr'],
  AITSafeArea_GetBottom: function () {
    var i = AITSafeArea_Get();
    return i ? (i.bottom || 0) * AITSafeArea_Dpr() : 0;
  },

  AITSafeArea_GetLeft__deps: ['$AITSafeArea_Get', '$AITSafeArea_Dpr'],
  AITSafeArea_GetLeft: function () {
    var i = AITSafeArea_Get();
    return i ? (i.left || 0) * AITSafeArea_Dpr() : 0;
  },

  AITSafeArea_GetRight__deps: ['$AITSafeArea_Get', '$AITSafeArea_Dpr'],
  AITSafeArea_GetRight: function () {
    var i = AITSafeArea_Get();
    return i ? (i.right || 0) * AITSafeArea_Dpr() : 0;
  }

});
