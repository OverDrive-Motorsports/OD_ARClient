# Messages d'erreur backend

Catalogue de référence pour `DataError` (`Data/Errors/DataError.cs`) :
d'où vient le tag entre crochets, la structure d'un message, tous les
messages qu'on peut recevoir aujourd'hui, et comment s'en servir dans le code.

## Structure d'un message

```
[Source] message
```

- **`Source`** (entre crochets) : le nom de la classe qui a produit
  l'erreur — pas une catégorie générique, le vrai nom technique
  (`ApiClient`, `ChampionshipMapper`, `DriverMapper`...).
- **`message`** : le détail — quel appel, quel champ, quelles valeurs.

C'est tout. Pas de label de catégorie, pas de couleur, pas d'indice ajouté
(on est revenus dessus — voir plus bas "Pourquoi pas plus").

Ce format vient de `DataError.ToString()` :
```csharp
public override string ToString() => $"[{source}] {message}";
```

## D'où vient le tag `[Source]`

**Ce n'est pas déduit automatiquement** (pas de réflexion, pas de nom de
fichier/stack trace) — c'est une chaîne littérale écrite à la main à chaque
endroit qui construit un `DataError` :

```csharp
new DataError(DataErrorKind.Network, "ApiClient", $"GET {url} failed: ...")
//                                    ^^^^^^^^^^^ le tag, en dur
```

Chaque mapper passe son propre nom de classe. Si un fichier est renommé, le
tag ne suit pas tout seul — il faut le changer à la main dans le `new
DataError(...)` correspondant.

## Tous les messages actuels

### Réseau / HTTP — `DataErrorKind.Network`, source `"ApiClient"`, `LogError`

Produit par `ApiClient.cs` quand `UnityWebRequest.Result != Success`. Le
détail exact (backend injoignable, timeout, 500, 401...) vient de
`request.error` + `request.responseCode` — pas de distinction structurée
entre ces cas, tout tombe dans `Network` (voir tableau) :

```
[ApiClient] GET http://localhost:3000/health failed: Cannot connect to destination host (HTTP 0)
[ApiClient] GET http://localhost:3000/health failed: Request timeout (HTTP 0)
[ApiClient] GET .../championships failed: HTTP/1.1 502 Bad Gateway (HTTP 502)
[ApiClient] GET .../championships failed: HTTP/1.1 500 Internal Server Error (HTTP 500)
[ApiClient] GET .../championships failed: HTTP/1.1 401 Unauthorized (HTTP 401)
[ApiClient] GET .../championships failed: HTTP/1.1 429 Too Many Requests (HTTP 429)
```

### JSON illisible — `DataErrorKind.Deserialize`, source `"ApiClient"`, `LogError`

Produit quand le corps de la réponse n'est pas un JSON valide/attendu :

```
[ApiClient] GET http://localhost:3000/health returned unparseable JSON: Unexpected character encountered while parsing value: <. Path '', line 0, position 0.
```

### Validation d'un mapper — `DataErrorKind.Validation`, `LogWarning`

Un par mapper, deux variantes (champ manquant / champ incohérent) :

```
[ChampionshipMapper] championship missing championshipCode
[ChampionshipMapper] championship code mismatch: target is 'f1', dto is 'wec'

[ChampionshipEventMapper] event missing eventId
[ChampionshipEventMapper] event id mismatch: target is 'evt_1255', dto is 'evt_9999'

[RaceSessionMapper] session missing sessionId
[RaceSessionMapper] session id mismatch: target is 'sess_9998', dto is 'sess_1111'

[DriverMapper] driver missing driverNumber
[DriverMapper] driver number mismatch: target is '63', dto is '1'

[TeamMapper] team missing teamId
[TeamMapper] team id mismatch: target is 'mercedes', dto is 'ferrari'

[StandingEntryMapper] standing entry missing driverNumber
[StandingEntryMapper] standing entry driver mismatch: target is '63', dto is '1'
```

Le cas "mismatch" ne devrait normalement jamais se produire en usage réel —
il ne se déclenche que si `EntityCollectionSync` applique un DTO sur la
mauvaise entity pour une clé donnée (bug interne), pas à cause d'une
mauvaise réponse backend. C'est un garde-fou, pas un cas attendu.

## Pourquoi pas plus (label générique, couleur, indice "quoi vérifier")

On est passés par ces trois idées et on est revenus dessus :
- **Label générique + couleur** (`[NETWORK]` en rouge...) : les balises
  couleur ne s'affichent que dans la Console de l'éditeur — sur un build
  Quest, les logs passent par logcat, qui ne les rend pas (elles
  apparaîtraient telles quelles, en texte brut).
- **Indice "quoi vérifier" ajouté automatiquement** : c'était un texte
  statique par catégorie (`Network` → "check the backend/gateway is
  reachable"), pas une vraie analyse de l'erreur — donc parfois faux (ex:
  une 500 n'est pas un problème d'accessibilité). Retiré pour ne pas
  afficher une piste de résolution potentiellement fausse.

Le format `[Source] message` reste donc volontairement minimal — la
sévérité (`LogError`/`LogWarning`) suffit à distinguer "tout l'appel a
échoué" de "une entrée a été ignorée".

## Comment utiliser les logs d'erreur dans le code

**Toujours passer par `Report()`, jamais reformater/logger soi-même :**

```csharp
yield return ApiClient.Get<HealthStatusDTO>("/health",
    data => Debug.Log("ok"),
    error => error.Report());   // <- pas de Debug.LogError(error.ToString()) à la main
```

`Report()` est le seul endroit qui décide `LogWarning` vs `LogError` selon
`kind`. Si cette politique change un jour (ex: un jour vouloir remonter les
erreurs Network à l'utilisateur plutôt que juste les logger), c'est **ce
seul fichier** (`Data/Errors/DataError.cs`) qu'on modifie — tous les appels
existants (`ApiClient`, tous les mappers via `EntityCollectionSync`) en
bénéficient automatiquement, sans y toucher.

**Pour créer une nouvelle erreur** (nouveau mapper, nouvel appel réseau) :

```csharp
return new DataError(DataErrorKind.Validation, "MonNouveauMapper", "raison précise ici");
```

- `kind` : `Network`/`Deserialize` si c'est `Core/Network` qui échoue,
  `Validation` si c'est un mapper qui refuse une entrée.
- `source` : le nom de la classe qui construit l'erreur (voir "D'où vient
  le tag" plus haut).
- `message` : assez précis pour comprendre sans avoir à ouvrir le
  débogueur — inclut les valeurs concernées (`target is 'X', dto is 'Y'`),
  pas juste "erreur de validation".

**Ne jamais** appeler `Debug.Log`/`LogWarning`/`LogError` directement pour
une erreur backend ailleurs dans le projet — ça casserait la centralisation
et on reviendrait au problème d'origine (format/sévérité incohérents
d'un endroit à l'autre).
