# Exemple : brancher une Entity sur un prefab existant

Cas concret du principe décrit dans
[UI-Refresh-Pattern.md](./UI-Refresh-Pattern.md), appliqué à un prefab qui
existe déjà dans le projet : `ODDataTable`
(`UI/Prefabs/Organisms/ODDataTable.prefab`, script
`UI/Organisms/ODDataTable.cs`). On l'utilise pour afficher une liste de
`StandingEntry` (classement live).

## C'est bien "via l'interface" — pas un accès direct

`ODDataTable` fait partie de `UI/`, qui n'a **aucune dépendance vers
Data/Core/Logic** (voir [UI/Doc/README.md](../../UI/Doc/README.md)) — il ne
connaît même pas l'existence de la classe `StandingEntry`. Il n'expose que
des méthodes publiques qui prennent des `string`/`List<string>` :

```csharp
public void SetColumns(List<ODTableColumn> columns)                        // définit les colonnes une fois
public void SetData(List<List<string>> data, List<string> rowIds = null)   // vide + reconstruit toutes les lignes
public void UpdateCell(string rowId, string columnId, string newValue)     // patch une seule cellule sans tout reconstruire
```

C'est donc le controller Logic qui fait la traduction : il lit les champs de
l'Entity et pousse des chaînes de caractères dans la table. `ODDataTable` ne
reçoit jamais un objet `StandingEntry` directement.

## Le controller

`ChampionshipApi.GetStandings` existe réellement dans `Core/Network` (voir
[Core/Doc/README.md](../../Core/Doc/README.md)).

```csharp
public class StandingsTableController : MonoBehaviour
{
    public string sessionId;
    public ODDataTable table;               // référence au prefab dans la scène
    public float refreshSeconds = 5f;

    private readonly List<StandingEntry> _standings = new List<StandingEntry>();

    private void Start()
    {
        table.SetColumns(new List<ODTableColumn>
        {
            new ODTableColumn { columnId = "pos",  header = "POS",  flexWidth = 0.5f, bold = true },
            new ODTableColumn { columnId = "num",  header = "#",    flexWidth = 0.5f },
            new ODTableColumn { columnId = "team",  header = "EQUIPE", flexWidth = 1.5f },
            new ODTableColumn { columnId = "gap",  header = "GAP",  flexWidth = 1f },
        });

        StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return ChampionshipApi.GetStandings(sessionId, _standings,
                () => Repaint(_standings),
                error => error.Report());

            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    private void Repaint(List<StandingEntry> standings)
    {
        var rows = new List<List<string>>();
        foreach (var s in standings)
        {
            rows.Add(new List<string>
            {
                s.position.ToString(),
                s.driverNumber.ToString(),
                s.teamId,
                s.gapToLeader,
            });
        }
        table.SetData(rows);
    }
}
```

## Ce qui se passe à chaque refresh

1. `ChampionshipApi.GetStandings` met à jour `_standings` **en place**
   (`EntityCollectionSync` — voir [UI-Refresh-Pattern.md](./UI-Refresh-Pattern.md)) :
   toujours la même `List<StandingEntry>`, jamais recréée.
2. `Repaint` relit l'état courant de `_standings` et construit une liste de
   `string` à partir des champs des entities.
3. `table.SetData(rows)` vide puis reconstruit les lignes visuelles de
   `ODDataTable`. C'est le seul moment où l'UI change réellement.

`SetColumns` n'est appelé qu'une fois (`Start`) — les colonnes ne changent
jamais, seules les données changent.

## Si les rafraîchissements deviennent très fréquents

`SetData` détruit et recrée toutes les lignes à chaque appel — très bien
pour un refresh toutes les 5s, mais couteux si on l'appelait à haute
fréquence (télémétrie). Dans ce cas, `ODDataTable.UpdateCell(rowId, columnId,
newValue)` permet de ne patcher qu'une cellule sans tout reconstruire — même
logique de coût que celle évoquée dans
[Core/Doc/README.md](../../Core/Doc/README.md) pour REST vs télémétrie :
reconstruire souvent = cher, patcher juste ce qui change = pas cher.
