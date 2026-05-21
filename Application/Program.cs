using System;
using System.Threading;
using LogTest;

namespace Application
{
	class Program
	{
		static void Main(string[] args)
		{
			LogInterface loggerWithFlush = new AsyncLogInterface();

			for (int i = 0; i < 15; i++)
			{
				loggerWithFlush.WriteLog("Number with Flush: " + i.ToString());
				Thread.Sleep(50);
			}

			loggerWithFlush.Stop_With_Flush();

			LogInterface loggerWithoutFlush = new AsyncLogInterface();

			for (int i = 50; i > 0; i--)
			{
				loggerWithoutFlush.WriteLog("Number with No flush: " + i.ToString());
				Thread.Sleep(20);
			}

			loggerWithoutFlush.Stop_Without_Flush();

			Console.ReadLine();
		}
	}
}