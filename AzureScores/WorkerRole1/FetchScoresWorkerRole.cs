using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WorkerRole1
{
    public class FetchScoresWorkerRole : RoleEntryPoint
    {
        private Fetcher fetcher;


        public FetchScoresWorkerRole()
        {
            fetcher = new Fetcher();
        }

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("WorkerRole1 entry point called", "Information");

            while (true)
            {
                Trace.WriteLine("Working", "Going to fetch the scores");
                fetcher.LoadGamesForAllDivisions();
                Trace.WriteLine("Working","All games loaded");
                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
