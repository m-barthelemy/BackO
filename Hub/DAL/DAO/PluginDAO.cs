using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;


namespace P2PBackupHub.DAL {

	public class PluginDAO {

		IDbConnection dbc;
		private User sessionUser;

		public PluginDAO() {
		}

		public PluginDAO(User currentUser) {
			sessionUser = currentUser;
		}

		public List<Plugin> GetForNode(int nodeId){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select<Plugin>( p => p.NodeId == nodeId);
			}
		}

		public void AddOrUpdateForNode( int nodeId, List<Plugin> plugins){
			using(dbc = DAL.Instance.GetDb())
			using(IDbTransaction dbt = dbc.BeginTransaction()){
				try{
					dbc.Delete<Plugin>( p => p.NodeId == nodeId);
					List<Plugin> nodePlugins = new List<Plugin>(plugins);
					foreach(Plugin p in nodePlugins)
						p.NodeId = nodeId;
					dbc.InsertAll<Plugin>(nodePlugins);
					dbt.Commit();
				}
				catch(Exception e){
					Utilities.Logger.Append("DAO", Severity.ERROR, "Could not update Node #"+nodeId+" plugins : "+e.ToString());
					dbt.Rollback();
					throw(e);
				}

			}
		}

		/// <summary>
		/// Gets all the plugins available on at least 1 node. 
		/// Multiple nodes having the same plugins won't generate duplicate entries here.
		/// </summary>
		public List<Plugin> GetDistinctAvailable(PluginCategory pluginType){
			SqlExpressionVisitor<Plugin> ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<Plugin>();
			ev.Where(p => p.Category == pluginType);
			ev.SelectDistinct(pl => pl.Name);
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select(ev);
				//return (from Plugin p in dbc.Select(ev) select p.Name).ToList();
			}
		}

	}
}

