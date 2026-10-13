"""Musique originale de la présentation Mini-CRM, synthétisée par le code (numpy).

Aucun échantillon, aucun extrait d'œuvre existante : chaque son est calculé à
partir d'oscillateurs et de bruit filtré. La musique est donc libre de droits.

Groove chaleureux et balancé à 96 BPM, en fa majeur :
Si♭maj9 – Lam9 – Solm9 – Do9sus4 ; le constat passe en ré mineur, la fin se
pose sur Famaj9. Une mesure = 2,5 s, 24 mesures = 60 s, calées sur l'image.

Elle suit le récit : un tic-tac d'horloge quand le temps presse, un tampon
quand le contrat expire et à chaque entrée du journal d'audit, un carillon
quand l'alerte se déclenche, des clics de frappe pendant la saisie, un souffle
à chaque transition.
"""
import numpy as np

SR = 44100
RNG = np.random.default_rng(2026)


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


class Piste:
    """Bus stéréo : un signal sec et un envoi vers la réverbération."""

    def __init__(self, dur):
        self.n = int(dur * SR)
        self.dry = np.zeros((2, self.n))
        self.wet = np.zeros((2, self.n))

    def add(self, t0, sig, gain=1.0, pan=0.0, send=0.0):
        if sig.ndim == 1:
            l, r = np.cos((pan + 1) * np.pi / 4), np.sin((pan + 1) * np.pi / 4)
            sig = np.vstack([sig * l * 1.414, sig * r * 1.414])
        i = int(round(t0 * SR))
        if i >= self.n:
            return
        j = min(self.n, i + sig.shape[1])
        part = sig[:, : j - i] * gain
        self.dry[:, i:j] += part
        if send:
            self.wet[:, i:j] += part * send


# ------------------------------------------------------------------ outils

def temps(dur):
    return np.arange(int(dur * SR)) / SR


def attaque(sig, ms=4):
    n = min(len(sig), int(SR * ms / 1000))
    sig[:n] *= np.linspace(0, 1, n)
    return sig


def filtre(x, lo=None, hi=None):
    """Filtre passe-bande par masque de fréquences (pour les sons courts)."""
    X = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    m = np.ones_like(f)
    if lo:
        m *= 1 / (1 + (lo / np.maximum(f, 1)) ** 4)
    if hi:
        m *= 1 / (1 + (f / hi) ** 4)
    return np.fft.irfft(X * m, len(x))


def bruit(dur):
    return RNG.standard_normal(int(dur * SR))


# ------------------------------------------------------------------ instruments

def piano(f, dur, vel=0.5, brillance=1.0):
    """Piano électrique (synthèse FM) : la chaleur des accords."""
    t = temps(dur + 1.4)
    idx = 1.7 * np.exp(-t * 5.5) * brillance + 0.22
    s = np.sin(2 * np.pi * f * t + idx * np.sin(2 * np.pi * f * t))
    s += 0.12 * brillance * np.sin(2 * np.pi * f * 14 * t) * np.exp(-t * 28)
    env = np.exp(-t * 1.25)
    env *= np.where(t > dur, np.exp(-(t - dur) * 7), 1.0)
    return attaque(s * env * vel, 3)


def nappe(notes, dur, vel=0.12, ouverture=0.8):
    """Nappe douce : dents de scie additives désaccordées, attaque lente."""
    t = temps(dur + 1.0)
    s = np.zeros_like(t)
    for m in notes:
        for detune in (-0.07, 0.07):
            f = hz(m + detune)
            for h in range(1, 9):
                s += np.sin(2 * np.pi * f * h * t + h) / h ** 1.7
    env = np.minimum(t / ouverture, 1.0) * np.where(t > dur, np.exp(-(t - dur) * 3), 1.0)
    return s * env * vel / len(notes)


def basse(f, dur, vel=0.5):
    t = temps(dur + 0.15)
    s = np.sin(2 * np.pi * f * t) + 0.22 * np.sin(4 * np.pi * f * t) + 0.06 * np.sin(6 * np.pi * f * t)
    env = np.minimum(t / 0.008, 1) * (0.75 + 0.25 * np.exp(-t * 5))
    env *= np.where(t > dur, np.exp(-(t - dur) * 30), 1.0)
    return s * env * vel


