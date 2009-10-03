// ****************************************************************
// This is free software licensed under the NUnit license. You
// may obtain a copy of the license as well as information regarding
// copyright ownership at http://nunit.org.
// ****************************************************************

using System;
using System.Threading;
using System.Configuration;
using System.Collections.Specialized;

namespace NUnit.Core
{
	/// <summary>
	/// TestRunnerThread encapsulates running a test on a thread.
	/// It knows how to create the thread based on configuration
	/// settings and can cancel abort the test if necessary.
	/// </summary>
	public class TestRunnerThread
	{
		#region Private Fields

		/// <summary>
		/// The Test runner to be used in running tests on the thread
		/// </summary>
		private TestRunner runner;

		/// <summary>
		/// The System.Threading.Thread created by the object
		/// </summary>
		private Thread thread;

		/// <summary>
		/// The EventListener interface to receive test events
		/// </summary>
		private NUnit.Core.ITestListener listener;

		/// <summary>
		/// Array of test names for ues by the thread proc
		/// </summary>
		//private string[] testNames;
		private TestFilter filter;
			
		/// <summary>
		/// Array of returned results
		/// </summary>
		private TestResult[] results;

		#endregion

		#region Properties

		/// <summary>
		/// True if the thread is executing
		/// </summary>
		public bool IsAlive
		{
			get	{ return this.thread.IsAlive; }
		}

		/// <summary>
		/// Array of returned results
		/// </summary>
		public TestResult[] Results
		{
			get { return results; }
		}

		#endregion

		#region Constructor

		public TestRunnerThread( TestRunner runner ) 
		{ 
			this.runner = runner;
			this.thread = new Thread( new ThreadStart( TestRunnerThreadProc ) );
			thread.IsBackground = true;
			thread.Name = "TestRunnerThread";
            thread.Priority = NUnitConfiguration.ThreadPriority;
            if (NUnitConfiguration.ApartmentState != ApartmentState.Unknown)
#if CLR_2_0
                thread.SetApartmentState(NUnitConfiguration.ApartmentState);
#else
                thread.ApartmentState = NUnitConfiguration.ApartmentState;
#endif
		}

		#endregion

		#region Public Methods

		public void Wait()
		{
			if ( this.thread.IsAlive )
				this.thread.Join();
		}

		public void Cancel()
		{
			this.thread.Abort(); // Request abort first

			// Wake up the thread if necessary
			if ( ( this.thread.ThreadState & ThreadState.WaitSleepJoin ) != 0 )
				this.thread.Interrupt();
		}

		public void StartRun( ITestListener listener )
		{
			StartRun( listener, TestFilter.Empty );
		}

		public void StartRun( ITestListener listener, TestFilter filter )
		{
			this.listener = listener;
			this.filter = filter;

			thread.Start();
		}

		#endregion

		#region Thread Proc
		/// <summary>
		/// The thread proc for our actual test run
		/// </summary>
		private void TestRunnerThreadProc()
		{
            try
            {
                results = new TestResult[] { runner.Run(this.listener, this.filter) };
            }
            catch (Exception ex)
            {
                if ( !(ex is ThreadAbortException) )
                    throw new ApplicationException("Exception in TestRunnerThread", ex);
            }
		}
		#endregion
	}
}