/**
 * @file: SoundService.cs
 * @description: Сервіс для відтворення звукових ефектів у іграх
 * @dependencies: System.Media, System.IO
 * @created: 2024-12-19
 */

using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace BoardGamesLibrary.Desktop.Services;

/// <summary>
/// Сервіс для управління звуковими ефектами
/// </summary>
public class SoundService
{
    private static SoundService? _instance;
    private readonly object _lockObject = new();
    private bool _isEnabled = true;
    private readonly string _settingsFilePath;

    public static SoundService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new SoundService();
            }
            return _instance;
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            SaveSettings();
        }
    }

    private SoundService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BoardGamesLibrary");
        
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _settingsFilePath = Path.Combine(appDataPath, "sound_settings.json");
        LoadSettings();
    }

    /// <summary>
    /// Відтворює звук кліку по клітинці
    /// </summary>
    public void PlayClickSound()
    {
        if (!_isEnabled) return;
        
        Task.Run(() =>
        {
            try
            {
                PlayTone(800, 50); // Високий короткий тон
            }
            catch
            {
                // Ігноруємо помилки відтворення звуку
            }
        });
    }

    /// <summary>
    /// Відтворює звук виконання ходу
    /// </summary>
    public void PlayMoveSound()
    {
        if (!_isEnabled) return;
        
        Task.Run(() =>
        {
            try
            {
                PlayTone(600, 100); // Середній тон
            }
            catch
            {
                // Ігноруємо помилки відтворення звуку
            }
        });
    }

    /// <summary>
    /// Відтворює звук перемоги
    /// </summary>
    public void PlayVictorySound()
    {
        if (!_isEnabled) return;
        
        Task.Run(() =>
        {
            try
            {
                // Мелодія перемоги: три тони що піднімаються
                PlayTone(523, 150); // C5
                Task.Delay(50).Wait();
                PlayTone(659, 150); // E5
                Task.Delay(50).Wait();
                PlayTone(784, 200); // G5
            }
            catch
            {
                // Ігноруємо помилки відтворення звуку
            }
        });
    }

    /// <summary>
    /// Відтворює звук поразки
    /// </summary>
    public void PlayDefeatSound()
    {
        if (!_isEnabled) return;
        
        Task.Run(() =>
        {
            try
            {
                // Мелодія поразки: два тони що опускаються
                PlayTone(392, 200); // G4
                Task.Delay(100).Wait();
                PlayTone(262, 300); // C4
            }
            catch
            {
                // Ігноруємо помилки відтворення звуку
            }
        });
    }

    /// <summary>
    /// Відтворює звук нічиєї
    /// </summary>
    public void PlayDrawSound()
    {
        if (!_isEnabled) return;
        
        Task.Run(() =>
        {
            try
            {
                PlayTone(440, 200); // A4
            }
            catch
            {
                // Ігноруємо помилки відтворення звуку
            }
        });
    }

    /// <summary>
    /// Генерує та відтворює тон заданої частоти та тривалості
    /// </summary>
    private void PlayTone(int frequency, int durationMs)
    {
        if (!_isEnabled) return;

        try
        {
            // Генеруємо синусоїдальний сигнал
            var sampleRate = 44100;
            var samples = (int)(sampleRate * durationMs / 1000.0);
            var amplitude = 0.3; // Гучність (30%)
            var buffer = new byte[samples * 2];

            for (int i = 0; i < samples; i++)
            {
                var sample = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * frequency * i / sampleRate));
                buffer[i * 2] = (byte)(sample & 0xFF);
                buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            // Створюємо тимчасовий WAV файл в пам'яті
            using var ms = new MemoryStream();
            WriteWavHeader(ms, samples, sampleRate);
            ms.Write(buffer, 0, buffer.Length);
            ms.Position = 0;

            // Відтворюємо звук
            using var player = new SoundPlayer(ms);
            player.PlaySync();
        }
        catch
        {
            // Ігноруємо помилки відтворення звуку
        }
    }

    /// <summary>
    /// Записує WAV заголовок для звукового файлу
    /// </summary>
    private void WriteWavHeader(Stream stream, int sampleCount, int sampleRate)
    {
        var dataSize = sampleCount * 2; // 16-bit = 2 bytes per sample
        var fileSize = 36 + dataSize;

        // RIFF header
        WriteString(stream, "RIFF");
        WriteInt(stream, fileSize);
        WriteString(stream, "WAVE");

        // fmt chunk
        WriteString(stream, "fmt ");
        WriteInt(stream, 16); // fmt chunk size
        WriteShort(stream, 1); // audio format (PCM)
        WriteShort(stream, 1); // number of channels (mono)
        WriteInt(stream, sampleRate);
        WriteInt(stream, sampleRate * 2); // byte rate
        WriteShort(stream, 2); // block align
        WriteShort(stream, 16); // bits per sample

        // data chunk
        WriteString(stream, "data");
        WriteInt(stream, dataSize);
    }

    private void WriteString(Stream stream, string s)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(s);
        stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteInt(Stream stream, int value)
    {
        var bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteShort(Stream stream, short value)
    {
        var bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Завантажує налаштування звуку з файлу
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var jsonString = File.ReadAllText(_settingsFilePath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<SoundSettings>(jsonString);
                if (settings != null)
                {
                    _isEnabled = settings.IsEnabled;
                }
            }
        }
        catch
        {
            // Якщо не вдалося завантажити, використовуємо значення за замовчуванням
            _isEnabled = true;
        }
    }

    /// <summary>
    /// Зберігає налаштування звуку в файл
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            var settings = new SoundSettings { IsEnabled = _isEnabled };
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            var jsonString = System.Text.Json.JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, jsonString);
        }
        catch
        {
            // Ігноруємо помилки збереження
        }
    }

    private class SoundSettings
    {
        public bool IsEnabled { get; set; } = true;
    }
}


