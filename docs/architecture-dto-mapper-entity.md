# DTO / Mapper / Entity / Network architecture — porting guide

This document describes the pattern the AR client (Unity) uses to talk to the
backend, so that the mobile front-end team (Flutter) can reproduce the same
logic. The sample code is Dart-like pseudocode, not C# — what to copy is the
structure and the rules, not the syntax.

## Overview

```
Network (HTTP)  →  DTO  →  Mapper  →  Entity  →  UI
```

Four clearly separated roles, each with a single responsibility:

| Layer | Role | Never does |
|---|---|---|
| **DTO** | Exact mirror of the received JSON | Logic, validation |
| **Mapper** | DTO → Entity, validates, handles errors | Create a new Entity on every call |
| **Entity** | Object the app uses, stable over time | Know the shape of the original JSON |
| **Network** | Calls the API, deserializes, calls the Mapper | Decide what to do with the result (that's the screen's/controller's job) |

## 1. DTO — a mirror of the JSON, nothing else

One class per JSON response shape, with the same field names as the backend
(camelCase). No methods, no logic.

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

**File layout**: group related DTOs in one file per domain rather than one
file per endpoint (e.g. all the "championship catalogue" DTOs together) —
this keeps the file count under control in a project with many endpoints.

**If two endpoints return the same shape** (e.g. event list vs event
detail), don't duplicate the DTO — one is enough, used in both cases.

## 2. Entity — the object the app actually uses

Usually the same fields as the DTO, but it is a **stable, long-lived**
object (not recreated on every network call):

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

Core rule: **an Entity is never replaced, it is updated in place**. The
screen that shows it creates one instance once (`Championship()`), keeps it,
and that same instance is updated on every fetch. Why:
- No objects recreated on every refresh (less garbage-collector pressure,
  which matters when a screen refreshes often).
- Any reference held elsewhere (by the UI, by another system) stays valid
  after a refresh — no need to wire it again.

For a **list** of entities (e.g. a driver list), same principle: the list
itself is never replaced. Its content is reconciled (see
"EntityCollectionSync" below): known entries are updated, new ones are
added, and the ones missing from the response are removed.

## 3. Mapper — DTO to Entity, with minimal validation

The mapper **never creates** an Entity — it receives one (existing, or just
instantiated by the caller) and writes into it:

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
  return null; // success
}
```

**What is validated**: only the *identifier* field (the one used as a key
elsewhere in the app — here `championshipCode`). If it is missing, or if it
changes while the Entity already had an identity, an error is returned and
nothing is written. **Cosmetic fields are never validated** — if they are
missing, they simply show empty; it isn't blocking.

**If two endpoints feed the same Entity with different subsets of fields**
(e.g. driver list vs detailed driver sheet), write two separate mapping
functions that each write into the same Entity — never overwriting the
fields the other one already filled. This is what lets an Entity get richer
over several calls.

### List reconciliation (`EntityCollectionSync`)

For a list of Entities fed by a list of DTOs, the generic logic (written
once, reused for every list):

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
      logError(error); // skip this entry, the rest of the list goes on
      continue;
    }

    seenKeys.add(key);
    if (isNew) target.add(entity);
  }

  target.removeWhere((e) => !seenKeys.contains(entityKey(e)));
}
```

An invalid entry is skipped (and logged); it doesn't fail the rest of the
list.

## 4. Error handling — one type, centralised

Only three categories, which cover everything:

```dart
enum DataErrorKind { network, deserialize, validation }

class DataError {
  final DataErrorKind kind;
  final String source;   // name of the mapper/component that raised the error
  final String message;
  DataError(this.kind, this.source, this.message);

  @override
  String toString() => '[$source] $message';

  // One place decides the severity — not duplicated at every call site.
  void report() {
    if (kind == DataErrorKind.validation) {
      logWarning(toString()); // one entry skipped, the rest is fine
    } else {
      logError(toString());  // the whole call failed
    }
  }
}
```

- `network`: unreachable request, timeout, HTTP error code.
- `deserialize`: response received but the JSON is invalid/unexpected.
- `validation`: valid response, but a mapper rejects an entry (identifier
  field missing/inconsistent).

**Rule**: everywhere in the code, call `error.report()` — never a manual
`log(...)`. If the severity policy changes one day, only one file changes.

## 5. Network — the generic HTTP client + per-domain facades

Two levels:

**Generic client**: knows no specific endpoint, only "how to speak HTTP":

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
      data = deserialize<T>(response.body); // e.g. via json_serializable / json.decode + fromJson
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

**Per-domain facade**: one method per endpoint, which fetches the DTO and
then applies the mapping to the Entity provided by the caller:

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

The caller (screen/controller) always provides the List/Entity it already
owns — the facade never returns a new object, it updates the one it is given
(see section 2).

## 6. How a screen consumes all this

```dart
class StandingsScreen {
  final List<StandingEntry> _standings = [];

  Future<void> refresh(String sessionId) async {
    await getStandings(sessionId, _standings, _repaint, (e) => e.report());
  }

  void _repaint() {
    // Reads _standings again (already up to date) and redraws the widgets —
    // never a new list assigned, always the same reference read again.
  }
}
```

- The Entity/list is created **once** by the screen.
- Every successful fetch **updates** that same instance.
- The redraw (`_repaint`) is an explicit call after each successful fetch —
  nothing redraws by itself just because fields changed.
- For data that must stay current while the screen is open (e.g. live
  standings), repeat the call on a loop/timer — never a stream that pushes
  by itself (except an explicit server→client stream such as SSE/WebSocket,
  out of scope for this document).

## Summary of the rules

1. The DTO mirrors the JSON exactly — no logic in it.
2. The Entity is created once per screen, never recreated afterwards.
3. The Mapper updates an existing Entity and validates only the identifier
   field, never the cosmetic fields.
4. A list of Entities is reconciled by key (add/update/remove), never
   replaced wholesale.
5. Every error goes through a single type (`DataError`) with 3 categories
   (`network`/`deserialize`/`validation`), and one decision point for the
   severity (`report()`).
6. Networking has two levels: a generic client that knows no endpoint, and
   per-domain facades that know the paths and call the mappers.
