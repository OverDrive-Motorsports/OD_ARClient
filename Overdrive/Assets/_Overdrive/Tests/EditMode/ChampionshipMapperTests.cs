/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## ChampionshipMapperTests - Covers ChampionshipMapper.TryApply: success,
 ## missing key, and key-changed-on-existing-target cases.
 ##
 */

using NUnit.Framework;

public class ChampionshipMapperTests
{
    [Test]
    public void TryApply_CopiesFields_OnValidDto()
    {
        var dto = new ChampionshipDTO
        {
            id = "openf1:championship:f1",
            championshipCode = "f1",
            name = "Formula 1",
            provider = "openf1",
            category = "single-seater",
            isActive = true,
        };
        var target = new Championship();

        DataError? error = dto.TryApply(target);

        Assert.IsNull(error);
        Assert.AreEqual("f1", target.code);
        Assert.AreEqual("Formula 1", target.name);
        Assert.IsTrue(target.isActive);
    }

    [Test]
    public void TryApply_ReturnsValidationError_WhenChampionshipCodeMissing()
    {
        var dto = new ChampionshipDTO { championshipCode = "" };
        var target = new Championship();

        DataError? error = dto.TryApply(target);

        Assert.IsNotNull(error);
        Assert.AreEqual(DataErrorKind.Validation, error.Value.kind);
    }

    [Test]
    public void TryApply_ReturnsError_AndLeavesTargetUntouched_WhenCodeChanges()
    {
        var target = new Championship { code = "f1" };
        var dto = new ChampionshipDTO { championshipCode = "wec" };

        DataError? error = dto.TryApply(target);

        Assert.IsNotNull(error);
        Assert.AreEqual("f1", target.code);
    }
}
