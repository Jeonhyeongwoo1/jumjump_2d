mergeInto(LibraryManager.library, {
  AITAuth_StartLogin: function(loginUrlPtr) {
    var loginUrl = UTF8ToString(loginUrlPtr);
    var state = window.__aitAuthState = {
      completed: false,
      resultJson: '',
      error: ''
    };

    function waitForAppsInTossSdk() {
      return new Promise(function(resolve) {
        var attempts = 0;
        var maxAttempts = 50;

        function check() {
          attempts++;

          if (window.AppsInToss &&
              (typeof window.AppsInToss.getAnonymousKey === 'function' ||
               typeof window.AppsInToss.getUserKeyForGame === 'function')) {
            resolve(window.AppsInToss);
            return;
          }

          if (attempts >= maxAttempts) {
            resolve(null);
            return;
          }

          setTimeout(check, 100);
        }

        check();
      });
    }

    function isProduction() {
      return window.IS_PRODUCTION === true;
    }

    function hasReactNativeBridge() {
      return !!window.ReactNativeWebView;
    }

    function isLocalDevelopmentHost() {
      var hostname = window.location && window.location.hostname ? window.location.hostname : '';
      return hostname === 'localhost' ||
        hostname === '127.0.0.1' ||
        hostname === '::1' ||
        /^192\.168\./.test(hostname) ||
        /^10\./.test(hostname) ||
        /^172\.(1[6-9]|2[0-9]|3[0-1])\./.test(hostname);
    }

    function resolveDevTossHash() {
      if (isProduction() || !isLocalDevelopmentHost()) {
        throw new Error('apps_in_toss_sdk_not_ready');
      }

      var key = 'ait_dev_toss_hash';
      var storedHash = localStorage.getItem(key);
      if (storedHash) {
        return storedHash;
      }

      var generatedHash = 'dev-webgl-user-' + Math.random().toString(36).slice(2);
      localStorage.setItem(key, generatedHash);
      return generatedHash;
    }

    async function resolveTossHash() {
      var appsInToss = await waitForAppsInTossSdk();
      if (appsInToss) {
        if (!hasReactNativeBridge()) {
          return resolveDevTossHash();
        }

        var getUserKey = typeof appsInToss.getAnonymousKey === 'function'
          ? appsInToss.getAnonymousKey
          : appsInToss.getUserKeyForGame;
        var result = await getUserKey();

        if (!result) {
          throw new Error('unsupported_toss_app_version');
        }

        if (result === 'INVALID_CATEGORY') {
          throw new Error('invalid_game_category');
        }

        if (result === 'ERROR') {
          throw new Error('get_user_key_failed');
        }

        if (result.type === 'HASH' && result.hash) {
          return result.hash;
        }

        throw new Error('invalid_user_key_response');
      }

      return resolveDevTossHash();
    }

    async function login() {
      try {
        var tossHash = await resolveTossHash();
        var response = await fetch(loginUrl, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            tossHash: tossHash
          })
        });
        var body = await response.text();

        if (!response.ok) {
          throw new Error('login_http_error:' + response.status + ':' + body);
        }

        state.resultJson = body;
      } catch (error) {
        state.error = error && error.message ? error.message : String(error);
      } finally {
        state.completed = true;
      }
    }

    login();
  },

  AITAuth_IsLoginCompleted: function() {
    var state = window.__aitAuthState;
    return state && state.completed ? 1 : 0;
  },

  AITAuth_GetLoginResultJson: function() {
    var state = window.__aitAuthState;
    return stringToNewUTF8(state ? state.resultJson || '' : '');
  },

  AITAuth_GetLoginError: function() {
    var state = window.__aitAuthState;
    return stringToNewUTF8(state ? state.error || '' : '');
  }
});
