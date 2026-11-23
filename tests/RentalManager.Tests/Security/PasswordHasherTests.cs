using RentalManager.Infrastructure.Security;

namespace RentalManager.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashProducesAnIterationsSaltAndValue()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.Equal(3, hash.Split('.').Length);
        Assert.StartsWith("100000.", hash);
    }

    [Fact]
    public void TwoHashesOfTheSamePasswordDiffer()
    {
        var first = _hasher.Hash("same-password");
        var second = _hasher.Hash("same-password");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void VerifyAcceptsTheCorrectPassword()
    {
        var hash = _hasher.Hash("correct-password");

        Assert.True(_hasher.Verify("correct-password", hash));
    }

    [Fact]
    public void VerifyRejectsTheWrongPassword()
    {
        var hash = _hasher.Hash("correct-password");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("100000.!!!.abc")]
    public void VerifyRejectsMalformedStoredValues(string value)
    {
        Assert.False(_hasher.Verify("anything", value));
    }
}
