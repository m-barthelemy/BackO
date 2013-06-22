using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using P2PBackup.Common;
//using Node.StorageLayer;
//using Node.Snapshots;
//using Node.Virtualization;

namespace P2PBackup.Common {

	public class PluginsDiscoverer {

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		private static readonly PluginsDiscoverer _instance = new PluginsDiscoverer();
		//Type[] wantedPluginInterfaces = new Type[]{typeof(IStorageDiscoverer), typeof(ISpecialObject)};
		Dictionary<string, Plugin> plugins;
		//Dictionary<string, Type> specialObjectsPlugins;

		public Dictionary<string, Plugin> Plugins{get{return plugins;}}
		//public Dictionary<string, Type> SpecialObjectsPlugins{get{return specialObjectsPlugins;}}

		public static PluginsDiscoverer Instance(){
			return _instance;
		}
		
		private PluginsDiscoverer(){

		}

		public void Start(){
			if(plugins != null) return;

			plugins = new Dictionary<string, Plugin>();

			string libsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Replace("bin", "lib");
			foreach(string assemblyName in Directory.EnumerateFiles(libsPath, "*.dll")){
				try{
					ScanAssembly(assemblyName);
				}
				catch(Exception e){
					LogEvent(this, new LogEventArgs{Severity = Severity.NOTICE, 
						Message="Could not browse assembly "+assemblyName+" for plugins : "+e.Message}
					);
				}
			}

		}
			
		public List<Plugin> GetPlugins<T>(){
			/*List<Plugin> plugins = new List<Plugin>();
			foreach(Plugin p in storagePlugins.Values)
				plugins.Add(new Plugin{Name = p.Name, Version = p.Version, Category=typeof(IStorageDiscoverer)});

			return plugins;*/
			return new List<Plugin>(plugins.Values.Where(p => p.RawType is T));
		}

		private void ScanAssembly(string assemblyPath){
			Assembly assembly = Assembly.LoadFrom(assemblyPath);
			//if(assembly == null) return;;

			Type[] types = assembly.GetExportedTypes();
			foreach(Type t in types){
				if(assemblyPath.EndsWith( "VMWare.dll"))
					Console.WriteLine ("Type="+t.FullName+", is discoverer = "+typeof(IStorageDiscoverer).IsAssignableFrom(t));

				//check if library type inplements one of the recognized extensibility interfaces
				if(
						(typeof(IStorageDiscoverer).IsAssignableFrom(t) || typeof(ISpecialObject).IsAssignableFrom(t))
						&& t.IsPublic && t.IsClass)
					{
					Console.WriteLine ("type "+t.FullName+" matches");
					LogEvent(this, 
					         new LogEventArgs{Severity = Severity.DEBUG, Message="Plugin search : type "+t.FullName+" matches"});
					//final validity checks : plugin must have a Name and Version
					try{
						IPlugin sd;
						sd = (IPlugin)Activator.CreateInstance(t);
					
						if(sd.Name != null && sd.Version != null){
							plugins.Add(sd.Name, 
								new Plugin{
										Category= ( (typeof(IStorageDiscoverer).IsAssignableFrom(t))?PluginCategory.IStorageDiscoverer:PluginCategory.ISpecialObject), 
										Name = sd.Name, 
										Version = sd.Version,
										IsProxyingPlugin = sd.IsProxyingPlugin,
										RawType = t
								}
							);
							Console.WriteLine ("Found new Plugin in "+assembly.GetName().Name
							                   +", Name="+sd.Name+", Version="+sd.Version);

							if(LogEvent != null) LogEvent(this, 
								new LogEventArgs{Severity = Severity.INFO, Message="Found new Plugin in "+assembly.GetName().Name
								+", Name="+sd.Name+", Version="+sd.Version});
						}
					}
					catch(System.Reflection.TargetInvocationException tie){
						LogEvent(this, new LogEventArgs{Severity = Severity.NOTICE, Message="Error testing plugin : "+tie.InnerException.Message});
					}
					catch(Exception iv){
						LogEvent(this, new LogEventArgs{Severity = Severity.NOTICE, Message="Invalid plugin : "+iv.ToString()});

					}
				}
				/*else if(typeof(ISpecialObject).IsAssignableFrom(t) && t.IsPublic && t.IsClass){
					//final validity checks : plugin must have a Name and Version
					try{
						ISpecialObject spo = (ISpecialObject)Activator.CreateInstance(t);
						if(spo.Name != null && spo.Version != null){
							specialObjectsPlugins.Add(spo.Name, t); //new Plugin{Category=t, Name = spo.Name, Version = spo.Version});
							if(LogEvent != null) LogEvent(this, 
								new LogEventArgs{Severity = Severity.INFO, Message="Found new Plugin in "+assembly.GetName().Name
								+", Name="+spo.Name+", Version="+spo.Version});
						}
					}
					catch{}
				}*/
			}
		}
		
	}
}