def kick(vel=1.0):
    t = temps(0.5)
    f = 46 + 100 * np.exp(-t * 30)
    s = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 7.5)
    click = filtre(bruit(0.5), lo=1500) * np.exp(-t * 300) * 0.25
    return attaque((s + click) * vel, 1)


def clap(vel=0.5):
    t = temps(0.35)
    n = filtre(bruit(0.35), lo=900, hi=5200)
    env = np.zeros_like(t)
    for k, d in enumerate((0.0, 0.009, 0.018)):
        env += np.where(t >= d, np.exp(-(t - d) * 160), 0) * (0.8 + 0.1 * k)
    env += np.where(t >= 0.02, np.exp(-(t - 0.02) * 14), 0) * 0.55
    return n * env * vel * 0.5


def charley(vel=0.3, ouvert=False):
    d = 0.18 if ouvert else 0.06
    t = temps(d)
    return filtre(bruit(d), lo=7000) * np.exp(-t * (18 if ouvert else 70)) * vel * 0.4


def tic(aigu=True, vel=0.4):
    """Tic-tac d'horloge : bloc de bois accordé."""
    t = temps(0.12)
    f = 2350 if aigu else 1650
    s = np.sin(2 * np.pi * f * t) * np.exp(-t * 90) + 0.4 * np.sin(2 * np.pi * f * 1.48 * t) * np.exp(-t * 140)
    return attaque(s * vel, 0.5)


def cloche(f, vel=0.4, dur=2.4):
    """Célesta / cloche : partiels légèrement inharmoniques."""
    t = temps(dur)
    s = np.zeros_like(t)
    for ratio, amp, dec in ((1, 1, 1.6), (2.0, 0.42, 2.6), (3.01, 0.2, 4.0), (4.17, 0.12, 6.0), (5.43, 0.07, 8.0)):
        s += amp * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t * dec)
    return attaque(s * vel, 2)


def pince(f, vel=0.3):
    """Pincé type marimba : l'arpège de la démonstration."""
    t = temps(0.6)
    s = np.sin(2 * np.pi * f * t) * np.exp(-t * 9) + 0.3 * np.sin(2 * np.pi * f * 4 * t) * np.exp(-t * 26)
    return attaque(s * vel, 1.5)


def tampon(vel=0.8, lourd=False):
    """Tampon encreur : un choc grave et un claquement de papier."""
    t = temps(0.6)
    f = (62 if lourd else 78) + 60 * np.exp(-t * 40)
    s = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * (6 if lourd else 11))
    s += filtre(bruit(0.6), lo=400, hi=4000) * np.exp(-t * 55) * (0.7 if lourd else 0.45)
    return attaque(s * vel, 0.5)


def frappe(vel=0.15):
    t = temps(0.03)
    return filtre(bruit(0.03), lo=2500, hi=9000) * np.exp(-t * 260) * vel


def souffle(dur=0.6, vel=0.35, montee=True):
    """Souffle de transition : bruit qui enfle, balayage qui monte."""
    t = temps(dur + 0.25)
    n = filtre(bruit(dur + 0.25), lo=1200, hi=9000)
    k = np.clip(t / dur, 0, 1)
    env = (k ** 2.2 if montee else (1 - k) ** 2) * np.where(t > dur, np.exp(-(t - dur) * 18), 1)
    sweep = np.sin(2 * np.pi * np.cumsum(300 + 1500 * k ** 2) / SR) * 0.15
    return (n * 0.6 + sweep) * env * vel


def reverb(wet, duree=2.3):
    """Réverbération par convolution avec une réponse impulsionnelle synthétique."""
    t = temps(duree)
    out = np.zeros_like(wet)
    for c in range(2):
        ir = filtre(bruit(duree), hi=6500) * np.exp(-t * 3.1)
        ir[: int(0.02 * SR)] = 0
        ir /= np.sqrt(np.sum(ir ** 2))
        size = 1 << int(np.ceil(np.log2(wet.shape[1] + len(ir))))
        out[c] = np.fft.irfft(np.fft.rfft(wet[c], size) * np.fft.rfft(ir, size), size)[: wet.shape[1]]
    return out


