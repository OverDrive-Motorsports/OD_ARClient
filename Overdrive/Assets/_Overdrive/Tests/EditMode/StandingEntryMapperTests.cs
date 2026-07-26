/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## StandingEntryMapperTests - Covers StandingEntryMapper.TryApply.
 ##
 */

using NUnit.Framework;

public class StandingEntryMapperTests
{
    [Test]
    public void TryApply_CopiesFields_OnValidDto()
    {
        var dto = new StandingEntryDTO { position = 1, driverNumber = 63, teamId = "openf1:f1:team:mercedes", gapToLeader = "0.000", points = 25 };
        var target = new StandingEntry();

        DataError? error = dto.TryApply(target);

        Assert.IsNull(error);
        Assert.AreEqual(1, target.position);
        Assert.AreEqual(25, target.points);
    }

    [Test]
    public void TryApply_ReturnsValidationError_WhenDriverNumberMissing()
    {
        var dto = new StandingEntryDTO { driverNumber = 0 };
        var target = new StandingEntry();

        DataError? error = dto.TryApply(target);

        Assert.IsNotNull(error);
        Assert.AreEqual(DataErrorKind.Validation, error.Value.kind);
    }
}
