"""Génère la présentation vidéo de Mini-CRM : musique, images, encodage, affiche.

    python build.py                 musique + vidéo + affiche + MP3 → assets/video/
    python build.py --still 32.6    une image à l'instant donné → apercu.png
    python build.py --preview 24    planche contact de toute la vidéo → apercu-planche.jpg
    python build.py --music         musique seule → assets/video/musique.mp3

Prérequis : Python 3.10+, `pip install numpy pillow selenium imageio-ffmpeg`,
Google Chrome. Les polices (Instrument Serif, Plus Jakarta Sans) et les icônes
sont chargées par Chrome au moment du rendu.
"""
import argparse
import base64
import json
import shutil
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ICI = Path(__file__).resolve().parent
SORTIE = ICI.parent
TL = json.loads((ICI / "timeline.json").read_text(encoding="utf-8"))
FPS = TL["fps"]
BAR = 60 / TL["bpm"] * TL["beatsPerBar"]
DUREE = TL["bars"] * BAR
NB_IMAGES = round(DUREE * FPS)
TRAVAIL = ICI / ".build"
VIDEO = SORTIE / "MiniCrm_presentation.mp4"


def ffmpeg():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError:
        exe = shutil.which("ffmpeg")
        if not exe:
            sys.exit("ffmpeg introuvable : pip install imageio-ffmpeg")
        return exe


def ecrire_timeline_js():
    (ICI / "timeline.js").write_text(
        "// Généré par build.py depuis timeline.json : ne pas modifier à la main.\n"
        "window.TL = " + json.dumps(TL, ensure_ascii=False) + ";\n", encoding="utf-8")


# ------------------------------------------------------------------ musique

def musique():
    sys.path.insert(0, str(ICI))
    import music
    TRAVAIL.mkdir(exist_ok=True)
    debut = time.time()
    audio = music.composer(TL)
    wav = TRAVAIL / "musique.wav"
    music.ecrire_wav(wav, audio)
    subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-i", str(wav),
                    "-codec:a", "libmp3lame", "-b:a", "192k",
                    "-metadata", "title=Mini-CRM, échéance (musique originale)",
                    "-metadata", "artist=Dibie Elisee Jules Cedric KOUADIO",
                    str(SORTIE / "musique.mp3")], check=True)
    print(f"musique : {audio.shape[1] / music.SR:.1f} s en {time.time() - debut:.1f} s")
    return wav


# ------------------------------------------------------------------ images

def navigateur():
    from selenium import webdriver
    opts = webdriver.ChromeOptions()
    for a in ("--headless=new", "--hide-scrollbars", "--force-device-scale-factor=1",
              "--window-size=1920,1080", "--disable-gpu-vsync", "--mute-audio"):
        opts.add_argument(a)
    d = webdriver.Chrome(options=opts)
    d.execute_cdp_cmd("Emulation.setDeviceMetricsOverride",
                      {"width": 1920, "height": 1080, "deviceScaleFactor": 1, "mobile": False})
    d.get((ICI / "film.html").as_uri())
    for _ in range(200):
        if d.execute_script("return window.FILM_READY === true"):
            break
        time.sleep(0.1)
    else:
        sys.exit("la composition ne s'est pas chargée (polices ?)")
    ok = d.execute_script("return document.fonts.check('80px \"Instrument Serif\"') && document.fonts.check('600 20px \"Plus Jakarta Sans\"')")
    if not ok:
        sys.exit("polices non chargées : vérifier la connexion à Google Fonts")
    return d


def capture(d, t, fmt="jpeg", qualite=94, affiche=False):
    d.execute_script("window.POSTER = arguments[1]; FILM.render(arguments[0]);", t, affiche)
    opts = {"format": fmt}
    if fmt == "jpeg":
        opts["quality"] = qualite
    return base64.b64decode(d.execute_cdp_cmd("Page.captureScreenshot", opts)["data"])


def rendre_images(workers=4):
    dossier = TRAVAIL / "images"
    shutil.rmtree(dossier, ignore_errors=True)
    dossier.mkdir(parents=True)
    debut = time.time()

    def lot(w):
        d = navigateur()
        try:
            for i in range(w, NB_IMAGES, workers):
                (dossier / f"f{i:05d}.jpg").write_bytes(capture(d, i / FPS))
                if w == 0 and i % (workers * 60) == 0:
                    print(f"  image {i}/{NB_IMAGES}", flush=True)
        finally:
            d.quit()

    with ThreadPoolExecutor(workers) as ex:
        list(ex.map(lot, range(workers)))
    print(f"images : {NB_IMAGES} en {time.time() - debut:.0f} s")
    return dossier


def encoder(images, wav):
    subprocess.run([
        ffmpeg(), "-y", "-loglevel", "error",
        "-framerate", str(FPS), "-i", str(images / "f%05d.jpg"),
        "-i", str(wav),
        # Les captures JPEG sont en pleine plage : on repasse en plage vidéo standard
        # (BT.709), lue à l'identique par tous les lecteurs.
        "-vf", "scale=in_range=full:out_range=tv:out_color_matrix=bt709,format=yuv420p",
        "-c:v", "libx264", "-preset", "slow", "-crf", "20",
        "-color_range", "tv", "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709",
        "-profile:v", "high", "-tune", "animation",
        "-c:a", "aac", "-b:a", "192k",
        "-movflags", "+faststart", "-shortest",
        "-metadata", "title=Mini-CRM en 60 secondes",
        "-metadata", "artist=Dibie Elisee Jules Cedric KOUADIO",
        str(VIDEO)], check=True)
    print(f"vidéo : {VIDEO.name} ({VIDEO.stat().st_size / 1e6:.1f} Mo)")


def affiche():
    d = navigateur()
    try:
        img = capture(d, 6.9, qualite=86, affiche=True)
    finally:
        d.quit()
    (SORTIE / "presentation-poster.jpg").write_bytes(img)
    print("affiche : presentation-poster.jpg")


# ------------------------------------------------------------------ aperçus

def image_fixe(t):
    d = navigateur()
    try:
        (ICI / "apercu.png").write_bytes(capture(d, t, fmt="png"))
    finally:
        d.quit()
    print(f"apercu.png ({t} s)")


def planche(n):
    from io import BytesIO
    from PIL import Image, ImageDraw
    d = navigateur()
    w, h, cols = 480, 270, 4
    rows = (n + cols - 1) // cols
    sheet = Image.new("RGB", (w * cols, (h + 26) * rows), "white")
    draw = ImageDraw.Draw(sheet)
    try:
        for k in range(n):
            t = (k + 0.5) * DUREE / n
            im = Image.open(BytesIO(capture(d, t))).resize((w, h))
            x, y = (k % cols) * w, (k // cols) * (h + 26)
            sheet.paste(im, (x, y + 26))
            draw.text((x + 6, y + 6), f"{t:5.2f} s", fill="black")
    finally:
        d.quit()
    sheet.save(ICI / "apercu-planche.jpg", quality=86)
    print("apercu-planche.jpg")


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--still", type=float)
    ap.add_argument("--preview", type=int)
    ap.add_argument("--music", action="store_true")
    ap.add_argument("--workers", type=int, default=4)
    a = ap.parse_args()
    ecrire_timeline_js()
    if a.still is not None:
        image_fixe(a.still)
    elif a.preview:
        planche(a.preview)
    elif a.music:
        musique()
    else:
        wav = musique()
        encoder(rendre_images(a.workers), wav)
        affiche()
