// Envia a temperatura da CPU para o display do watercooler Pichau Aqua 240X
// (chip WCH CH340, VID 0x1a86 / PID 0x484a), via porta serial virtual (COM3).
//
// Baseado na engenharia reversa do protocolo feita capturando o tráfego USB
// do software oficial com Wireshark + USBPcap.
//
// Estrutura do pacote identificada (payload, 23 bytes):
//     74 00 TT 08 26 XX YY ZZ 02 03 2e 02 02 02 02 02 02 02 02 02 02 30 1d
//              ^^
//              byte que carrega a temperatura da CPU em °C (confirmado)
//
// IMPORTANTE: os bytes fixos (08 26, XX YY ZZ, 02 03 2e, os 02 repetidos,
// 30 1d) foram copiados de capturas reais e podem codificar outros dados
// (RPM da bomba, checksum) que ainda não identificamos com certeza.
// Se o display se comportar de forma estranha, é sinal de que algum desses
// precisa ser calculado dinamicamente em vez de fixo.
//
// Requer rodar como Administrador (já configurado via app.manifest) e que
// o software oficial do watercooler esteja FECHADO (ele usa a COM3 sozinho).

using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using LibreHardwareMonitor.Hardware;

namespace WatercoolerTemp;

internal class Program
{
    // ==== CONFIGURAÇÃO ====
    private const string PortaCom = "COM3";      // ajuste se a porta mudar
    private const int BaudRate = 9600;            // valor comum para CH340
    private const int IntervaloMs = 1000;         // de quanto em quanto tempo atualizar

    private static void Main()
    {
        Console.WriteLine("=== Controle de Display - Pichau Aqua 240X ===\n");

        var computer = new Computer
        {
            IsCpuEnabled = true
        };
        computer.Open();

        SerialPort? porta = null;
        try
        {
            porta = new SerialPort(PortaCom, BaudRate)
            {
                WriteTimeout = 1000
            };
            porta.Open();
            Console.WriteLine($"Porta {PortaCom} aberta com sucesso.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERRO ao abrir a porta {PortaCom}: {ex.Message}");
            Console.WriteLine("Verifique se:");
            Console.WriteLine("  1. O software oficial do watercooler está FECHADO");
            Console.WriteLine("  2. A porta COM3 está correta (confira no Gerenciador de Dispositivos)");
            Console.WriteLine("\nPressione qualquer tecla para sair...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Enviando temperatura a cada 1 segundo. Pressione Ctrl+C para parar.\n");

        try
        {
            while (true)
            {
                int? temp = LerTemperaturaCpu(computer);

                if (temp is null)
                {
                    Console.WriteLine("Não consegui ler a temperatura da CPU. Tentando de novo...");
                    Thread.Sleep(IntervaloMs);
                    continue;
                }

                byte[] pacote = MontarPacote(temp.Value);
                porta.Write(pacote, 0, pacote.Length);

                Console.WriteLine($"Enviado: {temp}°C -> {BitConverter.ToString(pacote).Replace("-", " ")}");

                Thread.Sleep(IntervaloMs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nErro durante o envio: {ex.Message}");
        }
        finally
        {
            porta.Close();
            computer.Close();
        }
    }

    /// <summary>
    /// Lê a temperatura real da CPU usando LibreHardwareMonitorLib
    /// (biblioteca nativa .NET, referenciada direto via NuGet).
    /// </summary>
    private static int? LerTemperaturaCpu(Computer computer)
    {
        foreach (IHardware hardware in computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu) continue;

            hardware.Update();

            // Tenta achar o sensor "Package" primeiro (temperatura geral da CPU)
            ISensor? sensorPackage = hardware.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature && s.Name.Contains("Package"));

            if (sensorPackage?.Value is not null)
                return (int)Math.Round(sensorPackage.Value.Value);

            // Fallback: qualquer sensor de temperatura disponível
            ISensor? qualquerSensor = hardware.Sensors.FirstOrDefault(s =>
                s.SensorType == SensorType.Temperature && s.Value is not null);

            if (qualquerSensor?.Value is not null)
                return (int)Math.Round(qualquerSensor.Value.Value);
        }

        return null;
    }

    /// <summary>
    /// Monta o pacote de 23 bytes no formato identificado na engenharia reversa.
    /// </summary>
    private static byte[] MontarPacote(int temperaturaCelsius)
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