# ------------------------------------------------------------------ partition

BB9 = (46, [62, 65, 69, 72])  # Si♭maj9
AM9 = (45, [60, 64, 67, 71])  # Lam9
GM9 = (43, [58, 62, 65, 69])  # Solm9
C9S = (48, [58, 62, 65, 67])  # Do9sus4
DM9 = (38, [53, 57, 60, 64])  # Rém9
A7S = (45, [55, 62, 64, 69])  # La7sus4
A7 = (45, [55, 61, 64, 69])   # La7
F9 = (41, [57, 60, 64, 67])   # Famaj9

# (mesure, temps de début, durée en temps, accord)
GRILLE = [
    (0, 0, 4, BB9), (1, 0, 4, AM9), (2, 0, 2, GM9), (2, 2, 2, C9S),
    (3, 0, 4, DM9), (4, 0, 4, BB9), (5, 0, 2, A7S), (5, 2, 2, A7),
    (6, 0, 4, BB9), (7, 0, 4, AM9), (8, 0, 2, GM9), (8, 2, 2, C9S),
    (9, 0, 4, BB9), (10, 0, 4, AM9), (11, 0, 4, GM9), (12, 0, 4, C9S),
    (13, 0, 4, BB9), (14, 0, 4, AM9), (15, 0, 4, GM9), (16, 0, 4, C9S),
    (17, 0, 4, BB9), (18, 0, 4, AM9), (19, 0, 2, GM9), (19, 2, 2, C9S),
    (20, 0, 4, BB9), (21, 0, 4, C9S), (22, 0, 8, F9),
]

# Motif de célesta : ouverture et signature finale
MOTIF = [
    (0, 0, 77, 1.5), (0, 1.5, 76, 0.5), (0, 2, 74, 2),
    (1, 0, 72, 1.5), (1, 1.5, 74, 0.5), (1, 2, 76, 2),
    (2, 0, 74, 1.5), (2, 1.5, 72, 0.5), (2, 2, 70, 2),
    (22, 0, 77, 1), (22, 1, 76, 1), (22, 2, 72, 2), (23, 0, 76, 1), (23, 1, 77, 3),
]
CONTRECHANT = [(17, 0, 81, 2), (17, 2, 79, 2), (18, 0, 79, 2), (18, 2, 76, 2), (19, 0, 77, 2), (19, 2, 79, 2)]


