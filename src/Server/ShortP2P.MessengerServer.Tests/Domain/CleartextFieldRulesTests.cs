using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Tests.Domain;

public class CleartextFieldRulesTests
{
    [Theory]
    [InlineData("Alice", true)]
    [InlineData("bot name", true)]
    [InlineData("<script>", false)]
    [InlineData("line\nbreak", false)]
    [InlineData("nul\0char", false)]
    [InlineData("", false)]
    public void IsValid_RejectsControlsAndAngleBrackets(string value, bool expected) =>
        Assert.Equal(expected, CleartextFieldRules.IsValid(value, 256));

    [Fact]
    public void Sanitize_StripsDisallowedCharacters()
    {
        Assert.Equal("safe", CleartextFieldRules.Sanitize("<safe>", 256));
        Assert.Equal("ab", CleartextFieldRules.Sanitize("a\nb", 256));
    }
}
