using AMSGetFlights.Model;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace AMSGetFlights.Services
{
    public class StoredFlight
    {
        public string? XML { get; set; }
        public string? Lastupdate { get; set; }
    }
    public class SqLiteFlightRepository : IFlightRepositoryDataAccessObject
    {
        private static string? dbLocation;
        private static string? dbfileName;
        private readonly EventExchange eventExchange;

        public SqLiteFlightRepository(GetFlightsConfigService configService, EventExchange eventExchange)
        {
            dbLocation = configService.config.StorageDirectory;
            dbfileName = "AmsGetFlights.sqlite";
            File.Delete(Path.Combine(dbLocation, dbfileName));
            this.eventExchange = eventExchange;
        }

        public static string DbFile
        {
            get { return $"{dbLocation}\\{dbfileName}"; }
        }

        public void DeleteDBFile()
        {
            try
            {
                File.Delete(DbFile);
            }
            catch { }
        }

        private static SqliteConnection SimpleDbConnection()
        {

            if (!File.Exists(DbFile))
            {
                SqliteConnection sqliteConnection = new("Data Source=" + DbFile);
                sqliteConnection.Open();
                sqliteConnection.Execute(
                    @"create table StoredFlights(
                    flightID                  TEXT PRIMARY KEY,
                    XML                       TEXT,
                    callsign                  TEXT,
                    al                        TEXT,
                    apt                       TEXT,
                    fltNum                    TEXT,
                    type                      TEXT,
                    sdo                       TEXT,
                    sto                       TEXT,
                    lastupdate                TEXT
                )");
                sqliteConnection.Execute(
                    @"create table Subcriptions(
                    subscription                  TEXT PRIMARY KEY
                )");
                sqliteConnection.Close();
                System.GC.Collect();
            }

            return new SqliteConnection("Data Source=" + DbFile);
        }

        public int GetNumEntries()
        {
            string? sql = $"SELECT count(*) from StoredFlights";
            using var cnn = SimpleDbConnection();
            cnn.Open();
            try
            {
                return cnn.QueryFirst<int>(sql);
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                cnn.Close();
                System.GC.Collect();
            }
        }

        public IEnumerable<StoredFlight> GetStoredFlights(GetFlightQueryObject query, string? kind)
        {

            string sql = $"SELECT * from StoredFlights WHERE datetime(sto) >= datetime('{query.startQuery}') AND datetime(sto) <= datetime('{query.endQuery}') ";
            if (query.schedDate != null)
            {
                sql = $"SELECT * from StoredFlights WHERE sdo = '{query.schedDate}' ";
            }
            if (query.al != null)
            {
                sql += $" AND al = '{query.al}'";
            }
            if (query.apt != null)
            {
                sql += $" AND apt = '{query.apt}'";
            }
            if (query.flt != null)
            {
                sql += $" AND fltNum = '{query.flt}'";
            }
            if (query.callsign != null)
            {
                sql += $" AND callsign = '{query.callsign}'";
            }
            if (kind != null)
            {
                sql += $" AND type = '{kind}'";
            }

            sql += " ORDER BY sto ";

            using var cnn = SimpleDbConnection();
            try
            {
                cnn.Open();
                return cnn.Query<StoredFlight>(sql);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                cnn.Close();
                System.GC.Collect();
            }
        }

        public void DeleteRecord(AMSFlight record)
        {
            string? sql = $"DELETE FROM StoredFlights WHERE flightID = '{record.flightId.flightUniqueID}'";
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                cnn.Execute(sql, record);
                cnn.Close();
            }
            GC.Collect();
        }
        public void Prune(int backWindow)
        {

            DateTime pruneDate = DateTime.Now.AddDays(backWindow);
            string sql = $"DELETE FROM StoredFlights WHERE sto < datetime('{pruneDate}','localtime')";
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                cnn.Execute(sql);
                cnn.Close();
            }
            GC.Collect();
        }

        public void Indate(List<AMSFlight> fls)
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                SqliteCommand sqlComm;
                sqlComm = new SqliteCommand("begin", cnn);
                sqlComm.ExecuteNonQuery();


                foreach (AMSFlight record in fls)
                {
                    string sql = $"INSERT INTO StoredFlights (flightID, callsign, XML,al,apt,fltNum,type, sdo,sto, lastupdate) VALUES ('{record.flightId.flightID}','{record.callsign}', '{record.XmlRaw}','{record.flightId.iataAirline}','{record.flightId.iatalocalairport}','{record.flightId.flightNumber}','{record.flightId.flightkind}','{record.flightId.scheduleDate}', '{record.flightId.scheduleTime}',datetime('now'))" +
                        $"ON CONFLICT(flightID) DO UPDATE SET XML = '{record.XmlRaw}', callsign = '{record.callsign}', sto = '{record.flightId.scheduleTime}', sdo = '{record.flightId.scheduleDate}', lastupdate = datetime('now')";
                    cnn.Execute(sql);
                }


                sqlComm = new SqliteCommand("end", cnn);
                sqlComm.ExecuteNonQuery();
                cnn.Close();

                eventExchange.MonitorMessage($"Bulk Update of {fls.Count} flights");
            }

            GC.Collect();
        }
        public void Upsert(List<AMSFlight> fls)
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                SqliteCommand sqlComm;
                sqlComm = new SqliteCommand("begin", cnn);
                sqlComm.ExecuteNonQuery();


                foreach (AMSFlight record in fls)
                {
                    string sql = $"INSERT INTO StoredFlights (flightID, callsign, XML,al,apt,fltNum,type, sdo,sto, lastupdate) VALUES ('{record.flightId.flightID}','{record.callsign}', '{record.XmlRaw}','{record.flightId.iataAirline}','{record.flightId.iatalocalairport}','{record.flightId.flightNumber}','{record.flightId.flightkind}','{record.flightId.scheduleDate}', '{record.flightId.scheduleTime}',datetime('now'))" +
                        $"ON CONFLICT(flightID) DO UPDATE SET XML = '{record.XmlRaw}', callsign = '{record.callsign}', sto = '{record.flightId.scheduleTime}', sdo = '{record.flightId.scheduleDate}', lastupdate = datetime('now')";
                    cnn.Execute(sql);
                }


                sqlComm = new SqliteCommand("end", cnn);
                sqlComm.ExecuteNonQuery();
                cnn.Close();

                eventExchange.MonitorMessage($"Bulk Update of {fls.Count} flights");
            }

            GC.Collect();
        }


        public IEnumerable<string> GetAllSubscriptions()
        {
            using var cnn = SimpleDbConnection();
            try
            {
                cnn.Open();
                return cnn.Query<string>("SELECT subscription from Subcriptions ");
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                cnn.Close();
                System.GC.Collect();
            }
        }

        public void SaveSubsciptions(List<string> subscriptions)
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                SqliteCommand sqlComm;
                sqlComm = new SqliteCommand("begin", cnn);
                sqlComm.ExecuteNonQuery();

                cnn.Execute("DELETE FROM Subscriptions");

                foreach (string s in subscriptions)
                {
                    string sql = $"INSERT INTO Subscriptions (subscription) VALUES ('{s}')";
                    cnn.Execute(sql);
                }


                sqlComm = new SqliteCommand("end", cnn);
                sqlComm.ExecuteNonQuery();
                cnn.Close();
            }

            GC.Collect();
        }

        public void ClearFlights()
        {
            using (var cnn = SimpleDbConnection())
            {
                cnn.Open();
                SqliteCommand sqlComm;
                sqlComm = new SqliteCommand("begin", cnn);
                sqlComm.ExecuteNonQuery();


                string sql = $"DELETE from StoredFlights";
                cnn.Execute(sql);



                sqlComm = new SqliteCommand("end", cnn);
                sqlComm.ExecuteNonQuery();
                cnn.Close();
            }

            GC.Collect();
        }
    }
    public class MSSQLFlightRepository : IFlightRepositoryDataAccessObject
    {
        private string? ConnectionString { get; set; }

        private readonly EventExchange eventExchange;

        public MSSQLFlightRepository(GetFlightsConfigService configService, EventExchange eventExchange)
        {
            ConnectionString = configService.config.SQLConnectionString;
            this.eventExchange = eventExchange;
        }
        private IDbConnection SimpleDbConnection()
        {
            //create table StoredFlights(
            //                    flightID                  varchar(255) NOT NULL PRIMARY KEY,
            //                    XML                       TEXT,
            //                    callsign                  varchar(16),
            //                    al                        varchar(8),
            //                    apt                       varchar(10),
            //                    fltNum                    varchar(8),
            //                    type                      varchar(10),
            //                    sdo                       date,
            //                    sto                       datetime,
            //                    lastupdate                datetime
            //                );
            //create table Subscriptions(
            //                    subscription              TEXT            
            //                );
            return new System.Data.SqlClient.SqlConnection(ConnectionString);
        }

        public void DeleteRecord(AMSFlight record)
        {
            string sql = $"DELETE FROM StoredFlights WHERE flightID = '{record.flightId.flightUniqueID}'";
            using var cnn = SimpleDbConnection();
            cnn.Open();
            cnn.Execute(sql, record);
            cnn.Close();
        }
        public int GetNumEntries()
        {
            string sql = $"SELECT count(*) from StoredFlights";
            using var cnn = SimpleDbConnection();
            cnn.Open();
            try
            {
                return cnn.QueryFirst<int>(sql);
            }
            catch (Exception)
            {
                return 0;
            }
            finally { cnn.Close(); }
        }
        public IEnumerable<StoredFlight> GetStoredFlights(GetFlightQueryObject query, string? kind)
        {
            string sql = $"SELECT * from StoredFlights WHERE sto >= '{query.startQuery}' AND sto <= '{query.endQuery}' ";
            if (query.schedDate != null)
            {
                sql = $"SELECT * from StoredFlights WHERE sdo = '{query.schedDate}' ";
            }
            if (query.al != null)
            {
                sql += $" AND al = '{query.al}'";
            }
            if (query.apt != null)
            {
                sql += $" AND apt = '{query.apt}'";
            }
            if (query.flt != null)
            {
                sql += $" AND fltNum = '{query.flt}'";
            }
            if (query.callsign != null)
            {
                sql += $" AND callsign = '{query.callsign}'";
            }
            if (kind != null)
            {
                sql += $" AND type = '{kind}'";
            }

            sql += " ORDER BY sto ";

            using var cnn = SimpleDbConnection();
            try
            {
                cnn.Open();
                return cnn.Query<StoredFlight>(sql);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                cnn.Close();
            }
        }
        public void Prune(int backWindow)
        {
            DateTime pruneDate = DateTime.Now.AddDays(backWindow);
            string sql = $"DELETE FROM StoredFlights WHERE sto <'{pruneDate}' OR lastupdate IS NULL";
            using var cnn = SimpleDbConnection();
            cnn.Open();
            cnn.Execute(sql);
            cnn.Close();
        }
        public void Indate(List<AMSFlight> fls)
        {
            using var cnn = SimpleDbConnection();
            cnn.Open();
            string sql;
            try
            {
                foreach (AMSFlight record in fls)
                {
                    sql = $" INSERT INTO StoredFlights (flightID, callsign, XML,al,apt,fltNum,type, sdo,sto,lastupdate) VALUES ('{record.Key}','{record.callsign}', '{record.XmlRaw}','{record.flightId.iataAirline}','{record.flightId.iatalocalairport}','{record.flightId.flightNumber}','{record.flightId.flightkind}','{record.flightId.scheduleDate}', '{record.flightId.scheduleTime}',GETDATE());";
                    try
                    {
                        cnn.Execute(sql);
                    }
                    catch (System.Data.SqlClient.SqlException ex)
                    {
                        sql = $" UPDATE StoredFlights SET XML = '{record.XmlRaw}', callsign='{record.callsign}', al = '{record.flightId.iataAirline}', apt = '{record.flightId.iatalocalairport}', fltNum = '{record.flightId.flightNumber}', type = '{record.flightId.flightkind}', sdo = '{record.flightId.scheduleDate}', sto ='{record.flightId.scheduleTime}', lastupdate = GETDATE() WHERE flightID = '{record.Key}';";

                        cnn.Execute(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                eventExchange.MonitorMessage(ex.Message);
            }
            cnn.Close();

            eventExchange.MonitorMessage($"Upsert of {fls.Count} flights");
        }
        public void Upsert(List<AMSFlight> fls)
        {
            using var cnn = SimpleDbConnection();
            cnn.Open();
            string sql;
            try
            {
                foreach (AMSFlight record in fls)
                {
                    sql = $" INSERT INTO StoredFlights (flightID, callsign, XML,al,apt,fltNum,type, sdo,sto,lastupdate) VALUES ('{record.Key}','{record.callsign}', '{record.XmlRaw}','{record.flightId.iataAirline}','{record.flightId.iatalocalairport}','{record.flightId.flightNumber}','{record.flightId.flightkind}','{record.flightId.scheduleDate}', '{record.flightId.scheduleTime}',GETDATE());";
                    try
                    {
                        cnn.Execute(sql);
                    }
                    catch (System.Data.SqlClient.SqlException ex)
                    {
                        sql = $" UPDATE StoredFlights SET XML = '{record.XmlRaw}', callsign='{record.callsign}', al = '{record.flightId.iataAirline}', apt = '{record.flightId.iatalocalairport}', fltNum = '{record.flightId.flightNumber}', type = '{record.flightId.flightkind}', sdo = '{record.flightId.scheduleDate}', sto ='{record.flightId.scheduleTime}', lastupdate = GETDATE() WHERE flightID = '{record.Key}';";

                        cnn.Execute(sql);
                    }
                }
            }
            catch (Exception ex)
            {
                eventExchange.MonitorMessage(ex.Message);
            }
            cnn.Close();

            eventExchange.MonitorMessage($"Upsert of {fls.Count} flights");
        }

        public IEnumerable<string> GetAllSubscriptions()
        {
            using var cnn = SimpleDbConnection();
            try
            {
                cnn.Open();
                return cnn.Query<string>("SELECT  subscription  FROM Subscriptions");
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                cnn.Close();
                System.GC.Collect();
            }
        }

        public void SaveSubsciptions(List<string> subscriptions)
        {
            try
            {
                using (var cnn = SimpleDbConnection())
                {
                    cnn.Open();
                    cnn.Execute("DELETE FROM Subscriptions");

                    foreach (string s in subscriptions)
                    {
                        string sql = $"INSERT INTO Subscriptions (subscription) VALUES ('{s}')";
                        cnn.Execute(sql);
                    }
                    cnn.Close();
                }

                GC.Collect();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void ClearFlights()
        {
            using var cnn = SimpleDbConnection();
            try
            {
                cnn.Open();
                cnn.Execute("DELETE from StoredFlights");
            }
            catch (Exception)
            {
                //return null;
            }
            finally
            {
                cnn.Close();
                System.GC.Collect();
            }
        }
    }
}