/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## EntityCollectionSyncTests - Covers the add/update/remove/skip-invalid
 ## reconciliation logic shared by every list-based mapper call.
 ##
 */

using System.Collections.Generic;
using NUnit.Framework;

public class EntityCollectionSyncTests
{
    [Test]
    public void Sync_AddsNewEntity_ForAnUnseenKey()
    {
        var target = new List<Championship>();
        var dtos = new List<ChampionshipDTO> { new ChampionshipDTO { championshipCode = "f1", name = "Formula 1" } };

        EntityCollectionSync.Sync(target, dtos,
            dto => dto.championshipCode, entity => entity.code,
            () => new Championship(), (dto, entity) => dto.TryApply(entity));

        Assert.AreEqual(1, target.Count);
        Assert.AreEqual("f1", target[0].code);
    }

    [Test]
    public void Sync_UpdatesExistingEntity_InPlace_WithoutCreatingANewInstance()
    {
        var existing = new Championship { code = "f1", name = "Old Name" };
        var target = new List<Championship> { existing };
        var dtos = new List<ChampionshipDTO> { new ChampionshipDTO { championshipCode = "f1", name = "Formula 1 World Championship" } };

        EntityCollectionSync.Sync(target, dtos,
            dto => dto.championshipCode, entity => entity.code,
            () => new Championship(), (dto, entity) => dto.TryApply(entity));

        Assert.AreEqual(1, target.Count);
        Assert.AreSame(existing, target[0]); // same reference, mutated - not replaced
        Assert.AreEqual("Formula 1 World Championship", target[0].name);
    }

    [Test]
    public void Sync_RemovesEntities_WhoseKeyIsNoLongerInTheFetch()
    {
        var target = new List<Championship> { new Championship { code = "f1" }, new Championship { code = "wec" } };
        var dtos = new List<ChampionshipDTO> { new ChampionshipDTO { championshipCode = "f1" } };

        EntityCollectionSync.Sync(target, dtos,
            dto => dto.championshipCode, entity => entity.code,
            () => new Championship(), (dto, entity) => dto.TryApply(entity));

        Assert.AreEqual(1, target.Count);
        Assert.AreEqual("f1", target[0].code);
    }

    [Test]
    public void Sync_SkipsInvalidDto_WithoutAddingIt()
    {
        var target = new List<Championship>();
        var dtos = new List<ChampionshipDTO> { new ChampionshipDTO { championshipCode = "" } }; // invalid: missing key

        EntityCollectionSync.Sync(target, dtos,
            dto => dto.championshipCode, entity => entity.code,
            () => new Championship(), (dto, entity) => dto.TryApply(entity));

        Assert.AreEqual(0, target.Count);
    }
}
