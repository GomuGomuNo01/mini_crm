# Générateur de la présentation vidéo

La vidéo `../MiniCrm_presentation.mp4` (60 s, 1920×1080, 30 i/s), son affiche
`../presentation-poster.jpg` et sa musique `../musique.mp3` sont entièrement
produites par ce dossier : aucune séquence filmée, aucune banque d'images,
aucun échantillon sonore.

```bash
cd assets/video/source
python build.py                  # musique + vidéo + affiche + MP3 → assets/video/
python build.py --still 32.6     # une image à l'instant donné (apercu.png)
python build.py --preview 24     # planche contact de toute la vidéo (apercu-planche.jpg)
python build.py --music          # musique seule → assets/video/musique.mp3
```

Prérequis : Python 3.10+, `pip install numpy pillow selenium imageio-ffmpeg`
(imageio-ffmpeg fournit ffmpeg) et Google Chrome. Les polices Instrument Serif
et Plus Jakarta Sans (SIL Open Font License) et les icônes Bootstrap Icons
(MIT) sont chargées par Chrome au moment du rendu. Rendu complet : quelques
minutes avec 6 instances de Chrome en parallèle.

| Fichier | Rôle |
|---|---|
| `timeline.json` | 24 mesures à 96 BPM (une mesure = 2,5 s) : les 8 scènes commencent sur un premier temps |
| `film.html` | La composition : les 8 scènes, l'instrumentation et les transitions, en HTML |
| `film.css` | Charte « Échéance » : palette, typographies, composants à l'échelle vidéo |
| `film.js` | Moteur : `render(t)` dessine l'image à l'instant *t*, fonction pure du temps |
| `music.py` | Musique originale synthétisée (numpy) et calée sur l'image |
| `build.py` | Rendu parallèle par Chrome sans interface, encodage H.264 + AAC, affiche, MP3 |

`film.html` s'ouvre aussi directement dans un navigateur : `film.html?t=32.6`
affiche une image fixe, `film.html?play` joue l'animation en temps réel.

## Technique

Chaque image est une page HTML que Chrome sans interface capture à 1920×1080.
`render(t)` ne dépend que du temps : aucune animation CSS, aucun état conservé
d'une image à l'autre, si bien que les 1 800 images peuvent être calculées dans
le désordre, en parallèle, et donnent toujours le même résultat. ffmpeg les
assemble avec la musique (H.264, AAC 192 kb/s, `faststart` pour la lecture en
ligne).

## Charte « Échéance »

Propre à cette vidéo, différente de celles des autres projets du portfolio :
**la vidéo est un contrat de 60 secondes**. Un fil du temps traverse le bas de
l'écran de « Signature » à « Échéance » ; le point corail « aujourd'hui »
avance ; un compte à rebours `J-60 → J-0` tourne en haut à droite. Le fil est
vert tant qu'il reste plus de 30 « jours », passe au corail à J-30 (comme les
contrats dans l'application), puis redevient vert à la fin : « Renouvelé,
échéance tenue ».

- **Palette** : porcelaine lilas (`#F6F3F8`), prune lie-de-vin (`#6A2479`, nuit
  `#170820`), un seul accent corail (`#FF7A59`) réservé au temps qui passe.
- **Typographies** : Instrument Serif pour les titres et les chiffres (italique
  en dégradé prune → corail pour les mots clés), Plus Jakarta Sans pour tout le
  reste.
- **Motifs** : le logo (un point relié par un arc), le fil du temps, la barre de
  durée de vie d'un contrat, le tampon encreur.
- **Transitions** : *l'ouverture* (un trait corail coupe l'écran, la scène
  suivante s'ouvre depuis le fil) et *la pastille* (le point « aujourd'hui »
  s'agrandit jusqu'à remplir l'écran).
- **Mouvement** : les courbes de l'interface de Mini-CRM (sortie rapide, léger
  rebond), entrées « monte et se pose », décalages sur les temps de la musique.

| Scène | Mesures | Contenu |
|---|---|---|
| Ouverture | 0-3 | Le fil se trace, le point tombe et devient le logo, la promesse mot à mot |
| Le constat | 3-6 | Tableurs, post-it, question sans réponse ; le compte à rebours, puis le tampon « EXPIRÉ » |
| La solution | 6-9 | Un seul espace : le tableau de bord se monte, les trois piliers |
| Comment ça marche | 9-14 | Le client se saisit, le contrat se rattache, J-300 → J-12 et l'alerte se déclenche |
| Tout est tracé | 14-17 | Le journal d'audit se remplit, « avant → après » rayé puis corrigé |
| Les avantages | 17-20 | Six tuiles sur les croches |
| Sous le capot | 20-22 | 78 tests, 30 jours, 100 % tracé, 0 framework front-end, la pile technique |
| À vous de jouer | 22-24 | La signature, l'adresse du dépôt ; le contrat est renouvelé |

## Musique

Groove chaleureux et balancé à 96 BPM, en fa majeur, grille
Si♭maj9 – Lam9 – Solm9 – Do9sus4 ; le constat passe en ré mineur
(Rém9 – Si♭maj9 – La7sus4 → La7), la fin se pose sur Famaj9. Chaque son est
calculé : piano électrique en synthèse FM, nappes additives désaccordées,
basse, kick à hauteur descendante, clap en trois impulsions, charleston
balancé, tic-tac d'horloge, célesta aux partiels inharmoniques, pincé type
marimba, tampon encreur, souffles de transition, réverbération par
convolution et légère respiration sous le kick.

Elle suit l'image : intro sans batterie sur le tic-tac (le temps qui passe),
tampon et accord dissonant quand le contrat expire, groove complet dès que la
solution apparaît, clics de frappe pendant la saisie du client, carillon quand
l'alerte se déclenche, un tampon par entrée du journal d'audit, une note par
tuile, pause rythmique sur les chiffres, et résolution sur Famaj9 quand le
contrat est renouvelé.

Musique et vidéo sont produites pour ce projet par le code : aucun extrait
d'une œuvre existante, aucun droit tiers.
