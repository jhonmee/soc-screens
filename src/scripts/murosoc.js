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

  var STYLE_ID = 'murosoc-style';
  var CURSOR_ID = 'murosoc-cursor';
  var cursorHidden = false;
  var picker = null;
  var observer = null;

  function isSafeSelector(selector) {
    if (!selector || typeof selector !== 'string' || /[{}<>]/.test(selector)) {
      return false;
    }
    try {
      document.createDocumentFragment().querySelector(selector);
      return true;
    } catch (e) {
      return false;
    }
  }

  function urlMatches(pattern) {
    if (!pattern || pattern === '*') {
      return true;
    }
    var source = pattern.replace(/[.+?^${}()|[\]\\]/g, '\\$&').replace(/\*/g, '.*');
    try {
      return new RegExp('^' + source + '$', 'i').test(location.href);
    } catch (e) {
      return false;
    }
  }

  function buildCss() {
    var out = [];
    if (config.hideScrollbars) {
      out.push('html, body, * { scrollbar-width: none !important; } ::-webkit-scrollbar { display: none !important; width: 0 !important; height: 0 !important; }');
    }
    if (config.minWidth > 0) {
      out.push('html { overflow-x: auto !important; } body { min-width: ' + Math.round(config.minWidth) + 'px !important; }');
    }
    (config.hide || []).forEach(function (selector) {
      if (isSafeSelector(selector)) {
        out.push(selector + ' { display: none !important; }');
      }
    });
    if (isSafeSelector(config.isolate)) {
      var s = config.isolate;
      out.push('body * { visibility: hidden !important; }');
      out.push(s + ', ' + s + ' * { visibility: visible !important; }');
      out.push(s + ' { position: fixed !important; left: 0 !important; top: 0 !important; right: 0 !important; bottom: 0 !important; width: 100vw !important; height: 100vh !important; max-width: none !important; max-height: none !important; margin: 0 !important; transform: none !important; z-index: 2147483646 !important; overflow: auto !important; box-sizing: border-box !important; }');
    }
    if (config.css && urlMatches(config.cssPattern)) {
      out.push(config.css);
    }
    return out.join('\n');
  }

  function ensureStyle() {
    var root = document.head || document.documentElement;
    if (!root) {
      return;
    }
    var css = buildCss();
    var element = document.getElementById(STYLE_ID);
    if (!css) {
      if (element) {
        element.remove();
      }
      return;
    }
    if (!element) {
      element = document.createElement('style');
      element.id = STYLE_ID;
      root.appendChild(element);
    }
    if (element.textContent !== css) {
      element.textContent = css;
    }
  }

  function ensureCursor() {
    var element = document.getElementById(CURSOR_ID);
    if (!cursorHidden) {
      if (element) {
        element.remove();
      }
      return;
    }
    var root = document.head || document.documentElement;
    if (!element && root) {
      element = document.createElement('style');
      element.id = CURSOR_ID;
      element.textContent = '*, *::before, *::after { cursor: none !important; }';
      root.appendChild(element);
    }
  }

  function watch() {
    if (observer || !document.documentElement) {
      return;
    }
    var pending = false;
    observer = new MutationObserver(function () {
      if (pending) {
        return;
      }
      pending = true;
      setTimeout(function () {
        pending = false;
        ensureStyle();
        ensureCursor();
      }, 250);
    });
    observer.observe(document.documentElement, { childList: true });
    if (document.head) {
      observer.observe(document.head, { childList: true });
    }
  }

  function findElement(selector) {
    if (!isSafeSelector(selector)) {
      return null;
    }
    try {
      return document.querySelector(selector);
    } catch (e) {
      return null;
    }
  }

  function restorePan() {
    if (!config.pan) {
      return;
    }
    [0, 500, 1500, 3000, 6000].forEach(function (delay) {
      setTimeout(function () {
        var target = config.panSelector ? findElement(config.panSelector) : null;
        if (target) {
          target.scrollLeft = config.panX;
          target.scrollTop = config.panY;
        } else {
          window.scrollTo(config.panX, config.panY);
        }
      }, delay);
    });
  }

  function cssPath(element) {
    if (!(element instanceof Element)) {
      return '';
    }
    var parts = [];
    var node = element;
    while (node && node.nodeType === 1 && node !== document.documentElement) {
      if (node.id && /^[A-Za-z][\w-]*$/.test(node.id) && document.querySelectorAll('#' + node.id).length === 1) {
        parts.unshift('#' + node.id);
        break;
      }
      var tag = node.tagName.toLowerCase();
      var parent = node.parentElement;
      if (parent) {
        var same = Array.prototype.filter.call(parent.children, function (child) { return child.tagName === node.tagName; });
        if (same.length > 1) {
          tag += ':nth-of-type(' + (same.indexOf(node) + 1) + ')';
        }
      }
      parts.unshift(tag);
      if (tag === 'body') {
        break;
      }
      node = parent;
    }
    return parts.join(' > ');
  }

  function capturePan() {
    var best = null;
    var bestTop = 0;
    var elements = document.querySelectorAll('body *');
    for (var i = 0; i < elements.length && i < 20000; i++) {
      var el = elements[i];
      if ((el.scrollTop > 0 || el.scrollLeft > 0) && el.scrollHeight > el.clientHeight) {
        var area = el.clientWidth * el.clientHeight;
        if (area > bestTop) {
          bestTop = area;
          best = el;
        }
      }
    }
    if (best && window.scrollY === 0 && window.scrollX === 0) {
      return { x: Math.round(best.scrollLeft), y: Math.round(best.scrollTop), selector: cssPath(best) };
    }
    return { x: Math.round(window.scrollX), y: Math.round(window.scrollY), selector: '' };
  }

  function stopPicker() {
    if (!picker) {
      return;
    }
    document.removeEventListener('mousemove', picker.move, true);
    document.removeEventListener('click', picker.click, true);
    document.removeEventListener('keydown', picker.key, true);
    picker.box.remove();
    picker.label.remove();
    picker = null;
  }

  function startPicker(mode) {
    stopPicker();
    if (!document.body) {
      return;
    }
    var box = document.createElement('div');
    box.style.cssText = 'position:fixed;pointer-events:none;z-index:2147483647;border:2px solid #ff8c00;background:rgba(255,140,0,0.15);display:none;';
    var label = document.createElement('div');
    label.style.cssText = 'position:fixed;left:50%;top:8px;transform:translateX(-50%);z-index:2147483647;background:#1e1e1e;color:#fff;font:13px Segoe UI,sans-serif;padding:6px 10px;border-radius:4px;pointer-events:none;';
    label.textContent = mode === 'hide' ? 'Clic en el elemento que quieres ocultar · Esc para cancelar' : 'Clic en el elemento que quieres mostrar solo · Esc para cancelar';
    document.documentElement.appendChild(box);
    document.documentElement.appendChild(label);
    var current = null;
    picker = {
      box: box,
      label: label,
      move: function (e) {
        var target = e.target;
        if (!(target instanceof Element) || target === box || target === label) {
          return;
        }
        current = target;
        var rect = target.getBoundingClientRect();
        box.style.display = 'block';
        box.style.left = rect.left + 'px';
        box.style.top = rect.top + 'px';
        box.style.width = rect.width + 'px';
        box.style.height = rect.height + 'px';
      },
      click: function (e) {
        e.preventDefault();
        e.stopPropagation();
        var selector = cssPath(current || e.target);
        stopPicker();
        if (selector) {
          post({ type: 'picked', mode: mode, selector: selector });
        }
      },
      key: function (e) {
        if (e.key === 'Escape') {
          e.preventDefault();
          e.stopPropagation();
          stopPicker();
          post({ type: 'pickcancel' });
        }
      }
    };
    document.addEventListener('mousemove', picker.move, true);
    document.addEventListener('click', picker.click, true);
    document.addEventListener('keydown', picker.key, true);
  }

  function apply(next) {
    if (next) {
      config = next;
    }
    ensureStyle();
    ensureCursor();
    watch();
  }

  function onNavigate() {
    setTimeout(function () {
      ensureStyle();
      restorePan();
    }, 0);
  }

  ['pushState', 'replaceState'].forEach(function (name) {
    var original = history[name];
    history[name] = function () {
      var result = original.apply(this, arguments);
      onNavigate();
      return result;
    };
  });
  window.addEventListener('popstate', onNavigate);
  window.addEventListener('hashchange', onNavigate);
  document.addEventListener('DOMContentLoaded', function () {
    apply();
    restorePan();
  });
  window.addEventListener('load', restorePan);
  if (document.documentElement) {
    apply();
  }

  window.__muroSoc = {
    apply: apply,
    busy: function () {
      return { typed: hasTypedText(), dialog: hasOpenDialog() };
    },
    capturePan: capturePan,
    restorePan: restorePan,
    pick: startPicker,
    cancelPick: stopPicker,
    cursor: function (hidden) {
      cursorHidden = !!hidden;
      ensureCursor();
    }
  };
})();
