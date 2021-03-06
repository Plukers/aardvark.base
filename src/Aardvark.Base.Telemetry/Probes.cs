﻿#pragma warning disable 618
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Aardvark.Base
{
    public static partial class Telemetry
    {
        #region Counters

        public class Counter : IProbe<long>
        {
            private long m_value = 0;

            public Counter() => OnReset += (s, e) => Interlocked.Exchange(ref m_value, 0);

            public void Increment() => Interlocked.Increment(ref m_value);

            public void Increment(long x) => Interlocked.Add(ref m_value, x);

            public void Decrement() => Interlocked.Decrement(ref m_value);

            public void Decrement(long x) => Interlocked.Add(ref m_value, -x);

            public void Set(long x) => m_value = x;
            
            public long Value => m_value;

            public double ValueDouble => m_value;
        }

        #endregion

        #region Timers

        /// <summary>
        /// Uses a StopWatch to time the code block.
        /// Measurement also includes times when the thread is blocked.
        /// </summary>
        public class StopWatchTime : IProbe<TimeSpan>
        {
            private long m_sum = 0;

            public StopWatchTime() => OnReset += (s, e) => Interlocked.Exchange(ref m_sum, 0);

            public IDisposable Timer
            {
                get
                { 
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    return ProbeDisposable.Create(() =>
                    {
                        Interlocked.Add(ref m_sum, stopwatch.Elapsed.Ticks);
                    });
                }
            }

            public TimeSpan Value => TimeSpan.FromTicks(m_sum);

            public double ValueDouble => Value.TotalSeconds;
        }

        /// <summary>
        /// Overlapping timings from multiple threads will not be accumulated.
        /// For example, if 4 threads simultanously time a code block at 1 second
        /// using the same WallClockTime object, then recorded time will show 1 second.
        /// This is in contrast to CpuTime where it would show 4 seconds.
        /// </summary>
        public class WallClockTime : IProbe<TimeSpan>
        {
            private int m_actives = 0;
            private Stopwatch m_stopwatch = new Stopwatch();

            public WallClockTime() => OnReset += (s, e) => m_stopwatch.Reset();
            
            public IDisposable Timer
            {
                get
                {
                    if (Interlocked.Increment(ref m_actives) == 1)
                    {
                        m_stopwatch.Start();
                    }

                    return ProbeDisposable.Create(() =>
                    {
                        if (Interlocked.Decrement(ref m_actives) == 0)
                        {
                            m_stopwatch.Stop();
                        }
                    });
                }
            }

            public TimeSpan Value => m_stopwatch.Elapsed;

            public double ValueDouble => Value.TotalSeconds;
        }

        /// <summary>
        /// Measures the amount of time that the thread has spent using
        /// the processor (application code + operating system code).
        /// </summary>
        public class CpuTime : IProbe<TimeSpan>
        {
            private ProcessThread GetCurrentProcessThread()
            {
                Thread.BeginThreadAffinity();
                var tid = AppDomain.GetCurrentThreadId();
                if (m_threadLocalWin32ThreadId.Value != tid)
                {
                    m_threadLocalWin32ThreadId.Value = tid;
                    m_threadLocalProcessThread.Value = HardwareThread.GetProcessThread(tid);
                }
                Thread.EndThreadAffinity();
                return m_threadLocalProcessThread.Value;
            }
            
            private ThreadLocal<int> m_threadLocalWin32ThreadId = new ThreadLocal<int>(() => -1);
            private ThreadLocal<ProcessThread> m_threadLocalProcessThread = new ThreadLocal<ProcessThread>();
            
            private ThreadLocal<bool> m_active = new ThreadLocal<bool>(() => false);

            private long m_sum = 0;

            private bool m_measureUserTime = false;
            private long m_sumUser = 0;
            private IProbe<TimeSpan> m_probeUser = null;

            private bool m_measurePrivilegedTime = false;
            private long m_sumPrivileged = 0;
            private IProbe<TimeSpan> m_probePrivileged = null;

            public CpuTime() => OnReset += (s, e) =>
            {
                Interlocked.Exchange(ref m_sum, 0);
                Interlocked.Exchange(ref m_sumUser, 0);
                Interlocked.Exchange(ref m_sumPrivileged, 0);
            };

            public IDisposable Timer
            {
                get
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;

                    if (m_active.Value) throw new InvalidOperationException(
                        "Telemetry.StopWatchTime: nested start on same thread.");

                    long t0User = 0, t0Privileged = 0;
                    long t1User = 0, t1Privileged = 0;
                    
                    var pt = GetCurrentProcessThread();
                    if (pt == null) return Disposable.Create(() => { });
                    var t0 = pt.TotalProcessorTime.Ticks;
                    if (m_measureUserTime) t0User = pt.UserProcessorTime.Ticks;
                    if (m_measurePrivilegedTime) t0Privileged = pt.PrivilegedProcessorTime.Ticks;

                    m_active.Value = true;

                    return ProbeDisposable.Create(() =>
                    {
                        pt = GetCurrentProcessThread();
                        var t1 = pt.TotalProcessorTime.Ticks;
                        if (m_measureUserTime) t1User = pt.UserProcessorTime.Ticks;
                        if (m_measurePrivilegedTime) t1Privileged = pt.PrivilegedProcessorTime.Ticks;

                        Interlocked.Add(ref m_sum, t1 - t0);
                        if (m_measureUserTime) Interlocked.Add(ref m_sumUser, t1User - t0User);
                        if (m_measurePrivilegedTime) Interlocked.Add(ref m_sumPrivileged, t1Privileged - t0Privileged);

                        m_active.Value = false;
                    });

                }
            }

            /// <summary>
            /// Gets the time that was spent in application code.
            /// </summary>
            public IProbe<TimeSpan> UserTime
            {
                get
                {
                    if (!m_measureUserTime)
                    {
                        m_measureUserTime = true;
                        m_probeUser = new CustomProbeTimeSpan(() => TimeSpan.FromTicks(m_sumUser));
                    }
                    return m_probeUser;
                }
            }

            /// <summary>
            /// Gets the time that was spent in operating system code.
            /// </summary>
            public IProbe<TimeSpan> PrivilegedTime
            {
                get
                {
                    if (!m_measurePrivilegedTime)
                    {
                        m_measurePrivilegedTime = true;
                        m_probePrivileged = new CustomProbeTimeSpan(() => TimeSpan.FromTicks(m_sumPrivileged));
                    }
                    return m_probePrivileged;
                }
            }

            public TimeSpan Value => TimeSpan.FromTicks(m_sum);

            public double ValueDouble => Value.TotalSeconds;
        }

        /// <summary>
        /// Measures the amount of time that the thread has spent using
        /// the processor inside the application.
        /// </summary>
        public class CpuTimeUser : IProbe<TimeSpan>
        {
            private ProcessThread GetCurrentProcessThread()
            {
                Thread.BeginThreadAffinity();
                var tid = AppDomain.GetCurrentThreadId();
                if (m_threadLocalWin32ThreadId.Value != tid)
                {
                    m_threadLocalWin32ThreadId.Value = tid;
                    m_threadLocalProcessThread.Value = HardwareThread.GetProcessThread(tid);
                }
                Thread.EndThreadAffinity();
                return m_threadLocalProcessThread.Value;
            }

            private ThreadLocal<int> m_threadLocalWin32ThreadId = new ThreadLocal<int>(() => -1);
            private ThreadLocal<ProcessThread> m_threadLocalProcessThread = new ThreadLocal<ProcessThread>();

            private HashSet<int> m_threadIds = new HashSet<int>();
            private TimeSpan m_sum = TimeSpan.Zero;

            public CpuTimeUser() => OnReset += (s, e) => { lock (m_threadIds) m_sum = TimeSpan.Zero; };
            
            public IDisposable Timer
            {
                get
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    lock (m_threadIds)
                    {
                        if (m_threadIds.Contains(threadId))
                        {
                            throw new InvalidOperationException(
                                "Telemetry.CpuTime: nested start on same thread."
                                );
                        }
                        else
                        {
                            var p = GetCurrentProcessThread();
                            if (p == null) return Disposable.Create(() => { });
                            var t0 = p.UserProcessorTime;
                            m_threadIds.Add(threadId);
                            return ProbeDisposable.Create(() =>
                            {
                                //if (GetCurrentWin32ThreadId() != tid) Debugger.Break();
                                var p2 = GetCurrentProcessThread();
                                if (p2 != null)
                                {
                                    var t1 = p2.UserProcessorTime;
                                    lock (m_threadIds)
                                    {
                                        m_sum += t1 - t0;
                                        m_threadIds.Remove(threadId);
                                    }
                                }
                            });
                        }
                    }
                }
            }

            public TimeSpan Value
            {
                get
                {
                    lock (m_threadIds) return m_sum;
                }
            }

            public double ValueDouble => Value.TotalSeconds;
        }

        /// <summary>
        /// Measures the amount of time that the thread has spent using
        /// the processor inside the operating system core.
        /// </summary>
        public class CpuTimePrivileged : IProbe<TimeSpan>
        {
            private ProcessThread GetCurrentProcessThread()
            {
                Thread.BeginThreadAffinity();
                var tid = AppDomain.GetCurrentThreadId();
                if (m_threadLocalWin32ThreadId.Value != tid)
                {
                    m_threadLocalWin32ThreadId.Value = tid;
                    m_threadLocalProcessThread.Value = HardwareThread.GetProcessThread(tid);
                }
                Thread.EndThreadAffinity();
                return m_threadLocalProcessThread.Value;
            }
            private ThreadLocal<int> m_threadLocalWin32ThreadId = new ThreadLocal<int>(() => -1);
            private ThreadLocal<ProcessThread> m_threadLocalProcessThread = new ThreadLocal<ProcessThread>();

            private HashSet<int> m_threadIds = new HashSet<int>();
            private TimeSpan m_sum = TimeSpan.Zero;

            public CpuTimePrivileged()
            {
                OnReset += (s, e) => { lock (m_threadIds) m_sum = TimeSpan.Zero; };
            }

            public IDisposable Timer
            {
                get
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    lock (m_threadIds)
                    {
                        if (m_threadIds.Contains(threadId))
                        {
                            throw new InvalidOperationException(
                                "Telemetry.CpuTime: nested start on same thread."
                                );
                        }
                        else
                        {
                            // /* var tid = */ HardwareThread.GetCurrentWin32ThreadId(); sm?
                            var p = GetCurrentProcessThread();
                            if (p == null) return Disposable.Create(() => { });
                            var t0 = p.PrivilegedProcessorTime;
                            m_threadIds.Add(threadId);
                            return ProbeDisposable.Create(() =>
                            {
                                //if (GetCurrentWin32ThreadId() != tid) Debugger.Break();
                                var p2 = GetCurrentProcessThread();
                                if (p2 != null)
                                {
                                    var t1 = p2.PrivilegedProcessorTime;
                                    lock (m_threadIds)
                                    {
                                        m_sum += t1 - t0;
                                        m_threadIds.Remove(threadId);
                                    }
                                }
                            });
                        }
                    }
                }
            }

            public TimeSpan Value
            {
                get
                {
                    lock (m_threadIds) return m_sum;
                }
            }
            public double ValueDouble => Value.TotalSeconds;
        }

        #endregion

        #region Custom probes

        public class CustomProbeDouble : IProbe<double>
        {
            private Func<double> m_value;

            public CustomProbeDouble(Func<double> valueFunc) => m_value = valueFunc;

            public double Value => m_value();
            
            public double ValueDouble => m_value();
        }

        public class CustomProbeLong : IProbe<long>
        {
            private Func<long> m_value;

            public CustomProbeLong(Func<long> valueFunc) => m_value = valueFunc;

            public long Value => m_value();

            public double ValueDouble => m_value();
        }

        public class CustomProbeTimeSpan : IProbe<TimeSpan>
        {
            private Func<TimeSpan> m_value;

            public CustomProbeTimeSpan(Func<TimeSpan> valueFunc) => m_value = valueFunc;

            public TimeSpan Value => m_value();

            public Func<TimeSpan> Getter { set { m_value = value; } }

            public double ValueDouble => m_value().TotalSeconds;
        }

        public class CustomProbeString : IProbe<string>
        {
            private Func<string> m_value;

            public CustomProbeString(Func<string> valueFunc) => m_value = valueFunc;

            public string Value => m_value();

            public double ValueDouble => 0.0;
        }

        #endregion

        #region Snapshot probes

        /// <summary>
        /// Reports difference to current value of probe.
        /// </summary>
        public class SnapshotProbeLong : IProbe<long>
        {
            private IProbe<long> m_probe;
            private long m_base;

            /// <summary>
            /// Reports difference to current value of probe.
            /// </summary>
            public SnapshotProbeLong(IProbe<long> probe)
            {
                m_probe = probe ?? throw new ArgumentNullException();
                m_base = -probe.Value;
                OnReset += (s, e) => m_base = 0;
            }

            public long Value => m_base + m_probe.Value;

            public double ValueDouble => m_base + m_probe.Value;
        }

        /// <summary>
        /// Reports difference to current value of probe.
        /// </summary>
        public class SnapshotProbeDouble : IProbe<double>
        {
            private IProbe<double> m_probe;
            private double m_base;

            /// <summary>
            /// Reports difference to current value of probe.
            /// </summary>
            public SnapshotProbeDouble(IProbe<double> probe)
            {
                m_probe = probe ?? throw new ArgumentNullException();
                m_base = -probe.Value;
                OnReset += (s, e) => m_base = 0;
            }

            public double Value => m_base + m_probe.Value;

            public double ValueDouble => m_base + m_probe.Value;
        }

        /// <summary>
        /// Reports difference to current value of probe.
        /// </summary>
        public class SnapshotProbeTimeSpan : IProbe<TimeSpan>
        {
            private IProbe<TimeSpan> m_probe;
            private long m_base;

            /// <summary>
            /// Reports difference to current value of probe.
            /// </summary>
            public SnapshotProbeTimeSpan(IProbe<TimeSpan> probe)
            {
                m_probe = probe ?? throw new ArgumentNullException();
                m_base = -probe.Value.Ticks;
                OnReset += (s, e) => m_base = 0;
            }

            public TimeSpan Value => TimeSpan.FromTicks(m_base + m_probe.Value.Ticks);

            public double ValueDouble => Value.TotalSeconds;
        }

        #endregion

        #region Private

        private struct ProbeDisposable : IDisposable
        {
            public Action Action;

            public void Dispose() => Action();

            public static ProbeDisposable Create(Action a) => new ProbeDisposable { Action = a };
        }

        #endregion
    }
}
