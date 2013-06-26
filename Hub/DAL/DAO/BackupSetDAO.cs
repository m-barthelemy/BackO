using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackupHub.DAL {


	public class BackupSetDAO {

		IDbConnection dbc;
		User currentUser;

		public BackupSetDAO() {

		}

		public BackupSetDAO(User sessionUser) {
			currentUser = sessionUser;
		}

		public BackupSet Save(BackupSet bs){
			using (dbc = DAL.Instance.GetDb()){
				bs.Id = IdManager.GetId();
				bs.Generation = 0;
				dbc.Insert(bs);
				foreach(ScheduleTime st in bs.ScheduleTimes){
					st.BackupSetId = bs.Id;
				}
				new ScheduleDAO().Save(bs.ScheduleTimes);
			}
			return bs;
		}

		/// <summary>
		/// Update the specified BackupSet, and its ScheduleTimes.
		/// TEchnically, there is no such thing as 'update' for BackupSet object, since we need to keep it without modifications
		/// for past backup tasks to be restorable. So instead of updating, we insert a brand new object with an incremented 'Generation'
		/// </summary>
		/// <param name='bs'>
		/// Bs.
		/// </param>
		public BackupSet Update(BackupSet bs){

			// for security reasons, don't trust generation received from the wild
			bs.Generation = GetById(bs.Id).Generation;
			using (dbc = DAL.Instance.GetDb()){
				dbc.Open();
				IDbTransaction dbt = dbc.BeginTransaction();
				try{
					bs.Generation ++;
					dbc.Update<BackupSet>(new { Enabled = false  }, p => p.Id == bs.Id && p.Enabled);
					new ScheduleDAO().Delete(bs.Id);
					dbc.Insert(bs);

					foreach(ScheduleTime st in bs.ScheduleTimes){
						st.BackupSetId = bs.Id;
					}
					new ScheduleDAO().Save(bs.ScheduleTimes);
					dbt.Commit();
				}
				catch(Exception e){ // we want to log what happens since it's more than a simple SQL query
					Utilities.Logger.Append("DAO", Severity.ERROR, "Could not update BackupSet #"+bs.Id+" : "+e.ToString());
					dbt.Rollback();
					throw(e);
				}
			}
			return bs;
		}

		public BackupSet GetById(int id){
			using (dbc = DAL.Instance.GetDb()){
				BackupSet bs = dbc.Select<BackupSet>(b => b.Id == id && b.Enabled)[0];
				                                   /* && b.Generation == (dbc.GetScalar<BackupSet, int>(sbs =>  Sql.Max(sbs.Generation), sbs => sbs.Id == id))
				                                           )[0];*/
				                
				bs.ScheduleTimes.AddRange(new ScheduleDAO().GetForBS(id));
				return bs;
			}
		}

		public List<BackupSet> GetNodeBackupSets(uint nodeId){
			SqlExpressionVisitor<BackupSet> ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<BackupSet>();
			ev.Where(bs => bs.NodeId == nodeId && bs.Enabled);
			ev.OrderBy(bs => bs.Id);
			using (dbc = DAL.Instance.GetDb()){
				return dbc.Select<BackupSet>(ev);

			}
		}

		/// <summary>
		/// Gets the next Backupsets to be scheduled for running.
		/// </summary>
		/// <returns>
		/// The next to schedule.
		/// </returns>
		/// <param name='interval'>
		/// Interval, in minutes.
		/// </param>
		public List<BackupSet> GetNextToSchedule(int interval){
			Console.WriteLine(" -- - - - GetNextToSchedule : wanted interval (minutes)="+interval);
			DateTime now = DateTime.Now;
			DateTime endInterval = DateTime.Now.AddMinutes(interval);
			using(dbc = DAL.Instance.GetDb()){
				SqlExpressionVisitor<ScheduleTime> ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<ScheduleTime>();
				ev.Where( st => st.Day == DayOfWeek.Friday);
				//ev.O
				var jn = new JoinSqlBuilder<BackupSet, ScheduleTime>();
				//string endperiod = DateTime.Now.AddMinutes(interval).Hour+":"+DateTime.Now.AddMinutes(interval).Minute;

				// 1 - gather what to schedule for the rest of the current hour

				jn = jn.Join<BackupSet, ScheduleTime>(x => x.Id, y => y.BackupSetId/*, null,  x=> new{y}*/)
					.Where<BackupSet>(x => x.Enabled)
						.And<BackupSet>(x => !x.IsTemplate)
						/*.And<BackupSet>(x => x.Generation == 
						(dbc.GetScalar<BackupSet, int>(bs =>  Sql.Max(bs.Generation), bs => bs.Id == x.Id))
					)*/
						.And<ScheduleTime>(y => y.Day == now.DayOfWeek 
						                   && y.BeginHour == now.Hour
						                   && y.BeginMinute >= now.Minute
						                   /*y.Day == endInterval.DayOfWeek*/);

				// 2 -remaining hours of the current day 
				if(endInterval.DayOfWeek == now.DayOfWeek && endInterval.Hour > now.Hour+1){
					jn = jn.And<ScheduleTime>(s => s.Day == now.DayOfWeek 
					                          && s.BeginHour > now.Hour
					                          && s.BeginHour < endInterval.Hour);

					jn = jn.Or<ScheduleTime>(s => s.BeginHour == endInterval.Hour
					                         	&& s.BeginMinute < endInterval.Minute);
				}
				else if(endInterval.DayOfWeek != now.DayOfWeek){
					jn = jn.And<ScheduleTime>(s => s.Day == now.DayOfWeek 
					                         && s.BeginHour > now.Hour
					                         && s.BeginHour <= 23);
				}


				// 3- loop and add every complete (24h) day until (interval_end_day -1), if any.
				int nbOfDays = endInterval.Subtract(now).Days;
				for(int i=1; i<= nbOfDays; i++){
					jn = jn.Or<ScheduleTime>(s => s.Day == now.AddDays(i).DayOfWeek);
				}
			


				if(endInterval.DayOfWeek != now.DayOfWeek){
					// remaining hours of the end interval day
					jn = jn.Or<ScheduleTime>(y => y.Day == endInterval.DayOfWeek
					                          && y.BeginHour < endInterval.Hour
					                          //&& y.BeginMinute < endInterval.Minute
					                          );
					// remaining minutes
					jn = jn.Or<ScheduleTime>(y => y.Day == endInterval.DayOfWeek
					                         && y.BeginHour == endInterval.Hour
					                         && y.BeginMinute < endInterval.Minute
					                         );
				}

				/*else{ // remaining minutes
					jn = jn.And<ScheduleTime>(y => y.Day == endInterval.DayOfWeek
					                         && y.BeginHour == endInterval.Hour
					                         && y.BeginMinute < endInterval.Minute
					                         );
				}*/

				/*else if(endInterval.Hour > DateTime.Now.Hour){
					jn = jn.And<ScheduleTime>(y => y.BeginHour < endInterval.Hour
					                          && y.BeginMinute < endInterval.Minute);
				}
				else if(endInterval.Hour== DateTime.Now.Hour){
					jn = jn.And<ScheduleTime>(y => y.BeginMinute < endInterval.Minute);
				}*/

				// minutes of end hour
				/*jn = jn.Or<ScheduleTime>(y => y.Day == endInterval.DayOfWeek
				                         && y.BeginHour <= endInterval.Hour
				                         && y.BeginMinute < endInterval.Minute
				                         );*/

				jn.SelectAll<BackupSet>();
				Console.WriteLine("next to schedule SQL = "+jn.ToSql());
				return dbc.Query<BackupSet>(jn.ToSql());
			}
		}

		public List<BackupSetSchedule> GetPlan(DateTime from, DateTime to){
			//return GetNextToSchedule((int)to.Subtract(DateTime.Now).TotalMinutes);
			int interval = (int)to.Subtract(DateTime.Now).TotalMinutes;
			Console.WriteLine(" -- - - - GetPlan(): wanted interval (minutes)="+interval);
			DateTime now = DateTime.Now;
			DateTime endInterval = DateTime.Now.AddMinutes(interval);
			using(dbc = DAL.Instance.GetDb()){
				SqlExpressionVisitor<ScheduleTime> ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<ScheduleTime>();
				ev.Where( st => st.Day == DayOfWeek.Friday);
				var jn = new JoinSqlBuilder<BackupSetSchedule, BackupSet>();
				//string endperiod = DateTime.Now.AddMinutes(interval).Hour+":"+DateTime.Now.AddMinutes(interval).Minute;
				
				// 1 - gather what to schedule for the rest of the current hour
				
				jn = jn.LeftJoin<BackupSet, ScheduleTime>(x => x.Id, y => y.BackupSetId)
					.Where<BackupSet>(x => x.Enabled &&  !x.IsTemplate)
						.And<ScheduleTime>(y => y.Day == now.DayOfWeek 
					                   && y.BeginHour == now.Hour
					                   && y.BeginMinute >= now.Minute
					                   /*y.Day == endInterval.DayOfWeek*/);

				// 2 -remaining hours of the current day 
				if(endInterval.DayOfWeek == now.DayOfWeek && endInterval.Hour > now.Hour+1){
					jn.Or<BackupSet>(x => x.Enabled &&  !x.IsTemplate)
						.And<ScheduleTime>(s => s.Day == now.DayOfWeek 
				                          && s.BeginHour > now.Hour
				                          && s.BeginHour < endInterval.Hour);
					
					/*jn = jn.Or<ScheduleTime>(s => s.BeginHour == endInterval.Hour
					                         && s.BeginMinute < endInterval.Minute);*/
				}
				else if(endInterval.DayOfWeek != now.DayOfWeek){
					jn.Or<BackupSet>(x => x.Enabled &&  !x.IsTemplate)
						.And<ScheduleTime>(s => s.Day == now.DayOfWeek 
					                          && s.BeginHour > now.Hour
					                          && s.BeginHour <= 23);
				}
				
				
				// 3- loop and add every complete (24h) day until (interval_end_day -1), if any.
				int nbOfDays = endInterval.Subtract(now).Days;
				for(int i=1; i<= nbOfDays; i++){
					jn.Or<BackupSet>(x => x.Enabled &&  !x.IsTemplate)
						.And<ScheduleTime>(s => s.Day == now.AddDays(i).DayOfWeek);
				}
				
				
				
				if(endInterval.DayOfWeek != now.DayOfWeek){
					// remaining hours of the end interval day
					jn.Or<BackupSet>(x => x.Enabled &&  !x.IsTemplate)
						.And<ScheduleTime>(y => y.Day == endInterval.DayOfWeek
					                         && y.BeginHour < endInterval.Hour
					                         //&& y.BeginMinute < endInterval.Minute
					                         );
					// remaining minutes
					jn.Or<BackupSet>(x => x.Enabled &&  !x.IsTemplate)
						.And<ScheduleTime>(y => y.Day == endInterval.DayOfWeek
					                         && y.BeginHour == endInterval.Hour
					                         && y.BeginMinute < endInterval.Minute
					                         );
				}
				
				jn.SelectAll<BackupSetSchedule>();
				Console.WriteLine("next to schedule SQL = "+jn.ToSql());
				return dbc.Query<BackupSetSchedule>(jn.ToSql());
			}
		}

		public List<BackupSet> GetAll(int start, int limit, string orderbyField, string sortDirection, bool templates){
			SqlExpressionVisitor<BackupSet> ev = OrmLiteConfig.DialectProvider.ExpressionVisitor<BackupSet>();
			//ev.OrderBy(rn=> new{ at=Sql.Desc(rn.Rate), rn.Name });
			ev.Where( bs => bs.IsTemplate == templates && bs.Enabled);
			if(!string.IsNullOrEmpty(orderbyField) && sortDirection.ToUpper() == "ASC")
				ev.OrderBy(orderbyField);
				
			//else
			//	ev.OrderByDescending(orderbyField);
			ev.Limit(start, limit);
			using(dbc = DAL.Instance.GetDb())
				return dbc.Select<BackupSet>( ev);
			//return DAL.GetDb().Select<People>( e =
		}
		
		public List<BackupSet> GetAll(int start, int limit){
			return GetAll(start, limit, "Id", "ASC", false);
		}
	}
}

