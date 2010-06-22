// BuildInfo.cs created with MonoDevelop
// User: damien at 11:49 AM 10/17/2008
//
// Copyright Skull Squadron, All Rights Reserved.

using System;
using System.IO;
using EmergeTk;
using EmergeTk.Model;

namespace EmergeTk.Widgets.Html
{
	
	
	public class BuildInfo : Generic
	{		
		// private static new readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(BuildInfo));		
		
		string project;		
		static string build;
		
		public BuildInfo()
		{
		}
		
		public override void PostInitialize ()
		{
			base.PostInitialize ();
		
			//log.Debug("Going to Initialize");
			
			if ( build == null )
			{	
				//log.Debug("Going to get build");
				
				string buildResultsBaseUrl = Setting.Get("BuildResultsBaseUrl").DataValue;
				string buildInfoPath = this.RootContext.HttpContext.Server.MapPath("build.txt");
				buildInfoPath = buildInfoPath.Replace("flightcontrol/FlightControl","flightcontrol"); // Wierd bug where it's putting in an extra FlightControl
				//log.Debug("buildInfoPath is : ",buildInfoPath);
				System.IO.FileInfo buildInfo = new System.IO.FileInfo(buildInfoPath);	
				//log.Debug("Build info is at: ",buildInfo);
				if (buildInfo.Exists && !string.IsNullOrEmpty(buildResultsBaseUrl))
				{	
				//	log.Debug("Found build file and baseUrl, going to read it", buildResultsBaseUrl, buildInfoPath);
					
					// create reader & open file
					System.IO.TextReader tr = new System.IO.StreamReader(buildInfo.FullName);
					
					// read a line of text
					build = tr.ReadLine();
					
					// make build link
					
					build = "<a href=\"" + buildResultsBaseUrl + "/" + Project + "?log=log" + build + "\">" + build + "</a>";
					
					// close the stream
					tr.Close();
					
				//	log.Debug("Got build: " + build);
				}
//				else
//					log.Debug("Didn't find Build file",buildInfo);
			}		
			
			if ( build != null )
			{
				Label buildInfoLabel = this.RootContext.CreateWidget<Label>(this);	
				buildInfoLabel.Text = build;	
			}
//			else
//			{
//				log.Debug("Didn't add BuildInfoLabel because build was null");
//			}
		}
		
		public string Project {
			get {
				return project;
			}
			set {
				project = value;
			}
		}
	}
}
