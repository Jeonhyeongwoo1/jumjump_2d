mergeInto(LibraryManager.library, {
  $AITAd_ResolveSdk: function () {
    var ait = window.AppsInToss;
    if (ait && typeof ait.loadFullScreenAd === 'function' && typeof ait.showFullScreenAd === 'function') {
      return {
        source: 'full_screen',
        load: ait.loadFullScreenAd,
        show: ait.showFullScreenAd,
      };
    }

    if (ait && ait.GoogleAdMob &&
        typeof ait.GoogleAdMob.loadAppsInTossAdMob === 'function' &&
        typeof ait.GoogleAdMob.showAppsInTossAdMob === 'function') {
      return {
        source: 'google_admob',
        load: ait.GoogleAdMob.loadAppsInTossAdMob,
        show: ait.GoogleAdMob.showAppsInTossAdMob,
      };
    }

    if (window.GoogleAdMob &&
        typeof window.GoogleAdMob.loadAppsInTossAdMob === 'function' &&
        typeof window.GoogleAdMob.showAppsInTossAdMob === 'function') {
      return {
        source: 'window_google_admob',
        load: window.GoogleAdMob.loadAppsInTossAdMob,
        show: window.GoogleAdMob.showAppsInTossAdMob,
      };
    }

    return null;
  },

  $AITAd_ToErrorMessage: function (error) {
    if (!error) {
      return 'unknown_ad_error';
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

  $AITAd_IsSupported: function (fn) {
    if (!fn || typeof fn !== 'function') {
      return false;
    }

    if (typeof fn.isSupported !== 'function') {
      return true;
    }

    return fn.isSupported();
  },

  $AITAd_WaitForSdk: function (onReady, onFailed) {
    var startedAt = Date.now();
    var timeoutMs = 3000;

    function check() {
      var sdk = AITAd_ResolveSdk();
      if (sdk) {
        onReady(sdk);
        return;
      }

      if (Date.now() - startedAt >= timeoutMs) {
        onFailed('apps_in_toss_sdk_not_ready');
        return;
      }

      setTimeout(check, 100);
    }

    check();
  },

  $AITAd_ResumeAudioContexts: function () {
    var contexts = [];
    var module = typeof Module !== 'undefined' ? Module : null;

    function addContext(context) {
      if (context && typeof context.resume === 'function' && contexts.indexOf(context) < 0) {
        contexts.push(context);
      }
    }

    if (typeof WEBAudio !== 'undefined') {
      addContext(WEBAudio.audioContext);
    }

    if (module) {
      addContext(module.ctx);
      addContext(module.audioContext);
      addContext(module.webAudioContext);
      if (module.Sound) {
        addContext(module.Sound.audioContext);
      }
    }

    if (window.unityInstance && window.unityInstance.Module) {
      addContext(window.unityInstance.Module.ctx);
      addContext(window.unityInstance.Module.audioContext);
      addContext(window.unityInstance.Module.webAudioContext);
      if (window.unityInstance.Module.Sound) {
        addContext(window.unityInstance.Module.Sound.audioContext);
      }
    }

    for (var i = 0; i < contexts.length; i += 1) {
      try {
        if (contexts[i].state !== 'running' && contexts[i].state !== 'closed') {
          var resumeResult = contexts[i].resume();
          if (resumeResult && typeof resumeResult.catch === 'function') {
            resumeResult.catch(function (resumeError) {
              console.warn('[AITAd] audio resume failed:', resumeError);
            });
          }
        }
      } catch (error) {
        console.warn('[AITAd] audio resume failed:', error);
      }
    }
  },

  AITAd_Load__deps: ['$AITAd_ResolveSdk', '$AITAd_ToErrorMessage', '$AITAd_IsSupported', '$AITAd_WaitForSdk'],
  AITAd_Load: function (adGroupIdPtr, wave) {
    var adGroupId = UTF8ToString(adGroupIdPtr).trim();

    window.__aitAdState = {
      loadCompleted: false,
      loadError: '',
      showCompleted: false,
      showError: '',
      hasReward: false,
      rewardType: '',
      rewardAmount: 0,
      _loadCleanup: null,
      _showCleanup: null,
    };

    var state = window.__aitAdState;

    if (!adGroupId) {
      state.loadCompleted = true;
      state.loadError = 'missing_ad_group_id';
      return;
    }

    AITAd_WaitForSdk(function (sdk) {
      try {
        if (!AITAd_IsSupported(sdk.load)) {
          state.loadCompleted = true;
          state.loadError = 'ad_not_supported';
          console.warn('[AITAd] load not supported:', sdk.source);
          return;
        }

        console.log('[AITAd] load requested:', sdk.source, adGroupId);
        state._loadCleanup = sdk.load({
          options: { adGroupId: adGroupId },
          onEvent: function (event) {
            console.log('[AITAd] load event:', event && event.type ? event.type : event);
            if (event.type === 'loaded') {
              state.loadCompleted = true;
              if (state._loadCleanup) {
                state._loadCleanup();
                state._loadCleanup = null;
              }
            }
          },
          onError: function (error) {
            state.loadCompleted = true;
            state.loadError = AITAd_ToErrorMessage(error);
            console.warn('[AITAd] load error:', state.loadError);
            if (state._loadCleanup) {
              state._loadCleanup();
              state._loadCleanup = null;
            }
          },
        });
      } catch (error) {
        state.loadCompleted = true;
        state.loadError = AITAd_ToErrorMessage(error);
        console.warn('[AITAd] load exception:', state.loadError);
      }
    }, function (error) {
      state.loadCompleted = true;
      state.loadError = error;
      console.warn('[AITAd] sdk not ready for load:', error);
    });
  },

  AITAd_IsLoadCompleted: function () {
    var state = window.__aitAdState;
    return (state && state.loadCompleted) ? 1 : 0;
  },

  AITAd_GetLoadError: function () {
    var state = window.__aitAdState;
    return stringToNewUTF8(state ? (state.loadError || '') : '');
  },

  AITAd_Show__deps: ['$AITAd_ResolveSdk', '$AITAd_ToErrorMessage', '$AITAd_IsSupported', '$AITAd_WaitForSdk', '$AITAd_ResumeAudioContexts'],
  AITAd_Show: function (adGroupIdPtr, wave) {
    var adGroupId = UTF8ToString(adGroupIdPtr).trim();
    var state = window.__aitAdState;

    if (!state) {
      return;
    }

    state.showCompleted = false;
    state.showError = '';
    state.hasReward = false;
    state.rewardType = '';
    state.rewardAmount = 0;

    if (!adGroupId) {
      state.showCompleted = true;
      state.showError = 'missing_ad_group_id';
      AITAd_ResumeAudioContexts();
      return;
    }

    AITAd_WaitForSdk(function (sdk) {
      try {
        if (!AITAd_IsSupported(sdk.show)) {
          state.showCompleted = true;
          state.showError = 'ad_not_supported';
          console.warn('[AITAd] show not supported:', sdk.source);
          AITAd_ResumeAudioContexts();
          return;
        }

        console.log('[AITAd] show requested:', sdk.source, adGroupId);
        state._showCleanup = sdk.show({
          options: { adGroupId: adGroupId },
          onEvent: function (event) {
            console.log('[AITAd] show event:', event && event.type ? event.type : event);
            switch (event.type) {
              case 'userEarnedReward':
                state.hasReward = true;
                state.rewardType = (event.data && event.data.unitType) ? event.data.unitType : '';
                state.rewardAmount = (event.data && event.data.unitAmount) ? event.data.unitAmount : 0;
                break;

              case 'dismissed':
              case 'failedToShow':
                if (event.type === 'failedToShow') {
                  state.showError = 'ad_failed_to_show';
                }
                state.showCompleted = true;
                AITAd_ResumeAudioContexts();
                if (state._showCleanup) {
                  state._showCleanup();
                  state._showCleanup = null;
                }
                break;
            }
          },
          onError: function (error) {
            state.showCompleted = true;
            state.showError = AITAd_ToErrorMessage(error);
            console.warn('[AITAd] show error:', state.showError);
            AITAd_ResumeAudioContexts();
            if (state._showCleanup) {
              state._showCleanup();
              state._showCleanup = null;
            }
          },
        });
      } catch (error) {
        state.showCompleted = true;
        state.showError = AITAd_ToErrorMessage(error);
        console.warn('[AITAd] show exception:', state.showError);
        AITAd_ResumeAudioContexts();
      }
    }, function (error) {
      state.showCompleted = true;
      state.showError = error;
      console.warn('[AITAd] sdk not ready for show:', error);
      AITAd_ResumeAudioContexts();
    });
  },

  AITAd_ResumeAudio__deps: ['$AITAd_ResumeAudioContexts'],
  AITAd_ResumeAudio: function () {
    AITAd_ResumeAudioContexts();
  },

  AITAd_IsShowCompleted: function () {
    var state = window.__aitAdState;
    return (state && state.showCompleted) ? 1 : 0;
  },

  AITAd_GetShowError: function () {
    var state = window.__aitAdState;
    return stringToNewUTF8(state ? (state.showError || '') : '');
  },

  AITAd_HasReward: function () {
    var state = window.__aitAdState;
    return (state && state.hasReward) ? 1 : 0;
  },

  AITAd_GetRewardType: function () {
    var state = window.__aitAdState;
    return stringToNewUTF8(state ? (state.rewardType || '') : '');
  },

  AITAd_GetRewardAmount: function () {
    var state = window.__aitAdState;
    return (state && state.rewardAmount) ? state.rewardAmount : 0;
  },
});
