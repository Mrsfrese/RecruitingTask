namespace LogTest
{
	using System;
	using System.Collections.Concurrent;
	using System.IO;
	using System.Text;
	using System.Threading;

	public class AsyncLogInterface : LogInterface
	{
		private readonly BlockingCollection<LogLine> _queue;
		private readonly Thread _workerThread;
		private readonly string _logDirectory;
		private readonly IClock _clock;

		private StreamWriter _writer;
		private DateTime? _currentFileDate;
		private volatile bool _isStopping;

		// Uses default log directory and system clock
		public AsyncLogInterface() : this(@"./LogTest", new SystemClock())
		{
		}

		// Initializes logger dependencies and start background worker thread
		public AsyncLogInterface(string logDirectory, IClock clock)
		{

			_logDirectory = logDirectory;
			_clock = clock;
			_queue = new BlockingCollection<LogLine>();

			Directory.CreateDirectory(_logDirectory);


			_workerThread = new Thread(MainLoop)
			{
				IsBackground = true
			};

			_workerThread.Start();
		}

		// Adds a log entry to the queue without blocking the caller. If the logger is stopping, it will ignore new log entries.
		public void WriteLog(string text)
		{
			if (_isStopping || _queue.IsAddingCompleted)

				return;

			try
			{
				_queue.Add(new LogLine
				{
					Text = text,
					Timestamp = _clock.Now
				});
			}
			catch
			{
				// Logging errors should not crash the application
			}
		}

		// Stops the logger and flushes all pending log entries to the file. This method blocks until all logs are written and resources are cleaned up.
		public void Stop_With_Flush()
		{
			_isStopping = true;
			_queue.CompleteAdding();
			_workerThread.Join();
			CloseWriter();
		}

		// Stops the logger immediately and discards remaining log entries.
		public void Stop_Without_Flush()
		{
			_isStopping = true;

			while (_queue.TryTake(out _))
			{
			}

			_queue.CompleteAdding();
			_workerThread.Join();
			CloseWriter();
		}

		// Background worker loop that consumes queued logs and writes them to a file
		private void MainLoop()
		{
			try
			{
				foreach (LogLine logLine in _queue.GetConsumingEnumerable())
				{
					WriteLineToFile(logLine);
				}
			}
			finally
			{
				CloseWriter();
			}
		}

		// Writes a single log entry to the correct log file based on its timestamp. It ensures that logs are written to daily files and handles any file I/O errors gracefully.
		private void WriteLineToFile(LogLine logLine)
		{
			try
			{
				EnsureCorrectFile(logLine.Timestamp);

				StringBuilder sb = new StringBuilder();

				sb.Append(logLine.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				sb.Append("\t");
				sb.Append(logLine.LineText());

				_writer.WriteLine(sb.ToString());
			}
			catch
			{
				// Logging errors should not crash the application
			}
		}

		// Ensures that logs are written to the correct file based on the current date. If the date has changed since the last log entry, it closes the current file and opens a new one for the new date.
		private void EnsureCorrectFile(DateTime timestamp)
		{
			DateTime logDate = timestamp.Date;

			if (_writer != null && _currentFileDate == logDate)
				return;

			CloseWriter();

			_currentFileDate = logDate;

			string filePath = Path.Combine(_logDirectory, "Log" + timestamp.ToString("yyyyMMdd") + ".log");

			_writer = new StreamWriter(filePath, append: true);
			_writer.AutoFlush = true;

			_writer.WriteLine("Timestamp".PadRight(25, ' ') + "\t" + "Data".PadLeft(15, ' '));
		}

		// Flushes and closes the current StreamWriter. If there is no active StreamWriter, it does nothing. 
		private void CloseWriter()
		{
			if (_writer == null)
				return;

			_writer.Flush();
			_writer.Dispose();
			_writer = null;
		}
	}
}