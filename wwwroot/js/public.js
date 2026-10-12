/* ==========================================================================
   Mini-CRM — vitrine et connexion
   En-tête au scroll, récit synchronisé au défilement, démo interactive,
   remplissage des comptes de démonstration.
   ========================================================================== */
(function () {
  'use strict';

  var MC = window.MC;
  var $ = MC.$, $$ = MC.$$;
  var doc = document;
  var root = doc.documentElement;

  // L'en-tête devient compact et dépoli dès qu'on quitte le haut de page.
  function bindHeader() {
    var header = $('.site-header');
    if (!header || !('IntersectionObserver' in window)) return;
    var sentinel = doc.createElement('div');
    sentinel.setAttribute('aria-hidden', 'true');
    sentinel.style.cssText = 'position:absolute;top:0;left:0;width:1px;height:16px;pointer-events:none;';
    doc.body.prepend(sentinel);
    new IntersectionObserver(function (entries) {
      header.classList.toggle('is-scrolled', !entries[0].isIntersecting);
    }).observe(sentinel);
  }

  // Liens d'ancre dans une feuille : on ferme d'abord, on défile ensuite.
  doc.addEventListener('click', function (e) {
    var link = e.target.closest('dialog a[href^="#"]');
    if (!link) return;
    e.preventDefault();
    var id = link.getAttribute('href');
    MC.dialog.close(link.closest('dialog'));
    setTimeout(function () {
      var target = $(id);
      if (!target) return;
      target.scrollIntoView({ behavior: MC.reducedMotion ? 'auto' : 'smooth', block: 'start' });
      history.pushState(null, '', id);
    }, MC.duration('--dur-component') + 30);
  });

  // Récit : l'étape au centre de l'écran pilote l'état du visuel collant.
  function bindStory() {
    var story = $('[data-story]');
    if (!story) return;
    var stage = $('.story-stage', story);
    var steps = $$('[data-step]', story);

    function activate(n) {
      steps.forEach(function (s) { s.classList.toggle('is-active', +s.getAttribute('data-step') === n); });
      for (var i = 2; i <= steps.length; i++) stage.classList.toggle('is-s' + i, i <= n);
      stage.setAttribute('data-active', n);
    }

    if (!('IntersectionObserver' in window)) { activate(steps.length); return; }
    activate(1);
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) activate(+entry.target.getAttribute('data-step'));
      });
    }, { rootMargin: '-45% 0px -45% 0px' });
    steps.forEach(function (s) { io.observe(s); });
  }

  // Démo : filtrer des contrats d'exemple, avec réorganisation animée.
  function bindDemo() {
    var demo = $('[data-demo]');
    if (!demo) return;
    var items = $$('[data-demo-item]', demo);
    var count = $('[data-demo-count]', demo);
    var empty = $('[data-demo-empty]', demo);

    items.forEach(function (item, i) {
      item.style.viewTransitionName = 'demo-' + i;
      item.style.setProperty('view-transition-class', 'demo-item');
    });

    function apply(filter) {
      var shown = 0;
      items.forEach(function (item) {
        var tags = ' ' + item.getAttribute('data-tags') + ' ';
        var visible = filter === 'all' || tags.indexOf(' ' + filter + ' ') >= 0;
        item.hidden = !visible;
        if (visible) shown++;
      });
      count.innerHTML = '<strong>' + shown + '</strong> contrat' + (shown > 1 ? 's' : '');
      empty.hidden = shown > 0;
    }

    demo.addEventListener('change', function (e) {
      if (e.target.name !== 'demo-filter') return;
      var filter = e.target.value;
      if (!doc.startViewTransition || MC.reducedMotion) { apply(filter); return; }
      root.classList.add('is-local-vt');
      var vt = doc.startViewTransition(function () { apply(filter); });
      vt.ready.catch(function () { });
      vt.finished.finally(function () { root.classList.remove('is-local-vt'); });
    });
  }

  // Connexion (développement uniquement) : remplir avec un compte de démonstration.
  doc.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-fill-login]');
    if (!btn) return;
    var form = $('#account');
    if (!form) return;
    var email = form.querySelector('[name="Input.Email"]');
    var password = form.querySelector('[name="Input.Password"]');
    email.value = btn.getAttribute('data-email');
    password.value = btn.getAttribute('data-password');
    [email, password].forEach(function (input) {
      input.__touched = true;
      input.dispatchEvent(new Event('input', { bubbles: true }));
    });
    form.querySelector('[type=submit]').focus();
    MC.toast('Compte « ' + btn.textContent.trim() + ' » prêt : il ne reste qu\'à se connecter.', 'info');
  });

  function init() {
    bindHeader();
    bindStory();
    bindDemo();
  }

  if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', init);
  else init();
})();
