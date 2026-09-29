// MuroSOC - murosoc.js
(function () {
  'use strict';
  if (window.top !== window || window.__muroSoc) {
    return;
  }

  var config = __MUROSOC_CONFIG__;
  var lastActivity = 0;
  var lastHover = null;

  function post(message) {
    try {
      window.chrome.webview.postMessage(message);
    } catch (e) {
    }
  }

  function onActivity() {
    var now = Date.now();
    if (now - lastActivity > 5000) {
      lastActivity = now;
      post({ type: 'activity' });
    }
  }

  function setHover(value) {
    if (lastHover !== value) {
      lastHover = value;
      post({ type: 'hover', value: value });
    }
  }

  ['mousedown', 'keydown', 'wheel', 'touchstart'].forEach(function (name) {
    window.addEventListener(name, onActivity, true);
  });
  document.addEventListener('mousemove', function () { setHover(true); }, true);
  document.addEventListener('mouseleave', function () { setHover(false); }, true);
  window.addEventListener('blur', function () { setHover(false); });

  function isTextField(element) {
    if (!element) {
      return false;
    }
    if (element.isContentEditable) {
      return true;
    }
    var tag = element.tagName;
    if (tag === 'TEXTAREA') {
      return true;
    }
    if (tag !== 'INPUT') {
      return false;
    }
    var type = (element.type || 'text').toLowerCase();
    return ['text', 'search', 'email', 'url', 'tel', 'number', 'password'].indexOf(type) >= 0;
  }

  function hasTypedText() {
    var active = document.activeElement;
    if (isTextField(active)) {
      var value = active.isContentEditable ? active.textContent : active.value;
      if (value && value.length > 0) {
        return true;
      }
    }
    var fields = document.querySelectorAll('input, textarea');
    for (var i = 0; i < fields.length && i < 2000; i++) {
      var field = fields[i];
      if (isTextField(field) && field.value !== field.defaultValue && field.value.length > 0) {
        return true;
      }
    }
    return false;
  }

  function isVisible(element) {
    if (!element || element.getClientRects().length === 0) {
      return false;
    }
    var style = window.getComputedStyle(element);
    return style.visibility !== 'hidden' && style.display !== 'none' && style.opacity !== '0';
  }

  function hasOpenDialog() {
    if (document.querySelector('dialog[open]')) {
      return true;
    }
    var candidates = document.querySelectorAll('[role="dialog"], [role="alertdialog"], [aria-modal="true"]');
    for (var i = 0; i < candidates.length && i < 200; i++) {
      if (candidates[i].getAttribute('aria-hidden') !== 'true' && isVisible(candidates[i])) {
        return true;
      }
    }
    return false;
  }

  function apply(next) {
    if (next) {
      config = next;
    }
  }

  window.__muroSoc = {
    apply: apply,
    busy: function () {
      return { typed: hasTypedText(), dialog: hasOpenDialog() };
    }
  };
})();
