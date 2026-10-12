/* ==========================================================================
   Mini-CRM — noyau
   Utilitaires, thème, fenêtres (<dialog>), notifications et système de motion.
   Toutes les durées viennent des tokens CSS : un seul rythme pour toute l'app.
   ========================================================================== */
(function () {
  'use strict';

  var doc = document;
  var root = doc.documentElement;
  var mqReduce = window.matchMedia('(prefers-reduced-motion: reduce)');
  var mqFine = window.matchMedia('(hover: hover) and (pointer: fine)');

  var MC = (window.MC = {});
  Object.defineProperty(MC, 'reducedMotion', { get: function () { return mqReduce.matches; } });
  Object.defineProperty(MC, 'finePointer', { get: function () { return mqFine.matches; } });

  function $(sel, scope) { return (scope || doc).querySelector(sel); }
  function $$(sel, scope) { return Array.prototype.slice.call((scope || doc).querySelectorAll(sel)); }
  MC.$ = $;
  MC.$$ = $$;

  // Durée d'un token CSS en millisecondes (« 260ms » ou « 0.26s »).
  MC.duration = function (name) {
    var v = getComputedStyle(root).getPropertyValue(name).trim();
    if (!v) return 0;
    return v.slice(-2) === 'ms' ? parseFloat(v) : parseFloat(v) * 1000;
  };

  /* ------------------------------------------------------------------ Thème */
  function applyTheme(theme) {
    root.setAttribute('data-theme', theme);
    var meta = $('meta[name="theme-color"]');
    if (meta) meta.content = theme === 'dark' ? '#110c16' : '#f7f5fa';
    $$('[data-theme-toggle]').forEach(function (btn) {
      btn.setAttribute('aria-label', theme === 'dark' ? 'Activer le thème clair' : 'Activer le thème sombre');
    });
  }

  MC.toggleTheme = function () {
    var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    try { localStorage.setItem('mc-theme', next); } catch (e) { /* stockage indisponible */ }

    if (!doc.startViewTransition || MC.reducedMotion) { applyTheme(next); return; }
    root.classList.add('is-theming');
    var vt = doc.startViewTransition(function () { applyTheme(next); });
    vt.ready.catch(function () { });
    vt.finished.finally(function () { root.classList.remove('is-theming'); });
  };

  /* --------------------------------------------------------- Notifications */
  var TOAST = {
    success: { tone: 'success', icon: 'bi-check-lg', ms: 4200 },
    error:   { tone: 'danger',  icon: 'bi-exclamation-lg', ms: 6500 },
    info:    { tone: 'info',    icon: 'bi-info-lg', ms: 4200 }
  };

  MC.toast = function (message, type) {
    var host = $('#toaster');
    if (!host || !message) return;
    var cfg = TOAST[type] || TOAST.success;

    var el = doc.createElement('div');
    el.className = 'toast';
    el.setAttribute('data-tone', cfg.tone);
    el.setAttribute('role', type === 'error' ? 'alert' : 'status');
    el.style.setProperty('--toast-duration', cfg.ms + 'ms');
    el.innerHTML =
      '<span class="toast__icon"><i class="bi ' + cfg.icon + '" aria-hidden="true"></i></span>' +
      '<span class="toast__msg"></span>' +
      '<button type="button" class="icon-btn" aria-label="Fermer la notification"><i class="bi bi-x-lg" aria-hidden="true"></i></button>' +
      '<span class="toast__progress" aria-hidden="true"></span>';
    // textContent : le message (qui contient des noms saisis) n'est jamais interprété comme du HTML.
    el.querySelector('.toast__msg').textContent = message;
    host.appendChild(el);

    var remaining = cfg.ms, startedAt = 0, timer = null;
    function dismiss() {
      clearTimeout(timer);
      if (el.classList.contains('is-leaving')) return;
      el.classList.add('is-leaving');
      setTimeout(function () { el.remove(); }, MC.duration('--dur-hover') + 40);
    }
    function pause() {
      if (!timer) return;
      clearTimeout(timer); timer = null;
      remaining -= Date.now() - startedAt;
    }
    function resume() {
      if (timer) return;
      startedAt = Date.now();
      timer = setTimeout(dismiss, Math.max(remaining, 800));
    }
    el.addEventListener('mouseenter', pause);
    el.addEventListener('mouseleave', resume);
    el.addEventListener('focusin', pause);
    el.addEventListener('focusout', resume);
    el.querySelector('button').addEventListener('click', dismiss);
    resume();

    var visible = $$('.toast:not(.is-leaving)', host);
    if (visible.length > 3) visible[0].querySelector('button').click();
  };

  // Message à afficher après un rechargement complet de la page.
  MC.toastAfterReload = function (message, type) {
    try { sessionStorage.setItem('mc-toast', JSON.stringify({ m: message, t: type || 'success' })); } catch (e) { }
  };

  function flushPendingToasts() {
    try {
      var raw = sessionStorage.getItem('mc-toast');
      if (raw) {
        sessionStorage.removeItem('mc-toast');
        var data = JSON.parse(raw);
        MC.toast(data.m, data.t);
      }
    } catch (e) { }
    var flash = $('[data-flash]');
    if (flash && flash.textContent.trim()) MC.toast(flash.textContent.trim(), flash.getAttribute('data-flash'));
  }

  /* ---------------------------------------------------- Fenêtres <dialog>
     showModal() apporte gratuitement : piège du focus, fond inerte, Échap.
     On ajoute : animation de sortie, fermeture au clic extérieur (si
     [data-dismissable]) et retour du focus sur l'élément d'origine. */
  MC.dialog = {
    open: function (dialog, opener) {
      if (typeof dialog === 'string') dialog = doc.getElementById(dialog);
      if (!dialog) return null;
      if (!dialog.open) {
        dialog.classList.remove('is-leaving');
        dialog.__opener = opener || doc.activeElement;
        dialog.showModal();
      }
      return dialog;
    },
    close: function (dialog) {
      if (!dialog || !dialog.open || dialog.classList.contains('is-leaving')) return;
      dialog.classList.add('is-leaving');
      var ms = MC.reducedMotion ? 0 : MC.duration('--dur-component');
      setTimeout(function () {
        if (dialog.open) dialog.close();
        dialog.classList.remove('is-leaving');
      }, ms);
    },
    closeAll: function () { $$('dialog[open]').forEach(MC.dialog.close); }
  };

  // Échap : on intercepte pour jouer l'animation de sortie.
  doc.addEventListener('cancel', function (e) {
    if (e.target instanceof HTMLDialogElement && e.target.classList.contains('dialog')) {
      e.preventDefault();
      MC.dialog.close(e.target);
    }
  }, true);

  doc.addEventListener('close', function (e) {
    var dialog = e.target;
    if (!(dialog instanceof HTMLDialogElement)) return;
    dialog.classList.remove('is-leaving');
    // Le focus revient sur le déclencheur ; s'il a disparu (liste rechargée), sur le contenu.
    var opener = dialog.__opener;
    dialog.__opener = null;
    if ($('dialog[open]')) return; // une autre fenêtre reste ouverte : elle garde le focus
    if (opener && opener.isConnected && opener !== doc.body && typeof opener.focus === 'function') {
      opener.focus({ preventScroll: true });
    } else {
      var main = $('#contenu');
      if (main) main.focus({ preventScroll: true });
    }
  }, true);

  // Clic sur le voile : uniquement si le geste a commencé ET fini hors du panneau
  // (sélectionner du texte en débordant ne ferme pas la fenêtre).
  var downOnBackdrop = false;
  function outsidePanel(dialog, e) {
    var panel = dialog.querySelector('.dialog__panel');
    if (!panel) return false;
    var r = panel.getBoundingClientRect();
    return e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom;
  }
  doc.addEventListener('pointerdown', function (e) {
    var d = e.target;
    downOnBackdrop = d instanceof HTMLDialogElement && d.open && outsidePanel(d, e);
  });
  doc.addEventListener('click', function (e) {
    var d = e.target;
    if (downOnBackdrop && d instanceof HTMLDialogElement && d.hasAttribute('data-dismissable') && outsidePanel(d, e)) {
      MC.dialog.close(d);
    }
    downOnBackdrop = false;

    var opener = e.target.closest('[data-dialog-open]');
    if (opener) { e.preventDefault(); MC.dialog.open(opener.getAttribute('data-dialog-open'), opener); return; }

    var closer = e.target.closest('[data-dialog-close]');
    if (closer) { e.preventDefault(); MC.dialog.close(closer.closest('dialog')); return; }

    if (e.target.closest('[data-theme-toggle]')) MC.toggleTheme();
  });

  /* --------------------------------------------------------------- Boutons
     loading : spinner + désactivé · success : coche · null : état initial */
  MC.buttonState = function (btn, state) {
    if (!btn) return;
    var icon = btn.querySelector('.bi');
    if (icon && !btn.__icon) btn.__icon = icon.className;
    if (!state) {
      btn.removeAttribute('data-state');
      btn.removeAttribute('aria-busy');
      btn.disabled = false;
      if (icon && btn.__icon) icon.className = btn.__icon;
      return;
    }
    btn.setAttribute('data-state', state);
    if (state === 'loading') { btn.disabled = true; btn.setAttribute('aria-busy', 'true'); }
    if (state === 'success') { btn.removeAttribute('aria-busy'); if (icon) icon.className = 'bi bi-check-lg'; }
  };

  /* ==================================================================
     MOTION — révélation au scroll, compteurs, halo, parallaxe, aimant
     ================================================================== */
  var io = 'IntersectionObserver' in window
    ? new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (!entry.isIntersecting) return;
          io.unobserve(entry.target);
          revealNow(entry.target);
        });
      }, { rootMargin: '0px 0px -6% 0px', threshold: 0.1 })
    : null;

  function revealNow(el) {
    el.classList.add('is-revealed');
    if (el.hasAttribute('data-countup')) countUp(el);
  }

  // Prépare et observe tout ce qui doit apparaître dans « scope ».
  MC.reveal = function (scope) {
    scope = scope || doc;
    $$('[data-reveal-group]', scope).forEach(function (group) {
      $$('[data-reveal]', group).forEach(function (el, i) {
        if (!el.style.getPropertyValue('--reveal-index')) el.style.setProperty('--reveal-index', Math.min(i, 8));
      });
    });
    $$('[data-reveal], .lifespan, .donut, [data-countup]', scope).forEach(function (el) {
      if (el.classList.contains('is-revealed')) return;
      if (!io || MC.reducedMotion) { revealNow(el); return; }
      io.observe(el);
    });
  };

  // Compteur : de 0 à la valeur rendue par le serveur (qui reste la vérité).
  function countUp(el) {
    var target = parseFloat(el.getAttribute('data-countup'));
    if (isNaN(target) || target === 0 || MC.reducedMotion) return;
    var finalText = el.textContent;
    var suffix = el.getAttribute('data-suffix') || '';
    var fmt = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 0 });
    var duration = MC.duration('--dur-complex') + 300;
    var t0 = null;
    function frame(t) {
      if (t0 === null) t0 = t;
      var p = Math.min(1, (t - t0) / duration);
      var eased = 1 - Math.pow(1 - p, 4);
      el.textContent = fmt.format(Math.round(target * eased)) + suffix;
      if (p < 1) requestAnimationFrame(frame);
      else el.textContent = finalText;
    }
    requestAnimationFrame(frame);
  }

  // Halo qui suit le curseur sur les cartes .spotlight (desktop uniquement).
  function bindSpotlight() {
    doc.addEventListener('pointermove', function (e) {
      if (!MC.finePointer || e.pointerType !== 'mouse') return;
      var el = e.target.closest && e.target.closest('.spotlight');
      if (!el) return;
      var r = el.getBoundingClientRect();
      el.style.setProperty('--mx', (e.clientX - r.left) + 'px');
      el.style.setProperty('--my', (e.clientY - r.top) + 'px');
    }, { passive: true });
  }

  // Parallaxe au curseur : [data-parallax] contient des calques [data-depth].
  // Amplitude max 18px × profondeur, lissée par rAF, désactivée au tactile.
  function bindParallax() {
    $$('[data-parallax]').forEach(function (stage) {
      var layers = $$('[data-depth]', stage);
      var frame = null, tx = 0, ty = 0;
      function paint() {
        frame = null;
        layers.forEach(function (layer) {
          var depth = parseFloat(layer.getAttribute('data-depth')) || 0;
          layer.style.translate = (tx * depth * 18).toFixed(2) + 'px ' + (ty * depth * 18).toFixed(2) + 'px';
        });
      }
      stage.addEventListener('pointermove', function (e) {
        if (!MC.finePointer || MC.reducedMotion || e.pointerType !== 'mouse') return;
        var r = stage.getBoundingClientRect();
        tx = ((e.clientX - r.left) / r.width - 0.5) * 2;
        ty = ((e.clientY - r.top) / r.height - 0.5) * 2;
        if (!frame) frame = requestAnimationFrame(paint);
      }, { passive: true });
      stage.addEventListener('pointerleave', function () {
        tx = 0; ty = 0;
        if (!frame) frame = requestAnimationFrame(paint);
      });
    });
  }

  // Effet magnétique très léger sur certains CTA (max 6px).
  function bindMagnetic() {
    $$('[data-magnetic]').forEach(function (el) {
      el.addEventListener('pointermove', function (e) {
        if (!MC.finePointer || MC.reducedMotion || e.pointerType !== 'mouse') return;
        var r = el.getBoundingClientRect();
        var dx = (e.clientX - (r.left + r.width / 2)) / (r.width / 2);
        var dy = (e.clientY - (r.top + r.height / 2)) / (r.height / 2);
        el.style.translate = (dx * 6).toFixed(1) + 'px ' + (dy * 4).toFixed(1) + 'px';
      }, { passive: true });
      el.addEventListener('pointerleave', function () { el.style.translate = ''; });
    });
  }

  /* ------------------------------------------------------------------ Init */
  function init() {
    applyTheme(root.getAttribute('data-theme') || 'light');
    MC.reveal(doc);
    bindSpotlight();
    bindParallax();
    bindMagnetic();
    flushPendingToasts();
  }

  if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', init);
  else init();
})();