def composer(tl):
    bpm = tl["bpm"]
    beat = 60 / bpm
    bar = beat * tl["beatsPerBar"]
    dur = tl["bars"] * bar
    B = lambda m, b=0.0: m * bar + b * beat  # noqa: E731
    p = Piste(dur)

    def section(m):
        if m < 3: return "ouverture"
        if m < 6: return "constat"
        if m < 9: return "solution"
        if m < 14: return "demo"
        if m < 17: return "audit"
        if m < 20: return "avantages"
        if m < 22: return "chiffres"
        return "fin"

    kicks = []
    for (m, b0, nb, (bass, voix)) in GRILLE:
        sec = section(m)
        t0 = B(m, b0)
        long_ = nb * beat

        # Nappe : ouverture, constat, chiffres et fin
        if sec in ("ouverture", "constat", "chiffres", "fin"):
            vel = {"ouverture": 0.10, "constat": 0.11, "chiffres": 0.14, "fin": 0.15}[sec]
            p.add(t0, nappe([n - 12 for n in voix[:3]] + voix, long_, vel), send=0.45)

        # Piano électrique : accords tenus au calme, comping balancé dans le groove
        bril = 0.55 if sec in ("constat", "audit") else 1.0
        if sec in ("ouverture", "constat", "chiffres", "fin"):
            hits = [(0, long_ - 0.1, 0.30)]
        else:
            hits = [(0, beat * 1.4, 0.32), (beat * 1.75, beat * 0.5, 0.2), (beat * 2.5, beat * 1.2, 0.26), (beat * 3.5, beat * 0.4, 0.14)]
            hits = [h for h in hits if h[0] < long_]
        for (o, d, v) in hits:
            for i, n in enumerate(voix):
                hum = RNG.uniform(0.85, 1.05)
                p.add(t0 + o + i * 0.009, piano(hz(n), d, v * hum * 0.55, bril), pan=-0.35 + i * 0.23, send=0.28)

        # Basse
        if sec == "ouverture":
            if m == 2:
                p.add(t0, basse(hz(bass), long_ - 0.05, 0.42))
        elif sec in ("constat", "chiffres"):
            for o in range(0, nb, 2):
                p.add(t0 + o * beat, basse(hz(bass), beat * 1.9, 0.45))
        elif sec == "fin":
            p.add(t0, basse(hz(bass), long_, 0.5))
        else:
            for (o, d) in ((0, 1.4), (1.75, 0.5), (2.5, 1.0), (3.5, 0.4)):
                if o < nb:
                    p.add(t0 + o * beat, basse(hz(bass if o != 3.5 else bass + 12), beat * d, 0.5))

        # Batterie, mesure par mesure
        if b0 == 0:
            for mm in range(m, m + max(1, nb // 4)):
                s = section(mm)
                tb = B(mm)
                if s == "constat":
                    for o in (0, 2):
                        kicks.append(tb + o * beat); p.add(tb + o * beat, kick(0.75))
                elif s in ("solution", "demo", "audit", "avantages"):
                    for o in (0, 1.75, 2.5):
                        kicks.append(tb + o * beat); p.add(tb + o * beat, kick(0.95))
                    for o in (1, 3):
                        p.add(tb + o * beat, clap(0.55 if s != "audit" else 0.42), send=0.22)
                    for i in range(16):
                        o = i * 0.25 + (0.06 if i % 2 else 0)
                        v = 0.24 if i % 4 == 2 else 0.13
                        if s == "audit":
                            v *= 0.7
                        p.add(tb + o * beat, charley(v, ouvert=(i == 14)), pan=0.3)
                elif s == "chiffres":
                    for i in range(8):
                        p.add(tb + (i * 0.5 + (0.06 if i % 2 else 0)) * beat, charley(0.16), pan=0.3)
                elif s == "fin" and mm == 22:
                    kicks.append(tb); p.add(tb, kick(0.9))

    # Tic-tac : le temps qui passe (ouverture discrète, constat plus présent)
    for m in range(0, 6):
        for b in range(4):
            v = 0.12 if m < 3 else 0.22
            p.add(B(m, b), tic(b % 2 == 0, v), pan=-0.25 if b % 2 else 0.25, send=0.12)

    # Mélodies de célesta
    for (m, b, n, d) in MOTIF:
        p.add(B(m, b), cloche(hz(n), 0.22, 2.0 + d * beat), pan=0.15, send=0.55)
    for (m, b, n, d) in CONTRECHANT:
        p.add(B(m, b), cloche(hz(n), 0.14, 1.8 + d * beat), pan=-0.2, send=0.6)

    # Arpège de la démonstration (mesures 9 à 13), en croches
    for (m, b0, nb, (bass, voix)) in GRILLE:
        if 9 <= m <= 13:
            notes = [n + 12 for n in voix]
            ordre = [0, 1, 2, 3, 2, 1, 2, 3]
            for i in range(nb * 2):
                p.add(B(m, b0 + i * 0.5), pince(hz(notes[ordre[i % 8]]), 0.16 if i % 2 else 0.22), pan=0.4 if i % 2 else -0.1, send=0.3)

    # ---------- Événements synchronisés avec l'image
    # Transitions : souffle qui monte jusqu'au premier temps de la scène suivante
    for sc in tl["scenes"]:
        if "in" in sc:
            tb = B(sc["from"])
            p.add(tb - 0.62, souffle(0.6, 0.32 if sc["in"] == "split" else 0.26), send=0.25)
            if sc["in"] == "split":
                p.add(tb - 0.55, cloche(hz(93), 0.05, 0.6), pan=0.2, send=0.4)  # le trait corail

    # Ouverture : le point tombe sur le fil, la marque apparaît
    p.add(0.85, cloche(hz(84), 0.16, 1.6), send=0.5)
    p.add(1.75, cloche(hz(89), 0.12, 1.6), pan=-0.2, send=0.5)

    # Constat : compte à rebours puis tampon « EXPIRÉ » (cluster dissonant)
    for i in range(4):
        p.add(B(4) + 0.6 + i * beat * 0.75, tic(True, 0.3), send=0.15)
    p.add(B(5), tampon(1.0, lourd=True), send=0.3)
    for n in (73, 74, 80):
        p.add(B(5), cloche(hz(n), 0.07, 1.4), send=0.5)

    # Démonstration : frappe du nom et de l'email, carillon de l'alerte
    t9 = B(9)
    for i in range(12):
        p.add(t9 + 1.25 + i / 15 + 0.02, frappe(0.12 + RNG.uniform(0, 0.05)), pan=RNG.uniform(-0.3, 0.3))
    for i in range(19):
        p.add(t9 + 2.2 + i / 22 + 0.02, frappe(0.1 + RNG.uniform(0, 0.04)), pan=RNG.uniform(-0.3, 0.3))
    p.add(B(10.5) + 0.1, tampon(0.35), send=0.2)  # le contrat se rattache
    p.add(B(13), cloche(hz(81), 0.3, 2.4), pan=-0.1, send=0.6)
    p.add(B(13) + beat / 2, cloche(hz(84), 0.3, 2.4), pan=0.1, send=0.6)

    # Journal d'audit : un tampon par entrée
    for m in (14.5, 15, 15.5, 16):
        p.add(B(m), tampon(0.55), send=0.25)

    # Avantages : une note par tuile ; chiffres : une cloche par compteur
    gamme = [70, 74, 77, 81, 76, 79]
    for i in range(6):
        p.add(B(17.5) + i * beat / 2, pince(hz(gamme[i]), 0.22), pan=-0.3 + i * 0.12, send=0.35)
    for i, n in enumerate((74, 77, 81, 84)):
        p.add(B(20) + 0.75 + i * beat / 2, cloche(hz(n), 0.16, 1.8), send=0.5)

    # Fin : le contrat est renouvelé, deux tic-tac et l'accord se pose
    p.add(B(23.4), cloche(hz(89), 0.2, 2.6), send=0.6)
    p.add(B(23, 2), tic(True, 0.12)); p.add(B(23, 3), tic(False, 0.1))

    # ---------- Mixage
    mix = p.dry + reverb(p.wet) * 0.55
    # Respiration : la musique s'efface légèrement sous chaque kick (pompe douce)
    t = np.arange(p.n) / SR
    duck = np.ones(p.n)
    for k in kicks:
        i = int(k * SR); j = min(p.n, i + int(0.3 * SR))
        duck[i:j] *= 1 - 0.18 * np.exp(-(t[i:j] - k) / 0.09)
    mix *= duck
    # Mastering : niveau de travail fixé sur les crêtes usuelles, puis limiteur doux
    # (tanh) qui n'agit que sur les rares pointes : pas de son écrasé ni saturé.
    mix /= np.percentile(np.abs(mix), 99.95) / 0.7
    mix = np.tanh(mix)
    mix /= np.max(np.abs(mix)) / 0.89
    fade = np.ones(p.n)
    nf = int(1.4 * SR)
    fade[-nf:] = np.cos(np.linspace(0, np.pi / 2, nf)) ** 2
    fade[: int(0.02 * SR)] = np.linspace(0, 1, int(0.02 * SR))
    return (mix * fade).astype(np.float32)


def ecrire_wav(chemin, audio):
    import wave
    data = (np.clip(audio.T, -1, 1) * 32767).astype("<i2")
    with wave.open(str(chemin), "wb") as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(data.tobytes())
