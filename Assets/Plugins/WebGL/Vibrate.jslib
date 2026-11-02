mergeInto(LibraryManager.library, {
  WebVibrateBlast: function () {
    try {
      if (navigator.vibrate) navigator.vibrate([160, 60, 140, 90, 260]);
    } catch (e) {}
  }
});
