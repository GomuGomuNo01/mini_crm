// Mini-CRM : scripts front

// ---------- Sidebar mobile ----------
function toggleSidebar(force) {
    var sb = document.getElementById('sidebar');
    var bd = document.getElementById('sidebarBackdrop');
    if (!sb) return;
    var open = (typeof force === 'boolean') ? force : !sb.classList.contains('open');
    sb.classList.toggle('open', open);
    if (bd) bd.classList.toggle('show', open);
}

// ---------- Afficher / masquer le mot de passe ----------
function initPasswordToggles(scope) {
    (scope || document).querySelectorAll('.password-toggle').forEach(function (btn) {
        if (btn.dataset.bound === '1') return;
        btn.dataset.bound = '1';
        btn.addEventListener('click', function () {
            var container = btn.closest('.form-floating') || btn.parentElement;
            var input = container.querySelector('input');
            var icon = btn.querySelector('i');
            if (!input) return;
            if (input.type === 'password') {
                input.type = 'text';
                if (icon) icon.className = 'bi bi-eye-slash';
                btn.setAttribute('aria-label', 'Masquer le mot de passe');
            } else {
                input.type = 'password';
                if (icon) icon.className = 'bi bi-eye';
                btn.setAttribute('aria-label', 'Afficher le mot de passe');
            }
        });
    });
}

// ---------- Secteur : liste déroulante + « Autre… » ----------
function initSectorSelects(scope) {
    (scope || document).querySelectorAll('[data-sector-select]').forEach(function (sel) {
        if (sel.dataset.bound === '1') return;
        sel.dataset.bound = '1';
        var wrap = sel.closest('.mb-3');
        var input = wrap ? wrap.querySelector('[data-sector-input]') : null;
        if (!input) return;

        var current = input.value;
        if (current) {
            var match = Array.from(sel.options).some(function (o) { return o.value === current && o.value !== '__other__'; });
            if (match) { sel.value = current; input.style.display = 'none'; }
            else { sel.value = '__other__'; input.style.display = 'block'; }
        } else {
            sel.value = '';
            input.style.display = 'none';
        }

        sel.addEventListener('change', function () {
            if (sel.value === '__other__') {
                input.value = '';
                input.style.display = 'block';
                input.focus();
            } else {
                input.value = sel.value;
                input.style.display = 'none';
            }
        });
    });
}

// ---------- Toasts (notifications haut-droite) ----------
function showToast(message, type) {
    var c = document.getElementById('toastContainer');
    if (!c) { return; }
    var icon = type === 'error' ? 'bi-exclamation-octagon' : 'bi-check-circle';
    var el = document.createElement('div');
    el.className = 'toast toast-' + (type || 'success');
    el.setAttribute('role', 'alert');
    el.innerHTML =
        '<div class="toast-body d-flex align-items-center gap-2">' +
        '<i class="bi ' + icon + ' toast-icon"></i>' +
        '<span class="flex-grow-1">' + message + '</span>' +
        '<button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Fermer"></button>' +
        '</div>';
    c.appendChild(el);
    var t = bootstrap.Toast.getOrCreateInstance(el, { delay: 3500 });
    t.show();
    el.addEventListener('hidden.bs.toast', function () { el.remove(); });
}

// ---------- Init du contenu injecté dynamiquement ----------
function initDynamicContent(scope) {
    initSectorSelects(scope);
    initPasswordToggles(scope);
}

// ---------- Modal / Offcanvas CRUD ----------
function loadInto(bodyEl, url, onDone) {
    bodyEl.innerHTML = '<div class="text-center py-4 text-muted"><span class="spinner-border spinner-border-sm"></span> Chargement…</div>';
    fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function (r) { return r.text(); })
        .then(function (html) {
            bodyEl.innerHTML = html;
            initDynamicContent(bodyEl);
            if (onDone) onDone();
        })
        .catch(function () { bodyEl.innerHTML = '<p class="text-danger">Erreur de chargement.</p>'; });
}

function openCrudModal(url, title) {
    document.getElementById('crudModalTitle').textContent = title || '';
    var body = document.getElementById('crudModalBody');
    var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('crudModal'));
    modal.show();
    loadInto(body, url);
}

function openCrudPanel(url, title) {
    document.getElementById('crudPanelTitle').textContent = title || '';
    var body = document.getElementById('crudPanelBody');
    var panel = bootstrap.Offcanvas.getOrCreateInstance(document.getElementById('crudPanel'));
    panel.show();
    loadInto(body, url);
}

function closeCrud(el) {
    var modal = el.closest('.modal');
    if (modal) { var mi = bootstrap.Modal.getInstance(modal); if (mi) mi.hide(); return; }
    var oc = el.closest('.offcanvas');
    if (oc) { var oi = bootstrap.Offcanvas.getInstance(oc); if (oi) oi.hide(); }
}

