# Téléchargement et lecture d'un replay complet de course

Process pour télécharger dans le front l'intégralité des données d'une
course (tous pilotes, tous timestamps) dans une seule Entity, et l'utiliser
pour un replay. Complète
[UI-Refresh-Pattern.md](./UI-Refresh-Pattern.md) — mais ce cas est
volontairement différent : ce n'est **pas** une donnée qu'on rafraîchit,
c'est un snapshot figé téléchargé une fois puis parcouru localement.

## 1. Couverture complète — vérification catégorie par catégorie

Exigence : **tous les pilotes, toute la course, tous les timestamps
disponibles, toute donnée disponible**. Vérification de chaque catégorie
qu'on a déjà modélisée avant de dire que l'endpoint/l'Entity est complet :

| Catégorie | Par pilote ? | Historique complet confirmé ? |
|---|---|---|
| Position | Oui | **Non** — endpoint actuel ne donne que le dernier échantillon (voir section suivante) |
| Laps | Oui | Oui |
| Stints | Oui | Oui (nature même de la donnée : un stint = un intervalle de tours) |
| Pit stops | Oui | Oui (événements discrets, déjà complets) |
| Speed | Oui | Oui |
| Engine | Oui | Oui |
| Location | Oui | Oui |
| Intervals | Oui | **Non** — endpoint actuel ne donne que le dernier échantillon |
| Race control | Non (piste entière) | Oui (mais long-poll — voir plus bas) |
| Weather | Non (piste entière) | Oui |
| Team radio | Oui | Oui |
| Drivers (roster) | — | N/A, pas une série temporelle |
| Teams (roster) | — | N/A, pas une série temporelle |
| Standings | Oui | **Non — pas une série temporelle du tout** aujourd'hui : l'endpoint donne un classement courant, sans timestamp. Impossible d'inclure "à tous les timestamps" tant que le backend n'expose pas un historique de classement. |

Deux vrais trous empêchent de satisfaire l'exigence telle quelle
aujourd'hui : **position** et **intervals** n'ont pas d'historique
confirmé, et **standings** n'a même pas de dimension temporelle du tout. Il
faut lever ça avec le backend avant que "toutes les infos à tous les
timestamps" soit vrai à 100% — sinon ce sont les seules données qui
resteront figées à leur dernière valeur connue pendant tout le replay.

## 2. Le nouvel endpoint (proposition backend)

`GET /sessions/{sessionId}/race/full` — agrège tout ce qu'on a déjà
modélisé, pour tous les pilotes, sur toute la course, y compris les
rosters (drivers/teams) et les métadonnées de session pour l'affichage :

```json
{
  "sessionId": "openf1:session:9998",
  "session": { /* SessionDTO - nom, circuit, horaires... pour l'affichage */ },
  "drivers": [ /* SessionDriverDTO[] */ ],
  "teams": [ /* TeamDTO[] */ ],
  "positions": [ /* RacePositionDTO[] - historique complet, pas juste le dernier (à confirmer) */ ],
  "laps": [ /* RaceLapsDTO[] - un par pilote, déjà complet aujourd'hui */ ],
  "stints": [ /* StintDTO[] */ ],
  "pitStops": [ /* PitStopDTO[] */ ],
  "raceControl": [ /* RaceControlEventDTO[] */ ],
  "weather": [ /* RaceWeatherSampleDTO[] */ ],
  "radio": [ /* TeamRadioMessageDTO[] */ ],
  "telemetry": {
    "63": { "speed": [...], "engine": [...], "location": [...], "intervals": [...] },
    "1":  { "speed": [...], "engine": [...], "location": [...], "intervals": [...] }
  }
}
```

Réutilise tous les DTO existants — rien de nouveau à modéliser côté champs,
juste la forme d'agrégation. `standings` volontairement absent de cette
liste : rien à agréger tant qu'il n'y a pas d'historique côté backend.

## 3. Le téléchargement — pourquoi ce n'est pas un simple `ApiClient.Get<T>`

