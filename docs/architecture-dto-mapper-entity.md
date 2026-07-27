# Architecture DTO / Mapper / Entity / Network — guide de portage

Ce document décrit le pattern utilisé côté client AR (Unity) pour parler au
backend, dans le but que l'équipe front mobile (Flutter) puisse reproduire
la même logique. Le code d'exemple est en pseudocode proche de Dart, pas du
C# — l'idée à copier est la structure et les règles, pas la syntaxe.

## Vue d'ensemble

```
Réseau (HTTP)  →  DTO  →  Mapper  →  Entity  →  UI
```

Quatre rôles bien séparés, chacun avec une seule responsabilité :

| Couche | Rôle | Ne fait jamais |
|---|---|---|
| **DTO** | Miroir exact du JSON reçu | De la logique, de la validation |
| **Mapper** | DTO → Entity, valide, gère les erreurs | Créer une nouvelle Entity à chaque appel |
| **Entity** | Objet utilisable par l'app, stable dans le temps | Connaître la forme du JSON d'origine |
| **Network** | Appelle l'API, désérialise, appelle le Mapper | Décider quoi faire du résultat (ça, c'est à l'écran/au controller) |

## 1. DTO — miroir du JSON, rien d'autre

Une classe par forme de réponse JSON, avec les mêmes noms de champs que le
backend (camelCase). Aucune méthode, aucune logique.

```dart
class ChampionshipDTO {
  String? id;
  String? championshipCode;
  String? name;
  String? provider;
  String? category;
  bool isActive = false;
}
```

**Organisation des fichiers** : grouper plusieurs DTO liés dans un seul
fichier par domaine plutôt qu'un fichier par endpoint (ex: tous les DTO du
"catalogue championnat" ensemble) — évite l'explosion du nombre de
fichiers pour un projet qui a beaucoup d'endpoints.

