using Rai.Shared.Hmac;

namespace Rai.Tests;

public sealed class HmacSignerTests
{
    private const string Secret = "test-secret";
    private const string Payload = """{"name":"Test Contact"}""";

    [Fact]
    public void Sign_then_Verify_returns_true()
    {
        var signature = HmacSigner.Sign(Payload, Secret);
        Assert.True(HmacSigner.Verify(Payload, Secret, signature));
    }

    [Fact]
    public void Verify_fails_with_tampered_payload()
    {
        var signature = HmacSigner.Sign(Payload, Secret);
        Assert.False(HmacSigner.Verify(Payload + " tampered", Secret, signature));
    }

    [Fact]
    public void Verify_fails_with_wrong_secret()
    {
        var signature = HmacSigner.Sign(Payload, Secret);
        Assert.False(HmacSigner.Verify(Payload, "wrong-secret", signature));
    }
}
