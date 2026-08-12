using Azulyoro.Infrastructure.Email;
using Xunit;

namespace Azulyoro.UnitTests.Auth;

public class TokenHasherTests
{
    [Fact]
    public void NewToken_is_random_and_urlsafe()
    {
        var a = TokenHasher.NewToken();
        var b = TokenHasher.NewToken();

        Assert.NotEqual(a, b);
        Assert.DoesNotContain('+', a);
        Assert.DoesNotContain('/', a);
        Assert.DoesNotContain('=', a);
    }

    [Fact]
    public void Verify_matches_hash_of_same_token()
    {
        var token = TokenHasher.NewToken();
        var hash = TokenHasher.Hash(token);

        Assert.True(TokenHasher.Verify(token, hash));
        Assert.False(TokenHasher.Verify("other-token", hash));
        Assert.False(TokenHasher.Verify(token, null));
    }
}
