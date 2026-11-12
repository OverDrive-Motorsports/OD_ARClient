# Replays : catalogue, téléchargement et lecture

Un replay est **téléchargé sur le casque**, puis lu depuis le fichier local,
en ligne comme hors ligne. Il n'y a qu'un seul chemin de lecture.

## 1. Catalogue — fait

`GET /v1/race-data/championships/{code}/replays` liste les sessions
rejouables : celles dont le backend a stocké la télémétrie **et** les
positions x/y/z. Tous types de session confondus, du plus récent au plus
ancien.

```
ODReplayCatalog → ODReplayService.LoadCatalogAsync()
                    ├── ODBackendApi.GetChampionships()   (nom du championnat)
                    └── ODBackendApi.GetReplays(code)     → ReplayCatalogEntryDto
                  → ODChampionshipMapper.ToReplaySummary() → ReplaySummary
```

Deux requêtes en parallèle, quel que soit le nombre d'événements.

## 2. Téléchargement — fait (pas encore branché dans l'UI)

`GET /v1/race-data/sessions/{sessionId}/race/replay` renvoie toute la
session en un seul JSON, non compressé et sans `Content-Length` :

| Clé | Forme | Contenu |
|---|---|---|
| `telemetry` | par pilote (`"44": [...]`) | vitesse, rpm, rapport, gaz, frein, DRS, tour, **x/y/z**, timestamp |
| `position` | par pilote | position, écarts, tours bouclés, timestamp |
| `radio` | par pilote | URL audio, timestamp |
| `lap` | par pilote | durée et secteurs du tour — **sans timestamp** |
| `stint` | par pilote | pneus, tour de début et de fin — **sans timestamp** |
| `pitStop` | par pilote | tour, durée — **sans timestamp** |
| `raceControl` | liste à plat | drapeaux, Safety Car, messages, timestamp |
| `weather` | liste à plat | températures, vent, pluie, timestamp |

```
ODReplayService.DownloadAsync(summary, progress, token)
  ├── ODReplayStore.BeginDownload()             dossier vide, renvoie replay.json.part
  ├── ODBackendApi.DownloadReplay()             écrit le corps directement sur le disque
  ├── GetSessionResult + GetSessionDrivers      le podium, pour l'afficher hors ligne
  └── ODReplayStore.Commit()                    .part → replay.json, puis meta.json en dernier
```

- **Mémoire** : `DownloadHandlerFile` écrit au fil de l'eau, le JSON n'est
  jamais chargé en RAM pendant le téléchargement.
- **Progression** : en octets reçus (le backend ne donne pas la taille totale).
- **Timeout** : pas de durée maximale, mais abandon après
  `ODApiConfig.downloadStallSeconds` sans recevoir de données.
- **Échec ou annulation** : le dossier est supprimé, rien ne reste.
- **Un seul téléchargement par session** : redemander le même renvoie la tâche en cours.
- Une fois téléchargé, `ODReplayService.LoadDetailAsync` lit le podium
  depuis `meta.json` au lieu d'appeler le backend.

