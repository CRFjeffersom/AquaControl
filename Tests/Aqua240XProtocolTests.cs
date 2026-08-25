using WatercoolerTemp.Core;
using Xunit;

namespace AquaControl.Tests;

public sealed class Aqua240XProtocolTests
{
    [Fact]
    public void MontarPacoteGeraPayloadDe23Bytes()
    {
        byte[] pacote = Aqua240XProtocol.MontarPacote(55);

        Assert.Equal(23, pacote.Length);
        Assert.Equal(0x74, pacote[0]);
        Assert.Equal(0x00, pacote[1]);
        Assert.Equal(0x1D, pacote[22]);
    }

    [Fact]
    public void MontarPacoteInsereTemperaturaNoTerceiroByte()
    {
        byte[] pacote = Aqua240XProtocol.MontarPacote(87);

        Assert.Equal(87, pacote[2]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public void MontarPacoteRejeitaTemperaturaForaDoByte(int temperatura)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Aqua240XProtocol.MontarPacote(temperatura));
    }
}
