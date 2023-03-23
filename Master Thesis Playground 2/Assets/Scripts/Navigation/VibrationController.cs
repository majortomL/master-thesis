using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// a pattern is defined by repeating cycles; each cycle consists of a pause phase and a vibration phase;
// within the vibration phase the controller vibrates multiple times (defined by vibrationsPerCycle) separated by short pauses
// vibrateRatio defines how much time of the vibration phase is dedicated to vibrating, and how much is used for the short pauses
public readonly struct VibrationPattern
{
    public readonly float patternDuration;  // how many seconds should the pattern play (set negative to ignore value)
    public readonly int cycleCount;         // how often should the pattern repeat (set negative ignore value)

    public readonly float cycleDuration;    // how long is a single cycle (in seconds)
    public readonly int vibrationsPerCycle; // how often the controller should vibrate during vibration phase
    public readonly float pauseRatio;       // defines size of pause phase within cycle (value in [0,1])
    public readonly float vibrateRatio;     // in vibration phase, defines proportion between vibrating and short pauses (value in [0,1])

    public readonly float amplitude;
    public readonly float frequency;

    VibrationPattern(
        float _patternDuration,
        int _cycleCount,
        float _cycleDuration,
        int _vibrationsPerCycle,
        float _pauseRatio,
        float _vibrateRatio,
        float _amplitude,
        float _frequency
    )
    {
        patternDuration = _patternDuration;
        cycleCount = _cycleCount;

        cycleDuration = _cycleDuration;
        vibrationsPerCycle = _vibrationsPerCycle;
        pauseRatio = _pauseRatio;
        vibrateRatio = _vibrateRatio;

        amplitude = _amplitude;
        frequency = _frequency;
    }

    public static VibrationPattern makeTimed(
        float _amplitude,
        float _frequency,
        float _patternDuration,
        float _cycleDuration,
        int _vibrationsPerCycle = 1,
        float _vibrateRatio = 0.6f,
        float _pauseRatio = 0
    )
    {
        return new VibrationPattern(_patternDuration, -1, _cycleDuration, _vibrationsPerCycle, _pauseRatio, _vibrateRatio, _amplitude, _frequency);
    }

    public static VibrationPattern makeNumbered(
        float _amplitude,
        float _frequency,
        int _cycleCount,
        float _cycleDuration,
        int _vibrationsPerCycle = 1,
        float _vibrateRatio = 0.6f,
        float _pauseRatio = 0
    )
    {
        return new VibrationPattern(-1, _cycleCount, _cycleDuration, _vibrationsPerCycle, _pauseRatio, _vibrateRatio, _amplitude, _frequency);
    }

    public static VibrationPattern makeUnrestricted(
        float _amplitude,
        float _frequency,
        float _cycleDuration,
        int _vibrationsPerCycle = 1,
        float _vibrateRatio = 0.6f,
        float _pauseRatio = 0
    )
    {
        return new VibrationPattern(-1, -1, _cycleDuration, _vibrationsPerCycle, _pauseRatio, _vibrateRatio, _amplitude, _frequency);
    }
}

public interface VibrationController
{

    void vibrate(float frequency, float amplitude, float duration);

    void startPattern(VibrationPattern vp);
    void stopPattern(VibrationPattern vp);
}

public class VibrationHandler
{
    static VibrationController implementaion = null;

    public static void setImpl(VibrationController impl)
    {
        implementaion = impl;
    }

    private static bool valid()
    {
        if (implementaion != null) return true;
        Debug.LogWarning("Attempted to start controller vibration but no registered VibrationController.");
        return false;
    }

    public static void vibrate(float frequency, float amplitude, float duration)
    {
        if (!valid()) return;
        implementaion.vibrate(frequency, amplitude, duration);
    }

    public static void startPattern(VibrationPattern vp)
    {
        if (!valid()) return;
        implementaion.startPattern(vp);
    }
    public static void stopPattern(VibrationPattern vp)
    {
        if (!valid()) return;
        implementaion.stopPattern(vp);
    }
}


