mergeInto(LibraryManager.library, {
  AITAnalytics_EventLog: function (payloadJsonPtr) {
    var payloadJson = UTF8ToString(payloadJsonPtr);
    var payload = null;

    try {
      payload = JSON.parse(payloadJson);
    } catch (error) {
      console.error('[AITAnalytics] invalid payload json', error);
      return;
    }

    var sdk = window.AppsInToss;
    var eventLog = sdk && typeof sdk.eventLog === 'function' ? sdk.eventLog : null;
    if (!eventLog) {
      console.warn('[AITAnalytics] AppsInToss.eventLog is not ready', payload);
      return;
    }

    try {
      var result = eventLog.call(sdk, payload);
      if (result && typeof result.catch === 'function') {
        result.catch(function (error) {
          console.error('[AITAnalytics] eventLog failed', error);
        });
      }
    } catch (error) {
      console.error('[AITAnalytics] eventLog failed', error);
    }
  }
});
