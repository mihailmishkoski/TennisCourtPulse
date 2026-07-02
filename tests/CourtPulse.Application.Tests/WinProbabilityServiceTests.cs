using CourtPulse.Application.Analytics;
using CourtPulse.Domain.Enums;
using Xunit;

namespace CourtPulse.Application.Tests;

public sealed class WinProbabilityServiceTests
{
    private readonly WinProbabilityService _service = new WinProbabilityService();

    [Fact]
    public void EqualPlayers_AtStartOfMatch_IsRoughlyFiftyFifty()
    {
        WinProbability p = _service.Estimate(new WinProbabilityInput
        {
            Serving = PlayerSide.First,
            FirstServePointWin = 0.62,
            SecondServePointWin = 0.62
        });

        Assert.Equal(1.0, p.First + p.Second, 6);
        Assert.InRange(p.First, 0.48, 0.56); // small edge for the current server only
    }

    [Fact]
    public void LeadingByASetAndABreak_StronglyFavoursThatPlayer()
    {
        WinProbability p = _service.Estimate(new WinProbabilityInput
        {
            FirstSetsWon = 1,
            SecondSetsWon = 0,
            FirstGamesInSet = 3,
            SecondGamesInSet = 1,
            Serving = PlayerSide.First,
            FirstServePointWin = 0.62,
            SecondServePointWin = 0.62
        });

        Assert.True(p.First > 0.80, $"expected strong favourite, got {p.First:0.00}");
    }

    [Fact]
    public void ServingForTheGameAtFortyLove_RaisesProbabilityVersusDeuce()
    {
        WinProbabilityInput baseState = new WinProbabilityInput
        {
            Serving = PlayerSide.First,
            FirstServePointWin = 0.62,
            SecondServePointWin = 0.62
        };

        double atFortyLove = _service.Estimate(baseState with { ServerPointsInGame = 3, ReturnerPointsInGame = 0 }).First;
        double atLoveForty = _service.Estimate(baseState with { ServerPointsInGame = 0, ReturnerPointsInGame = 3 }).First;

        Assert.True(atFortyLove > atLoveForty);
    }

    [Fact]
    public void StrongerServer_IsFavouredOverWeakerServer()
    {
        WinProbability p = _service.Estimate(new WinProbabilityInput
        {
            Serving = PlayerSide.First,
            FirstServePointWin = 0.70,
            SecondServePointWin = 0.55
        });

        Assert.True(p.First > 0.60);
    }
}
