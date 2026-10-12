/* ==========================================================================
   Mini-CRM — interactions applicatives
   Formulaires (validation en ligne), CRUD en AJAX (modale + tiroir),
   filtres dynamiques, pagination, raccourcis, copie.
   Remplace jQuery + jquery-validation : la validation lit directement les
   attributs data-val-* générés par ASP.NET Core.
   ========================================================================== */
(function () {
  'use strict';

  var MC = window.MC;
  var $ = MC.$, $$ = MC.$$;
  var doc = document;
  var AJAX = { 'X-Requested-With': 'XMLHttpRequest' };

  /* ==================================================================
     VALIDATION EN LIGNE
     ================================================================== */
  var FR = {
    required: 'Ce champ est obligatoire.',
    email: 'Saisissez une adresse email valide (ex. nom@domaine.fr).',
    number: 'Saisissez un nombre valide (ex. 2400 ou 2400,50).',
    range: 'Valeur hors des limites autorisées.'
  };

  // Les messages implicites du framework sont en anglais : on les traduit.
  function message(el, rule) {
    var m = el.getAttribute('data-val-' + rule);
    if (!m || /^The (field )?.+ (field is required|must be a number)/i.test(m)) return FR[rule] || FR.required;
    return m;
  }

  function isSkipped(el) {
    return el.disabled || el.type === 'hidden' || el.type === 'radio' || el.type === 'checkbox' ||
      el.getAttribute('data-val') !== 'true' || el.closest('[hidden]');
  }

  function cleanNumber(v) { return v.replace(/[\s  ]/g, ''); }

  function errorFor(el) {
    var v = (el.value || '').trim();
    if (el.hasAttribute('data-val-required') && !v) return message(el, 'required');
    if (!v) return null;
    if (el.hasAttribute('data-val-email') && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return message(el, 'email');
    if (el.hasAttribute('data-val-number') && !/^-?\d+(?:[.,]\d+)?$/.test(cleanNumber(v))) return message(el, 'number');
    if (el.hasAttribute('data-val-range')) {
      var n = parseFloat(cleanNumber(v).replace(',', '.'));
      var min = parseFloat(el.getAttribute('data-val-range-min'));
      var max = parseFloat(el.getAttribute('data-val-range-max'));
      if (!isNaN(n) && ((!isNaN(min) && n < min) || (!isNaN(max) && n > max))) return message(el, 'range');
    }
    var lenMax = el.getAttribute('data-val-maxlength-max') || el.getAttribute('data-val-length-max');
    var lenMin = el.getAttribute('data-val-minlength-min') || el.getAttribute('data-val-length-min');
    if (lenMax && v.length > +lenMax) return el.getAttribute('data-val-maxlength') || el.getAttribute('data-val-length');
    if (lenMin && v.length < +lenMin) return el.getAttribute('data-val-minlength') || el.getAttribute('data-val-length');
    return null;
  }

  function slotFor(el) {
    var form = el.form || el.closest('form');
    if (!form || !el.name) return null;
    return form.querySelector('[data-valmsg-for="' + el.name.replace(/"/g, '\\"') + '"]');
  }

  function describe(el, slot) {
    if (!slot) return;
    if (!slot.id) slot.id = 'err-' + (el.id || el.name).replace(/[^\w-]/g, '-');
    var ids = (el.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean);
    if (ids.indexOf(slot.id) < 0) { ids.push(slot.id); el.setAttribute('aria-describedby', ids.join(' ')); }
  }

  function setState(el, error) {
    var slot = slotFor(el);
    describe(el, slot);
    if (error) {
      el.setAttribute('aria-invalid', 'true');
      el.classList.add('input-validation-error');
      el.classList.remove('is-valid');
      if (slot) {
        slot.textContent = error;
        slot.classList.remove('field-validation-valid');
        slot.classList.add('field-validation-error');
      }
    } else {
      el.removeAttribute('aria-invalid');
      el.classList.remove('input-validation-error');
      el.classList.toggle('is-valid', !!el.__touched && !!el.value.trim() && el.tagName === 'INPUT');
      if (slot) {
        slot.textContent = '';
        slot.classList.add('field-validation-valid');
        slot.classList.remove('field-validation-error');
      }
    }
  }

  function validate(el) {
    if (isSkipped(el)) return true;
    var error = errorFor(el);
    setState(el, error);
    return !error;
  }

  function shake(el) {
    var field = el.closest('.field');
    if (!field || MC.reducedMotion) return;
    field.classList.remove('is-shaking');
    void field.offsetWidth; // relance l'animation
    field.classList.add('is-shaking');
  }

  function validateForm(form) {
    var firstInvalid = null;
    $$('input, select, textarea', form).forEach(function (el) {
      el.__touched = true;
      if (!validate(el) && !firstInvalid) firstInvalid = el;
    });
    if (firstInvalid) {
      firstInvalid.focus();
      shake(firstInvalid);
    }
    return !firstInvalid;
  }

  // Erreurs renvoyées par le serveur : on les relie aux champs (ARIA) et on y amène le focus.
  function adoptServerErrors(scope) {
    var first = null;
    $$('.input-validation-error', scope).forEach(function (el) {
      el.setAttribute('aria-invalid', 'true');
      describe(el, slotFor(el));
      if (!first) first = el;
    });
    var summary = $('.validation-summary-errors', scope);
    if (summary) summary.setAttribute('role', 'alert');
    return first;
  }

  doc.addEventListener('focusout', function (e) {
    var el = e.target;
    if (!el.matches || !el.matches('.input, .select')) return;
    if (!el.value && !el.__touched) return; // ne pas crier sur un champ simplement traversé
    el.__touched = true;
    validate(el);
  });
  doc.addEventListener('input', function (e) {
    var el = e.target;
    if (el.matches && el.matches('.input') && (el.__touched || el.getAttribute('aria-invalid') === 'true')) validate(el);
  });
  doc.addEventListener('change', function (e) {
    var el = e.target;
    if (el.matches && el.matches('.select')) { el.__touched = true; validate(el); }
  });

  /* ------------------------------------------- Mot de passe : voir / masquer */
  doc.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-reveal-pass]');
    if (!btn) return;
    var input = btn.closest('.field__control').querySelector('input');
    var show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    btn.setAttribute('aria-pressed', show ? 'true' : 'false');
    btn.setAttribute('aria-label', show ? 'Masquer le mot de passe' : 'Afficher le mot de passe');
    btn.querySelector('.bi').className = 'bi ' + (show ? 'bi-eye-slash' : 'bi-eye');
    input.focus();
  });

  // Exigences du mot de passe cochées en direct (règles Identity du projet).
  var PASSWORD_RULES = {
    length: function (v) { return v.length >= 8; },
    digit: function (v) { return /\d/.test(v); },
    upper: function (v) { return /[A-Z]/.test(v); },
    lower: function (v) { return /[a-z]/.test(v); }
  };
  doc.addEventListener('input', function (e) {
    var input = e.target;
    if (!input.matches || !input.matches('[data-password-input]')) return;
    var list = doc.getElementById(input.getAttribute('data-password-input'));
    if (!list) return;
    $$('[data-rule]', list).forEach(function (li) {
      var rule = PASSWORD_RULES[li.getAttribute('data-rule')];
      li.classList.toggle('is-met', !!rule && rule(input.value));
    });
  });

  /* ------------------------------------ Contrat : durée calculée en direct */
  function describeDuration(hint) {
    var form = hint.closest('form');
    var start = form && form.querySelector('[name="StartDate"]');
    var end = form && form.querySelector('[name="EndDate"]');
    if (!start || !end || !start.value || !end.value) { hint.hidden = true; return; }
    var a = new Date(start.value), b = new Date(end.value);
    var days = Math.round((b - a) / 86400000);
    var text = hint.querySelector('span');
    hint.hidden = false;
    hint.classList.toggle('is-warning', days < 0);
    if (days < 0) { text.textContent = 'La date de fin précède la date de début.'; return; }
    var months = Math.round(days / 30.44);
    text.textContent = 'Durée : ' + (days < 60
      ? days + ' jour' + (days > 1 ? 's' : '')
      : months >= 12 && months % 12 === 0
        ? (months / 12) + ' an' + (months > 12 ? 's' : '')
        : months + ' mois');
  }
  doc.addEventListener('input', function (e) {
    if (e.target.name !== 'StartDate' && e.target.name !== 'EndDate') return;
    var hint = e.target.form && e.target.form.querySelector('[data-duration]');
    if (hint) describeDuration(hint);
  });

  /* ------------------------------------------ Secteur : liste + « Autre… » */
  function initSectorFields(scope) {
    $$('[data-sector-field]', scope).forEach(function (field) {
      if (field.__bound) return;
      field.__bound = true;
      var select = $('[data-sector-select]', field);
      var other = $('[data-sector-other]', field);
      var input = $('[data-sector-input]', field);
      if (!select || !other || !input) return;

      var current = input.value;
      var known = current && Array.prototype.some.call(select.options, function (o) { return o.value === current && o.value !== '__other__'; });
      select.value = current ? (known ? current : '__other__') : '';
      other.hidden = !current || known;

      select.addEventListener('change', function () {
        if (select.value === '__other__') {
          input.value = '';
          other.hidden = false;
          input.focus();
        } else {
          input.value = select.value;
          other.hidden = true;
          setState(input, null);
        }
      });
    });
  }

  /* ------------------------------------------- Copier dans le presse-papiers */
  doc.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-copy]');
    if (!btn || !navigator.clipboard) return;
    navigator.clipboard.writeText(btn.getAttribute('data-copy')).then(function () {
      var icon = btn.querySelector('.bi');
      var before = icon.className;
      icon.className = 'bi bi-check2';
      btn.setAttribute('data-copied', '');
      MC.toast(btn.getAttribute('data-copy-label') || 'Copié dans le presse-papiers.', 'info');
      setTimeout(function () { icon.className = before; btn.removeAttribute('data-copied'); }, 1600);
    });
  });

  /* ==================================================================
     CONTENU INJECTÉ : tout ce qui arrive en AJAX passe par ici
     ================================================================== */
  function enhance(scope) {
    $$('form', scope).forEach(function (f) { f.noValidate = true; });
    initSectorFields(scope);
    $$('[data-duration]', scope).forEach(describeDuration);
    MC.reveal(scope);
    return adoptServerErrors(scope);
  }
  MC.enhance = enhance;

  /* ==================================================================
     CRUD : modale (création / suppression) et tiroir (fiche / édition)
     ================================================================== */
  var skeletonHtml =
    '<div class="skeleton-stack" aria-hidden="true">' +
    '<div class="skeleton skeleton--title"></div>' +
    '<div class="skeleton skeleton--line w-80"></div>' +
    '<div class="skeleton skeleton--line w-60"></div>' +
    '<div class="skeleton skeleton--block"></div>' +
    '</div><span class="visually-hidden" role="status">Chargement…</span>';

  var errorHtml =
    '<div class="empty"><div class="empty__icon" style="background:var(--tone-danger-bg);color:var(--tone-danger-ink)"><i class="bi bi-wifi-off" aria-hidden="true"></i></div>' +
    '<p class="empty__title">Chargement impossible</p><p class="empty__text">Vérifiez votre connexion puis réessayez.</p></div>';

  function fetchHtml(url, options) {
    options = options || {};
    options.headers = AJAX;
    options.credentials = 'same-origin';
    return fetch(url, options).then(function (r) {
      // Session expirée : le serveur renvoie la page de connexion → on recharge.
      if (r.redirected && /\/Account\/Login/i.test(r.url)) { window.location.reload(); throw new Error('auth'); }
      if (!r.ok) throw new Error('HTTP ' + r.status);
      return r;
    });
  }

  function focusInside(body) {
    var target = $('[autofocus]', body) ||
      $('.input-validation-error', body) ||
      $('form .input:not([type=hidden]):not([disabled]), form .select', body);
    if (target) target.focus({ preventScroll: true });
  }

  function loadInto(body, url) {
    body.setAttribute('aria-busy', 'true');
    body.innerHTML = '';
    // Le squelette n'apparaît que si la réponse tarde : pas de flash en local.
    var skeletonTimer = setTimeout(function () { body.innerHTML = skeletonHtml; }, 140);

    return fetchHtml(url)
      .then(function (r) { return r.text(); })
      .then(function (html) {
        clearTimeout(skeletonTimer);
        body.innerHTML = html;
        body.removeAttribute('aria-busy');
        enhance(body);
        focusInside(body);
      })
      .catch(function (err) {
        clearTimeout(skeletonTimer);
        body.removeAttribute('aria-busy');
        if (err.message !== 'auth') body.innerHTML = errorHtml;
      });
  }

  function labelDialog(dialog, titleEl, title) {
    titleEl.textContent = title || '';
    // Sans titre (confirmation), la fenêtre prend le nom de son premier titre interne.
    if (title) dialog.setAttribute('aria-labelledby', titleEl.id);
  }

  function open(kind, url, title, opener) {
    var dialog = $(kind === 'panel' ? '#crudPanel' : '#crudModal');
    if (!dialog) { window.location.href = url; return; }
    var body = $('.dialog__body', dialog);
    var titleEl = $('.dialog__title', dialog);

    labelDialog(dialog, titleEl, title);
    MC.dialog.open(dialog, opener);
    loadInto(body, url).then(function () {
      if (!title) {
        var h = $('h2, h3', body);
        if (h) { if (!h.id) h.id = dialog.id + '-heading'; dialog.setAttribute('aria-labelledby', h.id); }
      }
    });
  }

  doc.addEventListener('click', function (e) {
    var m = e.target.closest('[data-modal-url]');
    if (m) { e.preventDefault(); open('modal', m.getAttribute('data-modal-url'), m.getAttribute('data-title'), m); return; }
    var p = e.target.closest('[data-panel-url]');
    if (p) { e.preventDefault(); open('panel', p.getAttribute('data-panel-url'), p.getAttribute('data-title'), p); }
  });

  // Après une action réussie : rafraîchir la liste en place, sinon recharger la page.
  function refreshAfterChange(message) {
    var live = $('[data-live-filter]');
    if (live && typeof live.__reload === 'function') {
      MC.toast(message, 'success');
      live.__reload();
    } else {
      MC.toastAfterReload(message, 'success');
      window.location.reload();
    }
  }

  function submitAjax(form) {
    var btn = form.querySelector('[type=submit]');
    var body = form.closest('.dialog__body');
    MC.buttonState(btn, 'loading');

    fetchHtml(form.getAttribute('action'), { method: 'POST', body: new FormData(form) })
      .then(function (r) {
        var type = r.headers.get('content-type') || '';
        return type.indexOf('application/json') >= 0
          ? r.json().then(function (json) { return { json: json }; })
          : r.text().then(function (html) { return { html: html }; });
      })
      .then(function (res) {
        if (res.json) {
          if (res.json.success) {
            MC.buttonState(btn, 'success');
            setTimeout(function () {
              MC.dialog.closeAll();
              refreshAfterChange(res.json.message);
            }, MC.reducedMotion ? 0 : 420);
          } else {
            MC.buttonState(btn, null);
            MC.dialog.closeAll();
            MC.toast(res.json.message || 'Action impossible.', 'error');
          }
          return;
        }
        // Erreurs de validation : le formulaire revient avec les messages et les saisies conservées.
        if (body) {
          body.innerHTML = res.html;
          var firstError = enhance(body);
          if (firstError) { firstError.focus(); shake(firstError); }
        }
      })
      .catch(function (err) {
        MC.buttonState(btn, null);
        if (err.message !== 'auth') MC.toast('Une erreur est survenue. Réessayez dans un instant.', 'error');
      });
  }

  doc.addEventListener('submit', function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement)) return;
    if (!validateForm(form)) { e.preventDefault(); return; }
    if (form.hasAttribute('data-ajax-form')) {
      e.preventDefault();
      submitAjax(form);
      return;
    }
    // Soumission classique (connexion, pages autonomes) : retour visuel immédiat.
    var btn = form.querySelector('[type=submit][data-loading]');
    if (btn) setTimeout(function () { MC.buttonState(btn, 'loading'); }, 0);
  });

  // Retour arrière vers une page dont le bouton était en chargement (bfcache).
  window.addEventListener('pageshow', function (e) {
    if (e.persisted) $$('[data-state="loading"]').forEach(function (b) { MC.buttonState(b, null); });
  });

  /* ==================================================================
     FILTRES DYNAMIQUES + PAGINATION
     ================================================================== */
  function bindLiveFilter(form) {
    var container = $(form.getAttribute('data-target'));
    var baseUrl = form.getAttribute('data-url');
    if (!container || !baseUrl) return;

    var host = container.closest('[data-busy-host]') || container;
    var exportLink = form.getAttribute('data-export-target') ? $(form.getAttribute('data-export-target')) : null;
    var exportUrl = form.getAttribute('data-export-url');
    var liveCount = $('[data-live-count]');
    var controller = null;

    function query() {
      if (!(form instanceof HTMLFormElement)) return '';
      var params = new URLSearchParams();
      new FormData(form).forEach(function (v, k) { if (v) params.set(k, v); });
      return params.toString();
    }

    function announce() {
      var count = $('[data-count]', container);
      if (liveCount && count) liveCount.textContent = count.textContent.trim();
    }

    function load(url, opts) {
      if (controller) controller.abort();
      controller = 'AbortController' in window ? new AbortController() : null;
      host.setAttribute('aria-busy', 'true');

      return fetchHtml(url, { signal: controller && controller.signal })
        .then(function (r) { return r.text(); })
        .then(function (html) {
          container.innerHTML = html;
          host.removeAttribute('aria-busy');
          enhance(container);
          announce();
          history.replaceState(null, '', url);
          if (opts && opts.scroll) {
            var top = host.getBoundingClientRect().top;
            if (top < 0) host.scrollIntoView({ block: 'start', behavior: MC.reducedMotion ? 'auto' : 'smooth' });
          }
        })
        .catch(function (err) {
          if (err.name === 'AbortError' || err.message === 'auth') return;
          host.removeAttribute('aria-busy');
          MC.toast('Impossible de mettre à jour la liste.', 'error');
        });
    }

    function refresh() {
      var qs = query();
      load(baseUrl + (qs ? '?' + qs : ''));
      if (exportLink && exportUrl) exportLink.setAttribute('href', exportUrl + (qs ? '?' + qs : ''));
    }

    form.__reload = function () { return load(window.location.pathname + window.location.search); };

    var timer;
    form.addEventListener('input', function (e) {
      if (e.target.type !== 'search' && e.target.type !== 'text') return;
      syncSearch(e.target);
      clearTimeout(timer);
      timer = setTimeout(refresh, 260);
    });
    form.addEventListener('change', function (e) {
      if (e.target.type === 'radio' || e.target.tagName === 'SELECT') refresh();
    });
    form.addEventListener('submit', function (e) { e.preventDefault(); clearTimeout(timer); refresh(); });

    function reset() {
      $$('input[type=search], input[type=text]', form).forEach(function (i) { i.value = ''; syncSearch(i); });
      var all = $('input[type=radio][value=""]', form);
      if (all) all.checked = true;
      refresh();
      var search = $('input[type=search]', form);
      if (search) search.focus();
    }

    form.addEventListener('click', function (e) {
      var clear = e.target.closest('[data-search-clear]');
      if (clear) {
        var input = $('input[type=search]', form);
        input.value = '';
        syncSearch(input);
        input.focus();
        refresh();
        return;
      }
      if (e.target.closest('[data-reset]')) { e.preventDefault(); reset(); }
    });

    container.addEventListener('click', function (e) {
      if (e.target.closest('[data-reset]')) { e.preventDefault(); reset(); return; }
      var link = e.target.closest('a[data-page][href]');
      if (!link) return;
      e.preventDefault();
      load(link.getAttribute('href'), { scroll: true });
    });
  }

  function syncSearch(input) {
    var wrap = input && input.closest('.search');
    if (wrap) wrap.classList.toggle('has-value', !!input.value);
  }

  // « / » place le curseur dans la recherche, Échap la vide.
  doc.addEventListener('keydown', function (e) {
    var search = $('[data-search-shortcut]');
    if (!search) return;
    var typing = /^(INPUT|TEXTAREA|SELECT)$/.test(doc.activeElement.tagName) || doc.activeElement.isContentEditable;
    if (e.key === '/' && !typing && !$('dialog[open]')) {
      e.preventDefault();
      search.focus();
      search.select();
    } else if (e.key === 'Escape' && doc.activeElement === search && search.value) {
      e.preventDefault();
      search.value = '';
      search.dispatchEvent(new Event('input', { bubbles: true }));
    }
  });

  /* ------------------------------------------------ Anneau : focus légende */
  function focusDonut(item, on) {
    var donut = doc.getElementById(item.getAttribute('data-donut'));
    if (!donut) return;
    var key = item.getAttribute('data-key');
    if (on) donut.setAttribute('data-focus', key); else donut.removeAttribute('data-focus');
    $$('.donut__seg', donut).forEach(function (seg) { seg.classList.toggle('is-focus', on && seg.getAttribute('data-key') === key); });
  }
  ['pointerover', 'focusin'].forEach(function (type) {
    doc.addEventListener(type, function (e) { var i = e.target.closest && e.target.closest('[data-donut]'); if (i) focusDonut(i, true); });
  });
  ['pointerout', 'focusout'].forEach(function (type) {
    doc.addEventListener(type, function (e) { var i = e.target.closest && e.target.closest('[data-donut]'); if (i) focusDonut(i, false); });
  });

  /* ------------------------------------------------------------------ Init */
  function init() {
    var firstError = enhance(doc);
    if (firstError && !$('dialog[open]')) firstError.focus();
    $$('[data-live-filter]').forEach(bindLiveFilter);
    $$('.search input').forEach(syncSearch);
  }

  if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', init);
  else init();
})();
