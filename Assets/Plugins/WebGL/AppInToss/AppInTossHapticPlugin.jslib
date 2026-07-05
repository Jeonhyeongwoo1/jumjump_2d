mergeInto(LibraryManager.library, {
  AITHaptic_VibrateLight: function () {
    if (typeof window !== 'undefined' && typeof window.aitVibrate === 'function') {
      window.aitVibrate(0);
      return;
    }

    if (typeof navigator !== 'undefined' && typeof navigator.vibrate === 'function') {
      navigator.vibrate(10);
    }
  },
});
