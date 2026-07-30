# Comment l'UI doit consommer les Entities

## Le principe

Une Entity (`Championship`, `StandingEntry`...) n'est **jamais recréée** à
chaque appel réseau. Une seule instance vit pendant toute la durée où l'écran
en a besoin, et chaque fetch la **modifie sur place** (`TryApply`) au lieu
d'en fabriquer une nouvelle. Pour une liste (`List<Driver>`,
`List<StandingEntry>`...), c'est pareil : la `List<T>` elle-même n'est jamais
remplacée, seul son contenu change (`EntityCollectionSync.Sync`).

Ça veut dire concrètement :
- **Qui possède la référence à l'entity/la liste ?** Le controller Logic qui
  affiche l'écran (ex: `ChampionshipPageController`) — jamais Core, jamais
  un DTO. Il la crée une fois (`new Championship()` / `new List<Driver>()`),
  la garde en champ, et la repasse à chaque appel réseau.
- **Le repaint reste un déclenchement explicite.** Muter l'entity en place
  ne redessine rien tout seul — Unity UI (TextMeshPro, Image...) ne se
  "bind" pas automatiquement sur un champ C#. Il faut toujours rappeler une
  fonction de repaint après le fetch, qui **relit l'état courant** de
  l'entity/la liste et met à jour les éléments visuels.

`GetEventDetail`/`GetStandings` ci-dessous existent réellement dans
`Core/Network/ChampionshipApi.cs` (voir
[Core/Doc/README.md](../../Core/Doc/README.md)) : fetch les DTO, les passe
au mapper, `onSuccess`/`onError` en callback.

## Cas 1 — une entity seule (donnée statique)

```csharp
public class EventDetailController : MonoBehaviour
{
    private readonly ChampionshipEvent _event = new ChampionshipEvent();

    private IEnumerator Refresh(string eventId)
    {
        yield return ChampionshipApi.GetEventDetail(eventId, _event,
            () => Repaint(_event),   // succès : redessine avec l'état courant
            error => error.Report()); // échec : l'ancien affichage reste tel quel (voir Data/Doc/README.md)
    }

    private void Repaint(ChampionshipEvent e)
    {
        titleLabel.text = e.name;
        locationLabel.text = e.location;
    }
}
```

`_championship` ne change jamais d'identité — seuls ses champs bougent.
`Repaint` peut être rappelée après n'importe quel fetch, elle lit toujours
la version la plus fraîche.

## Cas 2 — une liste (donnée live, ex: standings)

```csharp
public class StandingsController : MonoBehaviour
{
    public string sessionId;
    private readonly List<StandingEntry> _standings = new List<StandingEntry>();

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return ChampionshipApi.GetStandings(sessionId, _standings,
                () => Repaint(_standings),
                error => error.Report());

            yield return new WaitForSeconds(5f);
        }
    }

    private void Repaint(List<StandingEntry> standings)
    {
        ClearRows();
        foreach (var s in standings)   // triée par EntityCollectionSync dans l'ordre reçu du backend
            CreateRow(s.position, s.driverNumber, s.gapToLeader);
    }
}
```

`_standings` garde la même référence sur toute la durée de vie de l'écran.
`EntityCollectionSync.Sync` (appelé par `ChampionshipApi.GetStandings`) met
à jour/ajoute/retire les entrées dedans ; `Repaint` reconstruit juste les
lignes visuelles à partir de son état courant — même mécanique que
`ChampionshipPageController.PopulateStandings` déjà dans le projet (clear +
rebuild), sauf qu'ici la source de données ne change jamais de référence.

## Règle à retenir

| Situation | Qui recrée quoi |
|---|---|
| Nouvel écran ouvert | Le controller crée l'entity/liste **une fois** (`new`) |
| Données reçues du backend | Le mapper **modifie** l'entity/liste existante (`TryApply` / `Sync`) |
| Affichage | Une fonction `Repaint()` explicite, rappelée après chaque fetch réussi, qui relit l'état courant |

Ne jamais faire de `list = newList;` ou `entity = newEntity;` après un
fetch — ça casserait toute référence que l'UI ou un autre système aurait
gardée vers l'ancien objet, et ça annule l'intérêt de la mise à jour en
place (allocations évitées, référence stable).