Un `ApiClient.Get<T>` classique fait le parse JSON **de façon synchrone sur
le thread principal**, dans la coroutine, une fois la réponse reçue.
Pour une réponse de quelques Ko (tout ce qu'on a fait jusqu'ici), invisible.
Pour un payload de toute une course (potentiellement des centaines de
milliers d'échantillons télémétrie), ce parse synchrone bloquerait l'app
plusieurs secondes — inacceptable en VR (le même problème de "hitch" déjà
discuté pour le polling haute fréquence, ici en pire vu la taille).

**Deux options, pas de bonne solution magique :**

- **Paginer/découper le téléchargement** (par pilote, ou par catégorie de
  donnée) : plusieurs appels `ApiClient.Get` plus petits, assemblés au fur
  et à mesure dans la même Entity. Permet aussi d'afficher une vraie
  progression ("chargement pilote 3/20..."). Demande que le backend expose
  l'endpoint de façon découpable (ex: `?driverNumber=` sur `/race/full`).
- **Parser en arrière-plan** : télécharger le payload complet (une requête),
  puis désérialiser dans une tâche hors du thread principal — la même
  technique que le `LiveSocket` websocket qu'on avait construit puis
  retiré (lecture + parse JSON dans une `Task`, jamais sur le thread Unity),
  mais cette fois justifiée puisque c'est un besoin réel confirmé, pas
  anticipé. Un seul gros appel réseau, mais le parse ne bloque jamais l'app.

Recommandation : commencer par le découpage par pilote si le backend peut
le faire simplement — ça donne une progression gratuite en plus d'éviter
le hitch, alors que le parse en arrière-plan résout seulement le hitch.

## 4. L'Entity — organisée pour la lecture, pas pour la mise à jour

`RaceReplay` ne suit **pas** le pattern `EntityCollectionSync` des autres
Entities (pas de réconciliation par clé au fil des fetchs — ici on peuple
une fois, point). Ce qui compte à la place : que chaque série soit
**triée par timestamp et indexée par pilote**, pour permettre une lecture
rapide pendant le replay :

```csharp
public class RaceReplay
{
    public string sessionId;
    public RaceSession session;      // métadonnées pour l'affichage (nom, circuit, horaires...)
    public List<Driver> drivers = new();
    public List<Team> teams = new();

    // Chaque liste triée par timestamp croissant, une entrée par pilote.
    public Dictionary<int, List<RacePosition>> positionsByDriver = new();
    public Dictionary<int, List<Lap>> lapsByDriver = new();
    public Dictionary<int, List<Stint>> stintsByDriver = new();
    public Dictionary<int, List<PitStop>> pitStopsByDriver = new();
    public Dictionary<int, List<SpeedSample>> speedByDriver = new();
    public Dictionary<int, List<EngineSample>> engineByDriver = new();
    public Dictionary<int, List<LocationSample>> locationByDriver = new();
    public Dictionary<int, List<DriverIntervals>> intervalsByDriver = new();

    // Pas de dimension pilote - déjà globales à la session.
    public List<RaceControlEvent> raceControl = new();
    public List<WeatherSample> weather = new();
    public List<TeamRadioMessage> radio = new();
}
```

Le mapper (`RaceReplayMapper.TryApply`) réutilise tous les mappers
individuels déjà écrits (`RacePositionMapper`, `DriverLapsMapper`...) pour
construire chaque entrée, exactement comme `EntityCollectionSync` le fait
déjà — la différence ici est qu'on n'a pas besoin de réconcilier avec un
état précédent, juste de remplir les listes une fois puis de les trier.

## 5. Utilisation pour le replay

Le replay ne "reçoit" jamais de nouvelles données — il **cherche** dans
`RaceReplay` la valeur valide à un instant donné, piloté par une horloge de
lecture (`currentTime`, en secondes depuis le départ, ou un timestamp
absolu) :

```csharp
public class RaceReplayPlayer : MonoBehaviour
{
    public RaceReplay replay;
    public float playbackSpeed = 1f;
    private DateTime _currentTime;

    private void Update()
    {
        _currentTime = _currentTime.AddSeconds(Time.deltaTime * playbackSpeed);
        Repaint();
    }

    private void Repaint()
    {
        foreach (var driverNumber in replay.positionsByDriver.Keys)
        {
            RacePosition pos = LatestAt(replay.positionsByDriver[driverNumber], _currentTime);
            if (pos != null) UpdateDriverMarker(driverNumber, pos);
        }
    }

    // Recherche binaire : la liste est triée par timestamp, donc O(log n) au lieu de O(n) par frame.
    private RacePosition LatestAt(List<RacePosition> samples, DateTime time) { /* ... */ }
}
```

Même principe pour la vitesse (jauge alimentée par `speedByDriver`), la
position sur piste (`locationByDriver`), etc. — un seul mécanisme de
recherche générique (par timestamp, dans une liste triée) réutilisé pour
chaque type de donnée, plutôt qu'un code de lecture différent par widget.

**Scrubbing/avance rapide** : comme tout est déjà en mémoire et indexé,
avancer `_currentTime` de n'importe quel montant (y compris en arrière)
fonctionne immédiatement — c'est l'avantage direct du gros téléchargement
par rapport au SSE, qui ne peut que lire en avant au rythme du serveur.

## Résumé

| Étape | Ce qui se passe |
|---|---|
| Téléchargement | Un ou plusieurs appels réseau vers `/race/full`, parse hors thread principal ou découpé par pilote |
| Construction | `RaceReplayMapper` peuple `RaceReplay` une fois, listes triées par timestamp par pilote |
| Lecture | `RaceReplayPlayer` avance une horloge locale, cherche la valeur "au plus proche avant" `currentTime` dans chaque liste, redessine |
| Scrub/vitesse | Changer `currentTime` directement — aucun aller-retour réseau, tout est déjà en mémoire |
