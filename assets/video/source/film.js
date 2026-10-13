/* ==========================================================================
   Moteur de la vidéo : render(t) dessine l'image à l'instant t (secondes).
   Fonction pure du temps : aucune animation CSS, aucun état caché entre deux
   images. build.py l'appelle 1 800 fois (60 s × 30 i/s) et capture l'écran.

   Tout est calé sur la musique : 96 BPM, une mesure = 2,5 s, 24 mesures.
   B(n) = début de la mesure n, en secondes.

   Aperçu dans un navigateur : film.html?t=12.5 (image fixe) ou film.html?play
   ========================================================================== */
(function () {
  'use strict';

  var TL = window.TL;
  var BAR = 60 / TL.bpm * TL.beatsPerBar;
  var BEAT = BAR / TL.beatsPerBar;
  var DUR = TL.bars * BAR;
  var B = function (n) { return n * BAR; };

  var $ = function (id) { return document.getElementById(id); };
  var clamp = function (v, a, b) { a = a === undefined ? 0 : a; b = b === undefined ? 1 : b; return Math.min(b, Math.max(a, v)); };
  var P = function (t, t0, d) { return clamp((t - t0) / d); };
  var lerp = function (a, b, k) { return a + (b - a) * k; };

  // Courbes : les mêmes que l'interface de Mini-CRM (sortie rapide, rebond léger)
  var E = {
    out: function (k) { return 1 - Math.pow(1 - k, 4); },
    expo: function (k) { return k >= 1 ? 1 : 1 - Math.pow(2, -10 * k); },
    inOut: function (k) { return k < 0.5 ? 4 * k * k * k : 1 - Math.pow(-2 * k + 2, 3) / 2; },
    back: function (k) { var c1 = 1.55, c3 = c1 + 1; return 1 + c3 * Math.pow(k - 1, 3) + c1 * Math.pow(k - 1, 2); },
    sine: function (k) { return -(Math.cos(Math.PI * k) - 1) / 2; }
  };

  // Pose une transformation complète (translation, rotation, échelle, opacité)
  function T(el, o) {
    if (typeof el === 'string') el = $(el);
    if (!el) return;
    o = o || {};
    var sx = o.sx !== undefined ? o.sx : (o.s !== undefined ? o.s : 1);
    var sy = o.sy !== undefined ? o.sy : (o.s !== undefined ? o.s : 1);
    el.style.transform = 'translate3d(' + (o.x || 0) + 'px,' + (o.y || 0) + 'px,0) rotate(' + (o.r || 0) + 'deg) scale(' + sx + ',' + sy + ')';
    el.style.opacity = o.o === undefined ? 1 : o.o;
  }
  // Entrée « monte et se pose » : le geste de base de toute la vidéo
  function rise(el, t, t0, opt) {
    opt = opt || {};
    var k = (opt.ease || E.expo)(P(t, t0, opt.d || 0.8));
    T(el, { x: (1 - k) * (opt.dx || 0), y: (1 - k) * (opt.dist === undefined ? 44 : opt.dist), o: clamp(k * 1.5), s: lerp(opt.s0 || 1, 1, k), r: (1 - k) * (opt.r0 || 0) + (opt.r || 0) });
    return k;
  }
  function pop(el, t, t0, d) {
    var k = P(t, t0, d || 0.5);
    T(el, { s: k <= 0 ? 0.001 : E.back(k), o: clamp(k * 3) });
    return k;
  }
  function typed(el, text, t, t0, cps) {
    var n = Math.floor(clamp((t - t0) * (cps || 18), 0, text.length));
    el.textContent = text.slice(0, n);
    return n;
  }
  function drift(id, t, ax, ay, sp, ph) {
    T(id, { x: Math.sin(t * sp + ph) * ax, y: Math.cos(t * sp * 0.8 + ph) * ay });
  }
  var fr = new Intl.NumberFormat('fr-FR');
  var euros = function (v) { return fr.format(Math.round(v)) + ' €'; };

  // Découpe un texte en mots animables (garde le HTML des accents)
  function words(el, html) {
    el.innerHTML = html.replace(/(<span class="accent">)(.*?)(<\/span>)/, function (_, a, inner, c) {
      return inner.split(' ').map(function (w) { return '<span class="word accent">' + w + '</span>'; }).join(' ');
    }).split(/ (?![^<]*>)/).map(function (w) { return w.indexOf('class="word') > 0 ? w : '<span class="word">' + w + '</span>'; }).join(' ');
    return Array.prototype.slice.call(el.querySelectorAll('.word'));
  }
  function letters(el, text) {
    el.innerHTML = text.split('').map(function (c) {
      return c === '·' ? '<i class="ltr" style="display:inline-block">·</i>' : '<span class="ltr" style="display:inline-block">' + c + '</span>';
    }).join('');
    return Array.prototype.slice.call(el.querySelectorAll('.ltr'));
  }

  // ------------------------------------------------------------------ Préparation
  var S = {};
  function init() {
    S.t1 = words($('s1-t1'), 'Vos clients. Vos contrats.');
    S.t2 = words($('s1-t2'), '<span class="accent">Sous contrôle.</span>');
    S.word1 = letters($('s1-word'), 'mini·crm');
    S.word8 = letters($('s8-word'), 'mini·crm');
    S.chips1 = Array.prototype.slice.call(document.querySelectorAll('#s1-chips .chip'));
    S.chips7 = Array.prototype.slice.call(document.querySelectorAll('#s7-chips .chip'));
    S.segs = Array.prototype.slice.call(document.querySelectorAll('#s3-donut .seg'));

    // Logo d'ouverture : même composition que la marque de l'instrumentation,
    // agrandie 1/0,2364 fois ; on la centre puis on la ramène en haut à gauche.
    // (Mesure faite scène affichée : une scène masquée mesure 0 px.)
    var logo = $('s1-logo');
    $('s1').style.display = 'block';
    S.logoW = logo.offsetWidth; S.logoH = logo.offsetHeight;
    S.logoX = 960 - S.logoW / 2; S.logoY = 470 - S.logoH / 2;
    var mark = $('s1-mark');
    S.dotX = S.logoX + mark.offsetLeft + 31 + 158 * (12.5 / 32);
    S.dotY = S.logoY + mark.offsetTop + 31 + 158 * 0.5;

    // Repères du fil du temps (un par chapitre)
    var marks = $('tl-marks'), html = '';
    var short = ['Ouverture', 'Constat', 'Solution', 'Fonctionnement', 'Audit', 'Avantages', 'Technique', 'Fin'];
    TL.scenes.forEach(function (s, i) {
      var x0 = B(s.from) / DUR * 1728, x1 = B(s.to) / DUR * 1728;
      if (i > 0) html += '<span class="tl-tick" style="left:' + x0.toFixed(1) + 'px"></span>';
      html += '<span class="tl-label" data-i="' + i + '" style="left:' + ((x0 + x1) / 2).toFixed(1) + 'px">' + short[i] + '</span>';
    });
    marks.innerHTML = html;
    S.labels = Array.prototype.slice.call(marks.querySelectorAll('.tl-label'));
  }

  // ================================================================== SCÈNES
  var SC = {};

  // ---------- 1. Ouverture : le fil du temps devient la marque, puis la promesse
  SC.s1 = function (t) {
    drift('s1-b1', t, 40, 26, 0.35, 0); drift('s1-b2', t, 30, 20, 0.3, 2);

    // Le fil se trace depuis le centre, puis descend devenir la ligne de l'instrumentation
    var kd = E.expo(P(t, 0.15, 1.1));
    var km = E.inOut(P(t, 1.9, 0.9));
    T('s1-line', { y: km * 467.5, sx: lerp(kd, 1728 / 1400, km), sy: lerp(1, 0.75, km), o: 1 - P(t, 2.75, 0.2) });

    // Le point « aujourd'hui » tombe sur le fil, puis rejoint le logo
    var kp = P(t, 0.85, 0.5);
    var kj = E.inOut(P(t, 1.35, 0.6));
    var dx = lerp(960, S.dotX, kj), dy = lerp(540, S.dotY, kj) - Math.sin(kj * Math.PI) * 120;
    var dot = $('s1-dot');
    dot.style.left = dx + 'px'; dot.style.top = dy + 'px';
    T(dot, { s: (kp <= 0 ? 0.001 : E.back(kp)) * lerp(1, 1.55, kj), o: 1 - P(t, 2.05, 0.15) });
    dot.style.background = kj > 0.85 ? '#ffffff' : 'var(--corail)';

    // Marque, arc, lettres, puis réduction vers l'instrumentation
    var logo = $('s1-logo');
    var kl = E.inOut(P(t, 3.05, 0.6));
    var sc = lerp(1, 0.23636, kl);
    T(logo, { x: lerp(S.logoX, 96, kl), y: lerp(S.logoY, 48, kl), s: sc, o: t < 3.65 ? 1 : 0 });
    var km2 = P(t, 1.75, 0.55);
    T('s1-mark', { s: km2 <= 0 ? 0.001 : E.back(km2), o: clamp(km2 * 4) });
    $('s1-arc').style.strokeDasharray = E.out(P(t, 2.0, 0.55)) + ' 1';
    $('s1-spark').style.opacity = 0.65 * P(t, 2.4, 0.3);
    S.word1.forEach(function (el, i) {
      var k = E.expo(P(t, 2.05 + i * 0.05, 0.6));
      T(el, { y: (1 - k) * 60, o: clamp(k * 1.6) });
    });

    // Promesse : mot à mot, sur les demi-temps
    S.t1.forEach(function (el, i) { rise(el, t, B(1.5) + i * BEAT / 2, { dist: 70, d: 0.9 }); });
    S.t2.forEach(function (el, i) { rise(el, t, B(2) + i * BEAT / 2, { dist: 70, d: 0.9 }); });
    rise('s1-sub', t, B(2.25), { dist: 30 });
    var poster = !!window.POSTER;
    $('s1-chips').style.display = poster ? 'none' : 'flex';
    $('poster-play').style.display = poster ? 'grid' : 'none';
    S.chips1.forEach(function (el, i) { rise(el, t, B(2.5) + i * 0.08, { dist: 24, d: 0.6 }); });
  };

  // ---------- 2. Le constat : tout est éparpillé, l'échéance tombe sans prévenir
  SC.s2 = function (t) {
    var t0 = B(3);
    drift('s2-b1', t, 40, 30, 0.3, 1); drift('s2-b2', t, 30, 24, 0.25, 3);
    rise('s2-eye', t, t0 + 0.15, { dist: 20 });
    ['s2-l1', 's2-l2', 's2-l3'].forEach(function (id, i) { rise(id, t, t0 + 0.3 + i * BEAT / 2, { dist: 60, d: 0.9 }); });
    rise('s2-sub', t, t0 + BEAT * 2, { dist: 26 });

    // Les morceaux épars arrivent un par un et flottent
    var items = [['s2-xls1', 0.5, -4], ['s2-q', 1.0, 2], ['s2-xls2', 1.5, 5], ['s2-note', 2.0, -7], ['s2-cal', 2.6, -2]];
    items.forEach(function (it, i) {
      var k = E.expo(P(t, t0 + it[1], 0.9));
      T(it[0], {
        y: (1 - k) * 80 + Math.sin(t * 1.15 + i * 1.7) * 7,
        x: Math.cos(t * 0.9 + i) * 4,
        r: it[2] + (1 - k) * (i % 2 ? 9 : -9),
        s: lerp(0.9, 1, k), o: clamp(k * 1.6)
      });
    });

    // Compte à rebours de la date de fin, puis le tampon « EXPIRÉ » sur le premier temps de la mesure 5
    var steps = ['J-3', 'J-2', 'J-1', 'Jour J'];
    var idx = clamp(Math.floor((t - (B(4) + 0.6)) / (BEAT * 0.75)), 0, 3);
    $('s2-j').textContent = steps[idx];
    var ks = P(t, B(5), 0.35);
    T('s2-stamp', { s: ks <= 0 ? 0.001 : lerp(2.4, 1, E.out(ks)), r: -12, o: ks > 0 ? clamp(ks * 3) : 0 });
    var shake = t > B(5) ? Math.exp(-(t - B(5)) * 9) * Math.sin((t - B(5)) * 70) * 14 : 0;
    $('s2-cluster').style.transform = 'translate3d(' + shake + 'px,' + (shake * 0.4) + 'px,0)';
    rise('s2-late', t, B(5) + 0.35, { dist: 16 });
    rise('s2-punch', t, B(5) + BEAT * 2, { dist: 30, d: 0.9 });
  };

  // ---------- 3. La solution : un seul espace, tout est relié
  SC.s3 = function (t) {
    var t0 = B(6);
    drift('s3-b1', t, 40, 26, 0.3, 2); drift('s3-b2', t, 30, 22, 0.28, 0.5);
    rise('s3-eye', t, t0 + 0.1, { dist: 20 });
    rise('s3-l1', t, t0 + 0.25, { dist: 60, d: 0.9 });
    rise('s3-l2', t, t0 + 0.25 + BEAT / 2, { dist: 60, d: 0.9 });

    var ka = rise('s3-app', t, t0 + 0.5, { dist: 70, s0: 0.95, d: 1.0 });
    var push = E.sine(P(t, B(7.75), B(1.25)));
    if (ka >= 1) T('s3-app', { s: 1 + push * 0.025 });
    rise('s3-hello', t, t0 + 0.9, { dist: 16 });
    ['s3-k1', 's3-k2', 's3-k3', 's3-k4'].forEach(function (id, i) { rise(id, t, t0 + 1.1 + i * 0.14, { dist: 26, s0: 0.92 }); });
    var kc = E.out(P(t, t0 + 1.2, 1.3));
    $('s3-v1').textContent = euros(80800 * kc);
    $('s3-v2').textContent = Math.round(9 * kc);
    $('s3-v3').textContent = Math.round(13 * kc);
    $('s3-v4').textContent = Math.round(3 * kc);

    rise('s3-donut', t, t0 + 1.6, { dist: 26 });
    S.segs.forEach(function (seg, i) {
      var len = +seg.getAttribute('data-len'), off = +seg.getAttribute('data-off');
      var k = E.out(P(t, t0 + 1.9 + i * 0.18, 0.7));
      seg.setAttribute('stroke-dasharray', (len * k).toFixed(2) + ' ' + (100 - len * k).toFixed(2));
      seg.setAttribute('stroke-dashoffset', (-off).toFixed(2));
    });
    rise('s3-due', t, t0 + 1.75, { dist: 26 });
    [['s3-r1', 's3-f1', 96], ['s3-r2', 's3-f2', 94], ['s3-r3', 's3-f3', 92]].forEach(function (r, i) {
      rise(r[0], t, t0 + 2.0 + i * 0.2, { dist: 14 });
      $(r[1]).style.width = (r[2] * E.out(P(t, t0 + 2.15 + i * 0.2, 0.9))) + '%';
    });

    // Les trois piliers, sur les temps de la mesure 7
    ['s3-p1', 's3-p2', 's3-p3'].forEach(function (id, i) { rise(id, t, B(7) + i * BEAT, { dx: -40, dist: 0, d: 0.8 }); });
  };

  // ---------- 4. Comment ça marche : client → contrat → échéance
  function lifeCaption(d) {
    if (d <= 30) return 'Expire dans ' + d + ' jours';
    if (d <= 60) return d + ' jours restants';
    var m = Math.round(d / 30.44);
    return m + ' mois restant' + (m > 1 ? 's' : '');
  }
  SC.s4 = function (t) {
    var t0 = B(9);
    drift('s4-b1', t, 40, 26, 0.3, 1); drift('s4-b2', t, 30, 22, 0.28, 2.5);
    rise('s4-eye', t, t0 + 0.15, { dist: 20 });
    rise('s4-h', t, t0 + 0.3, { dist: 50, d: 0.9 });

    var step = t < B(10.5) ? 1 : t < B(12) ? 2 : 3;
    ['s4-st1', 's4-st2', 's4-st3'].forEach(function (id, i) {
      rise(id, t, t0 + 0.55 + i * 0.15, { dx: -30, dist: 0 });
      var el = $(id);
      el.classList.toggle('on', i + 1 === step);
      // Seul le texte s'estompe : la pastille reste opaque et masque le rail
      el.querySelector('div').style.opacity = i + 1 === step ? 1 : 0.38;
    });
    T('s4-rail', { o: E.out(P(t, t0 + 0.55, 0.6)) });
    var rail = 0.5 * E.inOut(P(t, B(10.5) - 0.2, 0.5)) + 0.5 * E.inOut(P(t, B(12) - 0.2, 0.5));
    T('s4-railfill', { sy: Math.max(rail, 0.001) });

    // Étape 1 : la fiche client se remplit
    var kOut = E.inOut(P(t, B(12), 0.5));
    var kc = rise('s4-client', t, t0 + 0.7, { dist: 50, s0: 0.96 });
    if (kc >= 1) T('s4-client', { y: -kOut * 50, o: 1 - kOut });
    pop('s4-av', t, t0 + 1.0, 0.5);
    var n1 = typed($('s4-name'), 'Studio Pixel', t, t0 + 1.25, 15);
    var n2 = typed($('s4-mail'), 'studio@pixel.design', t, t0 + 2.2, 22);
    var blink = Math.floor(t * 2.4) % 2 === 0 ? 1 : 0;
    $('s4-c1').style.opacity = t < t0 + 1.25 ? blink : (n1 < 12 ? 1 : 0);
    $('s4-c2').style.opacity = n1 >= 12 && n2 < 19 ? (t < t0 + 2.2 ? blink : 1) : 0;
    pop('s4-sector', t, t0 + 3.1, 0.45);
    pop('s4-status', t, t0 + 3.4, 0.45);

    // Étape 2 : le contrat se rattache et sa durée de vie s'affiche
    var t2 = B(10.5);
    var kk = rise('s4-contract', t, t2 + 0.1, { dist: 60, s0: 0.96 });
    var kUp = E.inOut(P(t, B(12), 0.7));
    if (kk >= 1) T('s4-contract', { y: -260 * kUp });
    $('s4-amount').textContent = euros(6200 * E.out(P(t, t2 + 0.5, 0.9)));
    pop('s4-active', t, t2 + 1.6, 0.45);

    // Étape 3 : le temps passe, l'échéance approche, l'alerte se déclenche
    var t3 = B(12);
    var kd = E.inOut(P(t, t3 + 0.75, 1.8));
    var days = Math.round(lerp(300, 12, kd));
    var start = 0.18 * E.out(P(t, t2 + 0.9, 1.0));
    var pct = t < t3 + 0.75 ? start : (365 - days) / 365;
    var col = days <= 30 ? 'var(--corail)' : 'var(--vert)';
    $('s4-fill').style.width = (pct * 100).toFixed(2) + '%';
    $('s4-fill').style.background = col;
    var now = $('s4-now');
    now.style.left = (pct * 100).toFixed(2) + '%';
    now.style.boxShadow = '0 0 0 4.5px ' + (days <= 30 ? '#ff7a59' : '#13a06b');
    now.style.opacity = clamp(P(t, t2 + 1.2, 0.3));
    var cap = $('s4-cap');
    cap.textContent = t < t3 + 0.75 ? '10 mois restants' : lifeCaption(days);
    cap.style.color = days <= 30 && t >= t3 + 0.75 ? '#a8391a' : '';
    pop('s4-soon', t, B(13), 0.5);

    rise('s4-count', t, t3 + 0.45, { dist: 40 });
    var j = $('s4-j');
    j.textContent = 'J-' + (t < t3 + 0.75 ? 300 : days);
    j.style.color = days <= 30 && t >= t3 + 0.75 ? 'var(--corail-fonce)' : 'var(--vert)';
    var ka = rise('s4-alert', t, B(13), { dist: 50, s0: 0.92, d: 0.7 });
    if (ka >= 1) T('s4-alert', { s: 1 + Math.exp(-(t - B(13) - 0.7) * 3) * Math.sin((t - B(13)) * 9) * 0.012 });
    rise('s4-final', t, B(13.5), { dist: 26, d: 0.9 });
  };

  // ---------- 5. Tout est tracé : le journal d'audit se remplit tout seul
  SC.s5 = function (t) {
    var t0 = B(14);
    drift('s5-b1', t, 40, 26, 0.3, 0.7); drift('s5-b2', t, 30, 22, 0.28, 1.9);
    rise('s5-eye', t, t0 + 0.15, { dist: 20 });
    rise('s5-l1', t, t0 + 0.3, { dist: 60, d: 0.9 });
    rise('s5-l2', t, t0 + 0.3 + BEAT / 2, { dist: 60, d: 0.9 });
    rise('s5-sub', t, t0 + BEAT * 2, { dist: 26 });
    rise('s5-code', t, t0 + BEAT * 4, { dist: 20 });
    rise('s5-day', t, t0 + 0.4, { dist: 14 });
    T('s5-vline', { sy: Math.max(E.out(P(t, t0 + 0.5, 1.2)), 0.001) });

    [14.5, 15, 15.5, 16].forEach(function (bar, i) {
      var te = B(bar);
      rise('s5-e' + (i + 1), t, te, { dist: 36, d: 0.7 });
      pop('s5-n' + (i + 1), t, te, 0.45);
    });
    [[2, 15], [4, 16]].forEach(function (d) {
      var te = B(d[1]);
      T('s5-x' + d[0], { sx: Math.max(E.out(P(t, te + 0.45, 0.35)), 0.001) });
      pop('s5-v' + d[0], t, te + 0.7, 0.45);
    });
  };

  // ---------- 6. Les avantages : six tuiles sur les croches
  SC.s6 = function (t) {
    var t0 = B(17);
    drift('s6-b1', t, 40, 26, 0.3, 1.3); drift('s6-b2', t, 30, 22, 0.28, 0.2);
    rise('s6-eye', t, t0 + 0.15, { dist: 20 });
    rise('s6-h', t, t0 + 0.3, { dist: 50, d: 0.9 });
    for (var i = 0; i < 6; i++) {
      rise('s6-t' + i, t, B(17.5) + i * BEAT / 2, { dist: 56, s0: 0.94, d: 0.85 });
      T('s6-g' + i, { sx: Math.max(E.out(P(t, B(18.75) + i * BEAT / 2, 0.5)), 0.001) });
    }
  };

  // ---------- 7. Sous le capot : les chiffres clés
  SC.s7 = function (t) {
    var t0 = B(20);
    drift('s7-b1', t, 40, 26, 0.3, 2.2); drift('s7-b2', t, 30, 22, 0.28, 0.9);
    rise('s7-eye', t, t0 + 0.15, { dist: 20 });
    rise('s7-h', t, t0 + 0.3, { dist: 50, d: 0.9 });
    var targets = [78, 30, 100, 0];
    targets.forEach(function (v, i) {
      var ts = t0 + 0.75 + i * BEAT / 2;
      rise('s7-c' + i, t, ts, { dist: 50, d: 0.8 });
      var n = Math.round(v * E.out(P(t, ts, 1.0)));
      $('s7-v' + i).innerHTML = i === 2 ? n + ' <small>%</small>' : String(n);
      if (i > 0) T('s7-s' + (i - 1), { o: P(t, ts, 0.5), sy: Math.max(E.out(P(t, ts, 0.6)), 0.001) });
    });
    S.chips7.forEach(function (el, i) { rise(el, t, B(21) + i * 0.1, { dist: 24, d: 0.6 }); });
  };

  // ---------- 8. À vous de jouer : la signature
  SC.s8 = function (t) {
    var t0 = B(22);
    drift('s8-b1', t, 40, 26, 0.3, 0.4); drift('s8-b2', t, 30, 22, 0.28, 1.4);
    pop('s8-mark', t, t0 + 0.15, 0.6);
    S.word8.forEach(function (el, i) {
      var k = E.expo(P(t, t0 + 0.3 + i * 0.045, 0.6));
      T(el, { y: (1 - k) * 40, o: clamp(k * 1.6) });
    });
    rise('s8-l1', t, t0 + 0.6, { dist: 70, d: 1.0 });
    rise('s8-l2', t, t0 + 0.6 + BEAT, { dist: 70, d: 1.0 });
    rise('s8-url', t, B(23), { dist: 26 });
    rise('s8-author', t, B(23) + BEAT, { dist: 16 });
  };

  // ================================================================== HUD
  function hud(t, cur, dark) {
    var h = $('hud');
    h.classList.toggle('dark', dark);
    var on = t >= 3.65;
    var brandOut = P(t, B(22), 0.35);
    T('hud-brand', { o: on ? 1 - brandOut : 0 });

    var s = TL.scenes[cur];
    var kc = E.out(P(t, Math.max(B(s.from), 3.7), 0.5));
    $('hud-num').textContent = String(cur + 1).padStart(2, '0');
    $('hud-label').textContent = s.label;
    T('hud-chapter', { y: (1 - kc) * 12, o: t < 3.7 ? 0 : kc * (1 - brandOut) });

    var renewed = t >= B(23.4);
    var left = Math.max(0, Math.ceil(DUR - t - 1e-6));
    $('hud-n').textContent = renewed ? 'Renouvelé' : 'J-' + left;
    $('hud-l').textContent = renewed ? 'échéance tenue' : "avant l'échéance";
    T('hud-count', { o: t < 3.7 ? 0 : E.out(P(t, 3.7, 0.5)) });

    // Fil du temps : vert tant qu'il reste plus de 30 « jours », corail ensuite, vert au renouvellement.
    // Il prend le relais du trait d'ouverture au moment où celui-ci se pose.
    T('tl', { o: P(t, 2.75, 0.25) });
    var progress = clamp(t / DUR);
    T('tl-fill', { sx: Math.max(progress, 0.0005) });
    var urgent = DUR - t <= 30 && !renewed;
    $('tl-fill').style.background = urgent ? 'var(--corail)' : 'var(--vert)';
    var dot = $('tl-dot');
    var pulse = renewed ? 1 + Math.sin((t - B(23.4)) * 8) * 0.12 * Math.exp(-(t - B(23.4)) * 1.5) : 1;
    dot.style.transform = 'translate3d(' + (progress * 1728).toFixed(2) + 'px,0,0) scale(' + pulse + ')';
    dot.style.background = renewed ? 'var(--vert)' : 'var(--corail)';
    S.labels.forEach(function (l, i) { l.classList.toggle('on', i === cur); });
  }

  // ================================================================== RENDU
  function render(t) {
    t = clamp(t, 0, DUR - 1e-4);
    var cur = 0;
    TL.scenes.forEach(function (s, i) { if (t >= B(s.from)) cur = i; });
    var next = TL.scenes[cur + 1];

    // Transition vers la scène suivante : elle se dévoile par-dessus la scène courante
    var trType = null, k = 0;
    if (next && next.in) {
      var d = next.in === 'disc' ? 0.6 : 0.55;
      var b = B(next.from);
      if (t >= b - d) { trType = next.in; k = (t - (b - d)) / d; }
    }

    TL.scenes.forEach(function (s, i) {
      var el = $(s.id);
      var show = i === cur || (trType && i === cur + 1);
      el.style.display = show ? 'block' : 'none';
      el.style.zIndex = i === cur + 1 ? 3 : 2;
      el.style.clipPath = 'none';
    });

    var a = $('tr-a'), bL = $('tr-b'), ring = $('tr-ring');
    a.style.display = bL.style.display = ring.style.display = 'none';
    if (trType === 'split') {
      // Un trait corail coupe l'écran, puis la scène suivante s'ouvre depuis le fil
      var kl = E.expo(P(k, 0, 0.32));
      var ko = E.inOut(P(k, 0.28, 0.72));
      var h = 540 * (1 - ko);
      $(next.id).style.clipPath = 'inset(' + h.toFixed(1) + 'px 0 ' + h.toFixed(1) + 'px 0)';
      a.style.display = bL.style.display = 'block';
      a.style.transform = 'translateY(' + (-ko * 540) + 'px) scaleX(' + kl + ')';
      bL.style.transform = 'translateY(' + (ko * 540) + 'px) scaleX(' + kl + ')';
      a.style.opacity = bL.style.opacity = 1 - P(k, 0.8, 0.2);
    } else if (trType === 'disc') {
      // Le point « aujourd'hui » s'agrandit jusqu'à remplir l'écran
      var cx = 96 + clamp(t / DUR) * 1728, cy = 1014;
      var r = 2300 * E.inOut(k);
      $(next.id).style.clipPath = 'circle(' + r.toFixed(1) + 'px at ' + cx.toFixed(1) + 'px ' + cy + 'px)';
      ring.style.display = 'block';
      ring.style.left = cx + 'px'; ring.style.top = cy + 'px';
      ring.style.transform = 'scale(' + (r / 100).toFixed(4) + ')';
      ring.style.borderWidth = (5 / Math.max(r / 100, 0.05)).toFixed(3) + 'px';
      ring.style.opacity = 1 - P(k, 0.75, 0.25);
    }

    SC[TL.scenes[cur].id](t);
    if (trType) SC[next.id](t);

    var mode = (trType && k > 0.55 ? next : TL.scenes[cur]).mode;
    hud(t, cur, mode === 'dark');
  }

  window.FILM = { render: render, duration: DUR, fps: TL.fps, bar: BAR };

  function ready() {
    init();
    var q = new URLSearchParams(location.search);
    if (q.has('poster')) window.POSTER = true;
    if (q.has('play')) {
      var t0 = performance.now();
      (function loop() { render(((performance.now() - t0) / 1000) % DUR); requestAnimationFrame(loop); })();
    } else {
      render(q.has('t') ? parseFloat(q.get('t')) : 6.8);
    }
    window.FILM_READY = true;
  }
  document.fonts.ready.then(ready);
})();