function reloadCrudTable() {
    var f = document.querySelector('[data-live-filter]');
    if (f && typeof f.__reload === 'function') f.__reload();
}

// Soumission AJAX d'un formulaire (création / édition / suppression)
function submitAjaxForm(form) {
    var btn = form.querySelector('button[type=submit]');
    if (btn) btn.disabled = true;

    fetch(form.getAttribute('action'), {
        method: 'POST',
        body: new FormData(form),
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    }).then(function (r) {
        var ct = r.headers.get('content-type') || '';
        if (ct.indexOf('application/json') >= 0) return r.json().then(function (j) { return { json: j }; });
        return r.text().then(function (t) { return { html: t }; });
    }).then(function (res) {
        if (res.json) {
            var container = form.closest('.modal, .offcanvas');
            if (container) {
                if (container.classList.contains('modal')) { var mi = bootstrap.Modal.getInstance(container); if (mi) mi.hide(); }
                else { var oi = bootstrap.Offcanvas.getInstance(container); if (oi) oi.hide(); }
            }
            if (res.json.success) {
                showToast(res.json.message, 'success');
                reloadCrudTable();
            } else {
                showToast(res.json.message || 'Action impossible.', 'error');
            }
        } else if (res.html != null) {
            // Erreurs de validation : ré-afficher le formulaire avec les messages
            var bodyEl = form.closest('#crudModalBody, #crudPanelBody');
            if (bodyEl) { bodyEl.innerHTML = res.html; initDynamicContent(bodyEl); }
            if (btn) btn.disabled = false;
        }
    }).catch(function () {
        if (btn) btn.disabled = false;
        showToast('Une erreur est survenue.', 'error');
    });
}

// ---------- Délégation d'événements globale ----------
document.addEventListener('click', function (e) {
    var m = e.target.closest('[data-modal-url]');
    if (m) { e.preventDefault(); openCrudModal(m.getAttribute('data-modal-url'), m.getAttribute('data-title')); return; }

    var p = e.target.closest('[data-panel-url]');
    if (p) { e.preventDefault(); openCrudPanel(p.getAttribute('data-panel-url'), p.getAttribute('data-title')); return; }

    var c = e.target.closest('[data-crud-close]');
    if (c) { e.preventDefault(); closeCrud(c); return; }
});

document.addEventListener('submit', function (e) {
    var form = e.target.closest('form[data-ajax-form]');
    if (!form) return;
    e.preventDefault();
    submitAjaxForm(form);
});

// ---------- Filtres de recherche dynamiques (AJAX) ----------
document.addEventListener('DOMContentLoaded', function () {
    initPasswordToggles(document);
    initSectorSelects(document);

    // Affiche en toast un message de succès laissé en TempData (cas non-AJAX)
    var flash = document.getElementById('flashSuccess');
    if (flash && flash.value) showToast(flash.value, 'success');

    document.querySelectorAll('[data-live-filter]').forEach(function (form) {
        var container = document.querySelector(form.getAttribute('data-target'));
        var baseUrl = form.getAttribute('data-url');
        if (!container || !baseUrl) return;

        var inputs = form.querySelectorAll('[data-filter]');
        var exportUrl = form.getAttribute('data-export-url');
        var exportLink = form.getAttribute('data-export-target')
            ? document.querySelector(form.getAttribute('data-export-target'))
            : null;

        function queryString() {
            var params = new URLSearchParams();
            inputs.forEach(function (el) { if (el.value) params.set(el.getAttribute('name'), el.value); });
            return params.toString();
        }

        function load(url) {
            container.classList.add('is-loading');
            fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) { return r.text(); })
                .then(function (html) {
                    container.innerHTML = html;
                    container.classList.remove('is-loading');
                    history.replaceState(null, '', url);
                })
                .catch(function () { container.classList.remove('is-loading'); });
        }

        function refresh() {
            var qs = queryString();
            load(baseUrl + (qs ? '?' + qs : ''));
            if (exportLink && exportUrl) exportLink.setAttribute('href', exportUrl + (qs ? '?' + qs : ''));
        }

        // Recharge la table en conservant l'URL courante (filtres + page).
        form.__reload = function () { load(window.location.pathname + window.location.search); };

        var timer;
        inputs.forEach(function (el) {
            var immediate = el.tagName === 'SELECT';
            el.addEventListener(immediate ? 'change' : 'input', function () {
                clearTimeout(timer);
                timer = setTimeout(refresh, immediate ? 0 : 300);
            });
        });

        var resetBtn = form.querySelector('[data-reset]');
        if (resetBtn) {
            resetBtn.addEventListener('click', function (e) {
                e.preventDefault();
                inputs.forEach(function (el) { el.value = ''; });
                refresh();
            });
        }

        container.addEventListener('click', function (e) {
            var link = e.target.closest('a.page-link');
            if (link && link.getAttribute('href')) { e.preventDefault(); load(link.getAttribute('href')); }
        });
    });
});
