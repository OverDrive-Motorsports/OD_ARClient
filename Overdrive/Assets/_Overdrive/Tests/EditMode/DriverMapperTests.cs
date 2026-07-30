/**
 ##
 ## OverDrive 2026
 ## All Technical rights reserved
 ##
 ## DriverMapperTests - Covers DriverMapper's two overloads applying to the
 ## same Driver instance (enrichment from two different endpoints).
 ##
 */

using NUnit.Framework;

public class DriverMapperTests
{
    [Test]
    public void TryApply_EnrichesDriver_FromBothSessionAndProfileDtos()
    {
        var target = new Driver();

        var sessionDto = new SessionDriverDTO { driverNumber = 63, fullName = "George RUSSELL", teamName = "Mercedes", teamColor = "#27F4D2" };
        DataError? error1 = sessionDto.TryApply(target);

        var profileDto = new DriverProfileDTO { driverNumber = 63, nationality = "GBR", championshipCode = "f1" };
        DataError? error2 = profileDto.TryApply(target);

        Assert.IsNull(error1);
        Assert.IsNull(error2);
        Assert.AreEqual(63, target.number);
        Assert.AreEqual("Mercedes", target.teamName); // from the session dto, untouched by the profile dto
        Assert.AreEqual("GBR", target.nationality);    // from the profile dto
    }

    [Test]
    public void TryApply_ReturnsValidationError_WhenDriverNumberMissing()
    {
        var dto = new SessionDriverDTO { driverNumber = 0 };
        var target = new Driver();

        DataError? error = dto.TryApply(target);

        Assert.IsNotNull(error);
        Assert.AreEqual(DataErrorKind.Validation, error.Value.kind);
    }
}
