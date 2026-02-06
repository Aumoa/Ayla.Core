using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Ayla;

public readonly struct TimerScope : IDisposable
{
    private readonly string m_Format;
    private readonly Stopwatch m_Stopwatch;

    public TimerScope(string format)
    {
        m_Format = format;
        m_Stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        const double _1ms = 1.0;
        const double _8s = 1000.0 * 8;

        double delta = m_Stopwatch.Elapsed.TotalMilliseconds;

        if (delta < _1ms)
        {
            double microseconds = delta * 1000.0;
            Debug.LogFormat(m_Format, microseconds.ToString("F2") + "µs");
        }
        else if (delta < _8s)
        {
            Debug.LogFormat(m_Format, delta.ToString("F2") + "ms");
        }
        else
        {
            double seconds = delta / 1000.0;
            Debug.LogFormat(m_Format, seconds.ToString("F2") + "s");
        }
    }
}
