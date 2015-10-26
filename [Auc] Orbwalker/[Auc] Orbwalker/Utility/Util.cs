using System;
using System.Collections.Generic;
using System.Threading;

namespace _Auc__Orbwalker.Utility
{
	class Util
	{
		public static volatile List<Timer> Timers = new List<Timer>();
		public static object Lockobj = new object();
		public static void DelayAction(Action action, int delayInMilliseconds)
		{
			Timer[] timer = { null };
			var cb = new TimerCallback(state =>
			{
				lock (Lockobj)
					Timers.Remove(timer[0]);
				timer[0].Dispose();
				action();
			});
			lock (Lockobj)
				Timers.Add(timer[0] = new Timer(cb, null, delayInMilliseconds, Timeout.Infinite));
		}

	}
	public class AutoResetDelay
	{
		int _started;
		public int Delay { get; private set; }

		public AutoResetDelay(int delayMilliseconds, bool startNow = false)
		{
			Delay = delayMilliseconds;
			Restart();
			if (!startNow)
				MakeReady();
		}

		public bool IsReady
		{
			get
			{
				if (MillisecondsLeft != 0) return false;
				Restart();
				return true;
			}
		}

		public int MillisecondsLeft
		{
			get
			{
				var timeSinceStart = Environment.TickCount - _started;
				var remaining = Delay - timeSinceStart;

				if (remaining < 0)
					remaining = 0;
				return remaining;
			}
		}

		public void MakeReady()
		{
			_started = Environment.TickCount - Delay;
		}
		public void Restart()
		{
			_started = Environment.TickCount;
		}
		public void Restart(int newDelayMs)
		{
			Delay = newDelayMs;
			_started = Environment.TickCount;
		}
	}
}
