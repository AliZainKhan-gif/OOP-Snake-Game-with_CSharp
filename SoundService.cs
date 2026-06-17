using System;
using System.Media;
using System.Threading.Tasks;

namespace SnakeGameWinForms;

public sealed class SoundService
{
    public bool Enabled { get; set; } = true;

    public void PlayStart() => Play(660, 70, SystemSounds.Asterisk);
    public void PlayEat() => Play(880, 55, SystemSounds.Exclamation);
    public void PlayLevelUp() => PlaySequence((988, 60), (1175, 80));
    public void PlayPause() => Play(440, 60, SystemSounds.Question);
    public void PlayGameOver() => PlaySequence((330, 120), (220, 180));

    private void Play(int frequency, int durationMs, SystemSound fallback)
    {
        if (!Enabled)
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                Console.Beep(frequency, durationMs);
            }
            catch
            {
                fallback.Play();
            }
        });
    }

    private void PlaySequence(params (int Frequency, int DurationMs)[] notes)
    {
        if (!Enabled)
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                foreach ((int frequency, int durationMs) in notes)
                {
                    Console.Beep(frequency, durationMs);
                }
            }
            catch
            {
                SystemSounds.Beep.Play();
            }
        });
    }
}