**Si deux endpoints renvoient la même forme** (ex: liste des events vs
détail d'un event), ne pas dupliquer le DTO — un seul suffit, utilisé dans
les deux cas.

## 2. Entity — l'objet que l'app utilise vraiment

Même liste de champs que le DTO en général, mais c'est un objet **stable,
qui vit longtemps** (pas recréé à chaque appel réseau) :

```dart
class Championship {
  String id = '';
  String code = '';
  String name = '';
  String provider = '';
  String category = '';
  bool isActive = false;
}
```

Règle centrale : **une Entity n'est jamais remplacée, elle est modifiée sur
place**. L'écran qui l'affiche en crée une instance une seule fois
(`Championship()`), la garde en mémoire, et c'est cette même instance qui
est mise à jour à chaque fetch. Pourquoi :
- Évite de recréer des objets à chaque rafraîchissement (moins de pression
  sur le garbage collector, important si un écran se rafraîchit souvent).
- Toute référence gardée ailleurs (par l'UI, par un autre système) reste
  valide après un refresh — pas besoin de la re-brancher.

Pour une **liste** d'entities (ex: liste de pilotes), même principe : la
liste elle-même n'est jamais remplacée. On réconcilie son contenu (voir
"EntityCollectionSync" plus bas) : les entrées déjà connues sont mises à
jour, les nouvelles sont ajoutées, celles qui ont disparu de la réponse
sont retirées.

## 3. Mapper — DTO vers Entity, avec validation minimale

Le mapper ne **crée jamais** d'Entity — il en reçoit une (déjà existante ou
tout juste instanciée par l'appelant) et écrit dedans :

```dart
DataError? applyChampionshipDto(ChampionshipDTO dto, Championship target) {
  if (dto.championshipCode == null || dto.championshipCode!.isEmpty) {
    return DataError(DataErrorKind.validation, 'ChampionshipMapper',
        'championship missing championshipCode');
  }
  if (target.code.isNotEmpty && target.code != dto.championshipCode) {
    return DataError(DataErrorKind.validation, 'ChampionshipMapper',
        "championship code mismatch: target is '${target.code}', dto is '${dto.championshipCode}'");
  }

  target.id = dto.id ?? '';
  target.code = dto.championshipCode!;
  target.name = dto.name ?? '';
  target.provider = dto.provider ?? '';
  target.category = dto.category ?? '';
  target.isActive = dto.isActive;
  return null; // succès
}
```

**Ce qui est validé** : uniquement le champ *identifiant* (celui qui sert
de clé ailleurs dans l'app — ici `championshipCode`). S'il manque, ou s'il
change alors que l'Entity avait déjà une identité, on retourne une erreur
et on n'écrit rien. **Les champs cosmétiques ne sont jamais validés** —
s'ils manquent, ils restent juste vides à l'affichage, ce n'est pas
bloquant.

**Si deux endpoints alimentent la même Entity avec des sous-ensembles de
champs différents** (ex: liste de pilotes vs fiche pilote détaillée), on
écrit deux fonctions de mapping distinctes qui écrivent chacune dans les
mêmes champs de la même Entity — sans jamais écraser les champs que
l'autre a déjà renseignés. C'est ce qui permet à une Entity de
s'enrichir progressivement au fil de plusieurs appels.

### Réconciliation de listes (`EntityCollectionSync`)

Pour une liste d'Entities alimentée par une liste de DTO, la logique
générique (à écrire une seule fois, réutilisable pour toutes les listes) :

```dart
void syncList<TDto, TEntity, TKey>(
  List<TEntity> target,
  List<TDto> dtos,
  TKey Function(TDto) dtoKey,
  TKey Function(TEntity) entityKey,
  TEntity Function() createEntity,
  DataError? Function(TDto, TEntity) applyDto,
) {
  final seenKeys = <TKey>{};

  for (final dto in dtos) {
    final key = dtoKey(dto);
    var entity = target.firstWhereOrNull((e) => entityKey(e) == key);
    final isNew = entity == null;
    entity ??= createEntity();

    final error = applyDto(dto, entity);
    if (error != null) {
      logError(error); // on ignore cette entrée, le reste de la liste continue
      continue;
    }

    seenKeys.add(key);
    if (isNew) target.add(entity);
  }

  target.removeWhere((e) => !seenKeys.contains(entityKey(e)));
}
```

Une entrée invalide est ignorée (loggée), elle ne fait pas échouer tout le
reste de la liste.

## 4. Gestion des erreurs — un seul type, centralisé

Trois catégories seulement, qui couvrent tout :

```dart
enum DataErrorKind { network, deserialize, validation }

class DataError {
  final DataErrorKind kind;
  final String source;   // nom du mapper/composant qui a produit l'erreur
  final String message;
  DataError(this.kind, this.source, this.message);

  @override
  String toString() => '[$source] $message';

  // Un seul endroit qui décide de la gravité — pas dupliqué à chaque appel.
  void report() {
    if (kind == DataErrorKind.validation) {
      logWarning(toString()); // une entrée ignorée, le reste va bien
    } else {
      logError(toString());  // tout l'appel a échoué
    }
  }
}
```

- `network` : requête injoignable, timeout, code HTTP d'erreur.
- `deserialize` : réponse reçue mais JSON invalide/inattendu.
- `validation` : réponse valide, mais un mapper refuse une entrée (champ
  identifiant manquant/incohérent).

**Règle** : partout dans le code, on appelle `error.report()` — jamais un
`log(...)` à la main. Si la politique de gravité change un jour, un seul
fichier à modifier.

## 5. Network — le client HTTP générique + les façades par domaine

Deux niveaux :

**Client générique**, ne connaît aucun endpoint précis, juste "comment
parler HTTP" :

```dart
Future<void> apiGet<T>(
  String path,
  void Function(T) onSuccess,
  void Function(DataError) onError,
) async {
  try {
    final response = await httpClient.get(Uri.parse('$baseUrl$path'),
        headers: {'Authorization': 'Bearer $authToken'});

    if (response.statusCode < 200 || response.statusCode >= 300) {
      onError(DataError(DataErrorKind.network, 'ApiClient',
          'GET $path failed: HTTP ${response.statusCode}'));
      return;
    }

    T data;
    try {
      data = deserialize<T>(response.body); // ex: via json_serializable / json.decode + fromJson
    } catch (e) {
      onError(DataError(DataErrorKind.deserialize, 'ApiClient',
          'GET $path returned unparseable JSON: $e'));
      return;
    }

    onSuccess(data);
  } catch (e) {
    onError(DataError(DataErrorKind.network, 'ApiClient', 'GET $path failed: $e'));
  }
}
```

**Façade par domaine**, une méthode par endpoint, qui fetch le DTO puis
applique le mapping sur l'Entity fournie par l'appelant :

```dart
Future<void> getChampionships(
  List<Championship> target,
  void Function() onSuccess,
  void Function(DataError) onError,
) => apiGet<List<ChampionshipDTO>>('/v1/championship/championships', (dtos) {
      syncList(target, dtos,
          (dto) => dto.championshipCode, (e) => e.code,
          () => Championship(), applyChampionshipDto);
      onSuccess();
    }, onError);
```

L'appelant (l'écran/le controller) fournit toujours la Liste/Entity qu'il
possède déjà — la façade ne retourne jamais un nouvel objet, elle modifie
celui qu'on lui passe (voir section 2).

## 6. Comment un écran doit consommer tout ça

```dart
class StandingsScreen {
  final List<StandingEntry> _standings = [];

  Future<void> refresh(String sessionId) async {
    await getStandings(sessionId, _standings, _repaint, (e) => e.report());
  }

  void _repaint() {
    // Relit _standings (déjà à jour) et redessine les widgets - jamais
    // de nouvelle liste assignée, toujours la même référence relue.
  }
}
```

- L'Entity/la liste est créée **une fois** par l'écran.
- Chaque fetch réussi **modifie** cette même instance.
- Le redessin (`_repaint`) est un appel explicite après chaque fetch réussi
  — rien ne se redessine tout seul juste parce que des champs ont changé.
- Pour une donnée qui doit rester à jour pendant qu'on regarde l'écran
  (ex: classement live), on répète l'appel sur une boucle/timer — jamais
  un flux qui pousse tout seul (sauf cas de flux serveur→client explicite
  type SSE/WebSocket, hors sujet de ce document).

## Résumé des règles à respecter

1. Le DTO mirror exactement le JSON — pas de logique dedans.
2. L'Entity est créée une seule fois par écran, jamais recréée après.
3. Le Mapper modifie une Entity existante, ne valide que le champ
   identifiant, jamais les champs cosmétiques.
4. Une liste d'Entities est réconciliée par clé (ajout/mise à
   jour/suppression), jamais remplacée en bloc.
5. Toute erreur passe par un type unique (`DataError`) avec 3 catégories
   (`network`/`deserialize`/`validation`), et un seul point de décision
   pour la gravité (`report()`).
6. Le réseau a deux niveaux : un client générique qui ne connaît aucun
   endpoint, et des façades par domaine qui connaissent les chemins et
   appellent les mappers.