Stockage : voir [Data/Doc/README.md](../../Data/Doc/README.md#downloaded-replays).

## 3. Lecture — basique fait (sans contrôles)

À l'ouverture de la page, le replay est téléchargé si besoin puis lu ; les
voitures attendent sur la grille (« Prêt · appuie sur « Regarder le replay » »).
La course part de l'extinction des feux, en temps réel, quand on appuie sur
« Regarder le replay » (capsule ou écran de la carte, qui allument aussi la TV),
et s'arrête quand on quitte la page (retour galerie, page cachée). Un appui
pendant le téléchargement démarre la course dès qu'elle est prête.

```
ODSportPage.LoadReplay(summary)
  ├── ODReplayService.LoadDetailAsync()       podium (meta.json si téléchargé)
  ├── ODReplayService.DownloadAsync()         seulement s'il n'est pas sur le casque
  ├── ODReplayService.LoadRaceAsync()         Task.Run → ODRaceReplayReader.Read()
  └── ODReplayPlayer.Load(race, summary, ODReplayTrackMap)   grille, horloge arrêtée
ODChampionshipPanel.ReplayWatchPressed → ODSportPage.WatchReplay()
  └── ODReplayPlayer.Begin()
        ├── chaque image     ODReplayMapper.TryGetPosition() → ODReplayTrackMap.MoveCar()
        ├── toutes les 0,5 s ODReplayMapper.ToFrame() → OrderChanged (si changé)
        │                      → RaceRankingManager.ShowEntries + podium
        └── chaque seconde   ProgressChanged → « Tour 12 / 56 · 0:21:45 »
```

- **`ODRaceReplayReader`** lit `replay.json` en flux (`JsonTextReader`) :
  la télémétrie à la main (seuls temps, x, y et vitesse sont gardés), le reste via
  `JObject`. Le replay de Chine 2025 (108 Mo) se lit en ~3 s sur Mac et
  occupe ~12 Mo (odomètre compris).
- **Horloge** : secondes depuis minuit UTC du premier timestamp.
  Départ = message `SESSION STARTED` de la direction de course.
- **Tours** : le `lapNumber` de la télémétrie est celui du leader pour
  toutes les voitures, il n'est donc pas utilisé. Le début du tour n d'une
  voiture = départ + somme de ses temps au tour (≈ 1 s d'écart au drapeau).
- **Classement** : dernière position officielle. Le backend ne donne pas
  d'écarts (`gapAhead` / `gapBehind` toujours nuls) : l'écart au leader est
  calculé là où est la voiture, = il y a combien de temps le leader était au
  même point de la course (`ODReplayMapper.Progress` / `TimeAt` : chaque tour
  coupé en trois aux lignes de secteur, temps régulier dans un secteur). Il
  est donc exact à chaque ligne de secteur et bouge en continu entre deux
  (rafraîchi toutes les 0,5 s). « +n tours » quand le leader a n tours
  d'avance ; « Abandon » quand les tours d'une voiture s'arrêtent plus de
  2 min avant le drapeau à damier.
- **Colonnes** : pneu = pastille aux couleurs Pirelli (S rouge, M jaune,
  H blanc, I vert, W bleu) + âge en tours ; arrêts ; écart ; dernier tour
  (violet = meilleur tour).
- **Carte** : le contour est le tour le plus rapide du vainqueur hors
  stands (`ODTrackLine`), une pastille par voiture aux couleurs de son
  équipe, interpolée entre deux échantillons (cachée si l'écart dépasse 3 s).

**Bug côté backend** : `/race/replay` donne à toutes les voitures les mêmes
x/y (et le même `lapNumber`) : `ListTelemetryFrames` associe à chaque
échantillon la position la plus proche *tous pilotes confondus*. Le lecteur
le détecte (`RaceReplay.sharedPositions` : la plupart des voitures ont
exactement les échantillons de la première) et place alors chaque voiture
sur le contour à partir de **sa propre vitesse**, qui, elle, est juste :
- **Odomètre** (`ReplayCar.odometer`) : mètres parcourus, intégrés depuis la
  vitesse. Dans un tour, la voiture avance selon les mètres parcourus entre
  ses deux passages sur la ligne : lente dans les virages, rapide dans les
  lignes droites. Un tour fait ~5 375 m à Shanghai, à ±10 m près d'un tour
  à l'autre.
- **Grille** : le tour 1 est compté à rebours depuis le premier passage sur
  la ligne. Chaque voiture part donc de sa vraie place, qui peut être après
  la ligne (à Shanghai, la grille est 125 à 170 m après la ligne de
  chronométrage ; départ des stands visible).
- **Ligne de chronométrage** (`RaceReplay.lineOffset`) : les x/y partagés
  suivent une voiture à la fois (et en changent parfois). À chaque
  échantillon, la place a sur le contour correspond à la fraction de tour f
  d'une des voitures. Le pic de a − f sur toute la course donne la ligne
  (19 % du contour pour la Chine 2025). Le tour change donc au bon endroit.

Le passage aux stands ne se voit pas sur la carte. Une fois le backend
corrigé (et le replay retéléchargé), les vraies positions sont utilisées
automatiquement.

**Tester** : dans l'éditeur, en Play mode, l'objet de la page Sport porte un
`ODReplayPlayer` dont le champ `speed` (secondes de course par seconde)
peut être monté à 60 pour parcourir la course en une minute.

À faire ensuite :
1. **Contrôles** : lecture/pause, vitesse, saut (l'horloge est
   `ODReplayPlayer.RaceTime`, la donnée est retrouvée par dichotomie).
2. **Conversion** en `replay.odr` binaire à la fin du téléchargement, pour
   ouvrir le replay sans relire 100 Mo de JSON (augmenter
   `ODReplayStore.FormatVersion`).
3. **Affichage** : drapeaux / Safety Car (direction de course), télémétrie
   du pilote sélectionné, voitures sur le circuit 3D (`CircuitPreview`).

## 4. Galerie — à faire

État par replay (Télécharger → progression → Téléchargé → Supprimer),
« Mes replays » = replays téléchargés (`ODReplayService.Downloaded()`),
et, sans réseau, seulement les replays téléchargés.
