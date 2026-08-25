namespace WatercoolerTemp.Core;

public static class Aqua240XProtocol
{
    /// <summary>
    /// Monta o pacote de 23 bytes no formato identificado na engenharia reversa.
    /// </summary>
    public static byte[] MontarPacote(int temperaturaCelsius)
    {
        if (temperaturaCelsius is < 0 or > 255)
            throw new ArgumentOutOfRangeException(nameof(temperaturaCelsius));

        return new byte[]
        {
            0x74, 0x00,                    // prefixo fixo observado
            (byte)temperaturaCelsius,      // <-- byte da temperatura
            0x08, 0x26,                    // bytes fixos (função ainda não confirmada)
            0x2f, 0x0f, 0x9b,              // possivelmente RPM da bomba (não confirmado)
            0x02, 0x03, 0x2e,              // fixos
            0x02, 0x02, 0x02, 0x02, 0x02,
            0x02, 0x02, 0x02, 0x02, 0x02,  // padding observado
            0x30, 0x1d,                    // sufixo fixo observado
        };
    }
}