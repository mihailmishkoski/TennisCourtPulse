using CourtPulse.Application.ExternalApi;
using CourtPulse.Application.Mapping;
using CourtPulse.Domain.Enums;
using Xunit;

namespace CourtPulse.Application.Tests;

public sealed class LiveMatchMapperTests
{
    private readonly LiveMatchMapper _mapper = new LiveMatchMapper();

    [Fact]
    public void ParsesLiveGameScoreAndServerFromTheEventFields()
    {
        LiveMatchApiResponse source = new LiveMatchApiResponse
        {
            EventKey = 1,
            FirstPlayer = "Alpha",
            SecondPlayer = "Beta",
            Serve = "First Player",
            GameResult = "40 - 30",
            Status = "Set 1"
        };

        MappedMatch mapped = _mapper.Map(source);

        Assert.Equal(PlayerSide.First, mapped.Serving);
        Assert.Equal(3, mapped.CurrentGameFirstPoints);  // 40 → index 3
        Assert.Equal(2, mapped.CurrentGameSecondPoints); // 30 → index 2
    }

    [Fact]
    public void AdvantageMapsToIndexFour_AndUnknownScoreFallsBackToZero()
    {
        MappedMatch withAdvantage = _mapper.Map(new LiveMatchApiResponse
        {
            EventKey = 2, FirstPlayer = "Alpha", SecondPlayer = "Beta", GameResult = "A - 40"
        });
        MappedMatch withoutScore = _mapper.Map(new LiveMatchApiResponse
        {
            EventKey = 3, FirstPlayer = "Alpha", SecondPlayer = "Beta", GameResult = null
        });

        Assert.Equal(4, withAdvantage.CurrentGameFirstPoints);
        Assert.Equal(3, withAdvantage.CurrentGameSecondPoints);
        Assert.Equal(0, withoutScore.CurrentGameFirstPoints);
        Assert.Equal(0, withoutScore.CurrentGameSecondPoints);
    }

    [Fact]
    public void DetectsDoublesFromTheSlashInPlayerNames()
    {
        MappedMatch mapped = _mapper.Map(new LiveMatchApiResponse
        {
            EventKey = 4, FirstPlayer = "Andrade da Silva/ Meligeni Alves", SecondPlayer = "Oliveira/ Pucinelli de Almeida"
        });

        Assert.True(mapped.IsDoubles);
    }
}
