mergeInto(LibraryManager.library, {
  AITAd_Load: function (adGroupIdPtr, wave) {
    var adGroupId = UTF8ToString(adGroupIdPtr);

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
    var sdk = window.AppsInToss && window.AppsInToss.GoogleAdMob;
    if (!sdk) {
      state.loadCompleted = true;
      state.loadError = 'apps_in_toss_sdk_not_ready';
      return;
    }

    if (sdk.loadAppsInTossAdMob.isSupported && !sdk.loadAppsInTossAdMob.isSupported()) {
      state.loadCompleted = true;
      state.loadError = 'ad_not_supported';
      return;
    }

    state._loadCleanup = sdk.loadAppsInTossAdMob({
      options: { adGroupId: adGroupId },
      onEvent: function (event) {
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
        state.loadError = error && error.message ? error.message : String(error);
        if (state._loadCleanup) {
          state._loadCleanup();
          state._loadCleanup = null;
        }
      },
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

  AITAd_Show: function (adGroupIdPtr, wave) {
    var adGroupId = UTF8ToString(adGroupIdPtr);
    var state = window.__aitAdState;

    if (!state) {
      return;
    }

    state.showCompleted = false;
    state.showError = '';
    state.hasReward = false;
    state.rewardType = '';
    state.rewardAmount = 0;

    var sdk = window.AppsInToss && window.AppsInToss.GoogleAdMob;
    if (!sdk) {
      state.showCompleted = true;
      state.showError = 'apps_in_toss_sdk_not_ready';
      return;
    }

    if (sdk.showAppsInTossAdMob.isSupported && !sdk.showAppsInTossAdMob.isSupported()) {
      state.showCompleted = true;
      state.showError = 'ad_not_supported';
      return;
    }

    state._showCleanup = sdk.showAppsInTossAdMob({
      options: { adGroupId: adGroupId },
      onEvent: function (event) {
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
            if (state._showCleanup) {
              state._showCleanup();
              state._showCleanup = null;
            }
            break;
        }
      },
      onError: function (error) {
        state.showCompleted = true;
        state.showError = error && error.message ? error.message : String(error);
        if (state._showCleanup) {
          state._showCleanup();
          state._showCleanup = null;
        }
      },
    });
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
