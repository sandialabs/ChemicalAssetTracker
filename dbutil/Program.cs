using Common;
using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dbutil
{
    class Program
    {
        private static CmdLine s_command_line;

        static void Main(string[] args)
        {
            s_command_line = new CmdLine(args);

            if (s_command_line.ArgCount == 0 || s_command_line.HasFlag("-help"))
            {
                ShowUsage();
                return;
            }

            if (s_command_line.HasFlag("-create")) CreateDatabase();
            //if (s_command_line.HasFlag("-validate")) ValidateDatabase();

            if (!s_command_line.HasFlag("-noprompt"))
            {
                Console.Write("Press Return to exit ...");
                Console.ReadLine();
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Use: dbutil [-help] [-create] [-noprompt]");
        }

        static void CreateDatabase()
        {
            string institution = s_command_line.GetArg("-institution");
            string hostname = s_command_line.GetArg("-hostname", "localhost");
            string username = s_command_line.GetArg("-cmsuser", "cms");
            string password = s_command_line.GetArg("-cmspswd", "cms");
            ConnectionSettings settings = new ConnectionSettings
            {
                Hostname = hostname,
                Username = username,
                Password = password
            };
            CMSDB.ConnectionSettings = settings;
            Console.WriteLine($"Opening database on {hostname} as {username} ...");
            string site = s_command_line.GetArg("-site");
            //if (institution == null) institution = "Ministry";

            using (CMSDB db = new CMSDB())
            {
                // KLUDGE: catch exceptions during EnsureCreated.
                // This method attempts to create the __EFMigrationHistory table but fails because it
                // uses varchar(767) for the MigrationID, which is too big for mariadb
                TaskTimer t1 = new TaskTimer("timer");
                try
                {
                    db.Database.EnsureCreated();
                }
                catch(Exception ex)
                {
                    while (ex.InnerException != null) ex = ex.InnerException;
                    Console.WriteLine(ex.Message);
                }
                if (institution != null)
                {
                    Console.WriteLine($"Creating root location type and top-level location ({institution})");
                    LocationType lt = new LocationType
                    {
                        Name = institution,
                        ValidChildren = "",
                    };
                    db.LocationTypes.Add(lt);
                    db.SaveChanges();
                    db.AddLocation(institution, 0, lt.LocationTypeID, true);
                    db.UpdateMissingLocationPaths();
                    //db.StoreSetting(CMSDB.InstitutionKey, institution);
                }
                if (site != null) db.StoreSetting(CMSDB.CurrentSiteKey, site);
                if (site != null  &&  institution != null) db.FindOrAddLocation($"{institution}/{site}");
                db.SaveChanges();
                Console.WriteLine($"Database tables created: {t1.ElapsedSeconds} seconds");
                t1.Reset();
                db.EnsureSeeded();
                Console.WriteLine($"Database seeded: {t1.ElapsedSeconds} seconds");
            }
        }

        static void CreateInventoryItem(string barcode, string chemical_name, string owner, string casnumber, string location, string group, string state, double container_size, string units, double remaining, CMSDB db)
        {
            InventoryItem item = new InventoryItem()
            {
                Barcode = barcode,
                CASNumber = casnumber,
                ChemicalName = chemical_name,
                State = state,
                ContainerSize = container_size,
                Units = "mL",
                RemainingQuantity = remaining
            };
            if (location != null)
            {
                string locstr = db.NormalizeLocation(location);
                StorageLocation loc = db.FindOrAddLocation(locstr);
                item.LocationID = loc.LocationID;
            }
            if (group != null) item.Group = db.StorageGroups.FirstOrDefault(x => x.Name == group);
            if (owner != null) item.Owner = db.Owners.FirstOrDefault(x => x.Name == owner);
            db.InventoryItems.Add(item);
            db.SaveChanges();
        }


        static void RunSQLCmd(string description, string sql)
        {
            Console.WriteLine("\n" + description);
            string cmd = $"mysql -u root -proot CMS -e \"{sql}\"";
            var proc = System.Diagnostics.Process.Start("cmd", "/c " + cmd);
            proc.WaitForExit();
        }

        static void Dump(CMSDB db, string what)
        {
            what = what.ToLower();
            if (what == "owners" || what == "all") RunSQLCmd("Owners", "select * from Owners");
            if (what == "locations" || what == "all") RunSQLCmd("Locations", "select * from StorageLocations");
            if (what == "groups" || what == "all") RunSQLCmd("Groups", "select * from StorageGroups");
            if (what == "inventory" || what == "all") RunSQLCmd("Inventory", "select * from InventoryItems");
        }
    }
}
