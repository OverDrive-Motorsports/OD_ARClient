/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipEventMapperTests - Covers ChampionshipEventMapper.TryApply.
 ##
 */

using NUnit.Framework;

public class ChampionshipEventMapperTests
{
    [Test]
    public void TryApply_CopiesFields_OnValidDto()
    {
        var dto = new EventDTO
        {
            eventId = "openf1:event:1255",
            name = "Bahrain Grand Prix",
            circuit = "Bahrain International Circuit",
            seasonYear = 2026,
        };
        var target = new ChampionshipEvent();

        DataError? error = dto.TryApply(target);

        Assert.IsNull(error);
        Assert.AreEqual("Bahrain Grand Prix", target.name);
        Assert.AreEqual(2026, target.seasonYear);
    }

    [Test]
    public void TryApply_ReturnsValidationError_WhenEventIdMissing()
    {
        var dto = new EventDTO { eventId = null };
        var target = new ChampionshipEvent();

        DataError? error = dto.TryApply(target);

        Assert.IsNotNull(error);
        Assert.AreEqual(DataErrorKind.Validation, error.Value.kind);
    }
}
