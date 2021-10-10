using System;
using System.Data.SQLite;
using System.IO;

namespace workday
{
    public class database
    {
        public database()
        {
            runUpgrades();
        }

        public TimeSpan readOvertime(int userId)
        {
            using (SQLiteConnection connection =
                new SQLiteConnection("Data Source=workday.db;Version=3;New=True;Compress=True;"))
            {
                SQLiteCommand cmd =
                    new SQLiteCommand("SELECT Overtime FROM Users WHERE (Id = @userId);--",
                        connection);
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    try
                    {
                        cmd.Connection = connection;
                        connection.Open();
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TimeSpan ot = TimeSpan.Parse(reader["Overtime"].ToString());
                                return ot;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // ignored
                        if (!e.Message.Contains("already exists"))
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }

            return TimeSpan.Parse("00:00:00");
        }

        public void updateOvertime(string overtime, int userId)
        {
            SQLiteCommand command =
                new SQLiteCommand(
                    "UPDATE Users SET Overtime = @ot where (Id = @userId);--");
            command.Parameters.AddWithValue("@ot", overtime);
            command.Parameters.AddWithValue("@userId", userId);
            runSQL(command);
        }

        public void newWorkday(int user, DateTime date, DateTime start, DateTime finish)
        {
            SQLiteCommand insert = new SQLiteCommand();
            if (finish != DateTime.MinValue)
            {
                insert.CommandText =
                    "INSERT INTO Workdays(User, Date, Start_Time, Finish_Time, Settled) VALUES(@user, @date, @start, @finish, 1);--";
                insert.Parameters.AddWithValue("@finish", finish);
            }
            else
            {
                insert.CommandText =
                    "INSERT INTO Workdays(User, Date, Start_Time, Settled) VALUES(@user, @date, @start, 0);--";
            }

            insert.Parameters.AddWithValue("@user", user);
            insert.Parameters.AddWithValue("@date", date);
            insert.Parameters.AddWithValue("@start", start);
            runSQL(insert);
        }

        public void updateUnsettled(DateTime finish, int recordId)
        {
            SQLiteCommand command =
                new SQLiteCommand(
                    "UPDATE Workdays SET Finish_Time = @fT, Settled = 1 where (Id = @recordId);--");
            command.Parameters.AddWithValue("@fT", finish);
            command.Parameters.AddWithValue("@recordId", recordId);
            runSQL(command);
        }

        private void runUpgrades()
        {
            string[] directories = Directory.GetDirectories("../../Upgrades");
            foreach (string subdir in directories)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(subdir);
                FileInfo[] files = dirInfo.GetFiles("*.sql");
                SQLiteCommand sql = new SQLiteCommand(File.ReadAllText(files[0].FullName));
                runSQL(sql);
            }
        }

        public void createUser()
        {
            Console.WriteLine("\nPlease enter your username:");
            string username = Console.ReadLine();

            while (confirmUser(username))
            {
                Console.Write("\nThat username is already taken, please select another" +
                              "\nPress any key to continue\n");
                Console.ReadKey();
                Console.Clear();
            }

            timeManager.setUsername(username);

            Console.WriteLine("\nPlease enter your password:");
            string password = Console.ReadLine();

            Console.WriteLine("\nPlease enter your first name:");
            string fname = Console.ReadLine();
            timeManager.setFname(fname);

            Console.WriteLine("\nPlease enter your surname:");
            String lname = Console.ReadLine();
            timeManager.setLname(lname);

            Console.WriteLine("\nPlease enter your weekly hours:");
            double hours = Double.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
            timeManager.setHours(hours);

            SQLiteCommand insert = new SQLiteCommand(
                "INSERT INTO Users(Username, Password, First_Name, Last_Name, Week_Hours) VALUES(@username, @password, @fname, @lname, @hours);--");
            insert.Parameters.AddWithValue("@username", username);
            insert.Parameters.AddWithValue("@password", password);
            insert.Parameters.AddWithValue("@fname", fname);
            insert.Parameters.AddWithValue("@lname", lname);
            insert.Parameters.AddWithValue("@hours", hours);
            runSQL(insert);
        }

        // Returns true if username already exists
        // Returns false if username is free 
        public Boolean confirmUser(string username)
        {
            using (SQLiteConnection connection =
                new SQLiteConnection("Data Source=workday.db;Version=3;New=True;Compress=True;"))
            {
                SQLiteCommand cmd =
                    new SQLiteCommand("SELECT * FROM Users WHERE (username = @username);--",
                        connection);
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Connection = connection;
                    connection.Open();
                    int reader = Convert.ToInt32(cmd.ExecuteScalar());
                    if (reader > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public Boolean login(string username, string password)
        {
            if (!confirmUser(username))
            {
                Console.WriteLine("User {0} does not exist\n", username);
                Console.ReadKey();
                return false;
            }

            using (SQLiteConnection connection =
                new SQLiteConnection("Data Source=workday.db;Version=3;New=True;Compress=True;"))
            {
                SQLiteCommand cmd =
                    new SQLiteCommand("SELECT * FROM Users WHERE (username = @username AND password = @pwd);--",
                        connection);
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@pwd", password);
                    try
                    {
                        cmd.Connection = connection;
                        connection.Open();
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (username == reader["Username"].ToString() &&
                                    password == reader["Password"].ToString())
                                {
                                    timeManager.setUserId(int.Parse(reader["Id"].ToString()));
                                    timeManager.setUsername(reader["Username"].ToString());
                                    timeManager.setFname(reader["First_Name"].ToString());
                                    timeManager.setLname(reader["Last_Name"].ToString());
                                    timeManager.setHours(double.Parse(reader["Week_Hours"].ToString()));
                                    return true;
                                }

                                return false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // ignored
                        if (!e.Message.Contains("already exists"))
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }

            return false;
        }

        private void runSQL(SQLiteCommand cmd)
        {
            using (SQLiteConnection connection =
                new SQLiteConnection("Data Source=workday.db;Version=3;New=True;Compress=True;"))
            {
                try
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    // ignored
                    if (!e.Message.Contains("already exists"))
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        public static void readWorkdays(SQLiteCommand cmd)
        {
            using (SQLiteConnection connection =
                new SQLiteConnection("Data Source=workday.db;Version=3;New=True;Compress=True;"))
            {
                try
                {
                    cmd.Connection = connection;
                    connection.Open();
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            timeManager.setId(int.Parse(reader["Id"].ToString()));
                            timeManager.setDate(Convert.ToDateTime(reader["Date"]));
                            timeManager.setStart(Convert.ToDateTime(reader["Start_Time"]));
                            try
                            {
                                timeManager.setFinish(Convert.ToDateTime(reader["Finish_Time"]));
                            }
                            catch
                            {
                                timeManager.setFinish((DateTime) DateTime.MinValue);
                            }

                            timeManager.setSettled((bool) reader["Settled"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    // ignored
                    if (!e.Message.Contains("already exists"))
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}