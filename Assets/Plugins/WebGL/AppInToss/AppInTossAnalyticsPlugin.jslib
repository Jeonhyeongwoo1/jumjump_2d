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

    if (payload.Log_name && !payload.log_name) {
      payload.log_name = payload.Log_name;
    }
    if (payload.Log_type && !payload.log_type) {
      payload.log_type = payload.Log_type;
    }
    if (payload.Params && !payload.params) {
      payload.params = payload.Params;
    }

    var dispatch = function (retryCount) {
      var sdk = window.AppsInToss;
      var eventLog = sdk && typeof sdk.eventLog === 'function' ? sdk.eventLog : null;
      if (!eventLog) {
        if (retryCount < 20) {
          setTimeout(function () {
            dispatch(retryCount + 1);
          }, 250);
          return;
        }

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
    };

    dispatch(0);
  }
});
