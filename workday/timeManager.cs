using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Globalization;

namespace workday
{
    public class timeManager
    {
        private static string fname, lname, _username;
        private static int userId;
        private static double hours;
        private static List<string> workdayFields = new List<string>();
        private static List<int> recordId = new List<int>();
        private static List<DateTime> date = new List<DateTime>();
        private static List<DateTime> start = new List<DateTime>();
        private static List<DateTime> finish = new List<DateTime>();
        private static Boolean settled;

        public timeManager()
        {
            workdayFields.Add("Record Id");
            workdayFields.Add("User");
            workdayFields.Add("Date");
            workdayFields.Add("Start Time");
            workdayFields.Add("Finish Time");
            workdayFields.Add("Settled");
        }

        private void updateUserOvertime(DateTime start, DateTime finish)
        {
            database db = new database();
            TimeSpan currentOvertime = TimeSpan.Parse("00:00:00");

            do
            {
                currentOvertime = db.readOvertime(userId);
            } while (currentOvertime == TimeSpan.MinValue);
            
            double daily = hours / 5;
            TimeSpan duration = finish - start;
            
            // TODO : Add a lunch time functionality for this to be dynamic
            duration = duration - TimeSpan.Parse("00:30:00");
            TimeSpan dailyTime = TimeSpan.FromHours(daily);

            TimeSpan overtime = duration - dailyTime;
            overtime = overtime + currentOvertime;
            db.updateOvertime(overtime.ToString(), userId);
        }

        // Returns true if there are unsettled workdays
        // Returns false if there are unsettled workdays
        private Boolean checkUnsettled()
        {
            foreach (var date in finish)
            {
                if (date.Equals(DateTime.MinValue))
                {
                    return true;
                }
            }

            return false;
        }

        private void updateUnsettled()
        {
            Console.Write("Please enter the Id of the record to update: ");
            int updateId = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
            updateId--;
            Console.Write("Please enter the time which your day ended at\n" +
                          "(format hh:mm): ");
            string tempTime = Console.ReadLine();
            finish[updateId] = Convert.ToDateTime(date[updateId].ToString().Substring(0, 11) + tempTime);
            database db = new database();
            db.updateUnsettled(finish[updateId], recordId[updateId]);
        }

        public void newWorkday()
        {
            listClear();
            // Before proceeding the user should settle unsettled workdays
            while (displayUnsettled())
            {
                Console.WriteLine("You have {0} unsettled workdays to settle before proceeding", recordId.Count);
                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
                Program.clearScreen();
                listClear();
                displayUnsettled();
                updateUnsettled();
                listClear();
            }

            listClear();

            DateTime date = getDate();
            DateTime startTime = getTime("start", date);
            DateTime endTime = getTime("finish", date);

            database db = new database();
            db.newWorkday(userId, date, startTime, endTime);

            if (endTime != DateTime.MinValue)
            {
                updateUserOvertime(startTime, endTime);
            }
        }

        // TODO : Make this non static
        // TODO : Make this void a DateTime
        public DateTime getDate()
        {
            string date = string.Empty;
            DateTime returnDate = default;
            do
            {
                Console.Write("\nPlease enter the date for this record (dd/mm/yyyy):" +
                              "\nOR type \"Today\" for the date today: ");
                date = Console.ReadLine();
                if (date.ToLower() == "today")
                {
                    date = DateTime.Now.ToString();
                    date = date.Substring(0, 10);
                    date = date + " 00:00:00";
                    return Convert.ToDateTime(date);
                }
                else if (date == string.Empty)
                {
                    Program.clearScreen();
                    Console.WriteLine("Please do not leave this empty" +
                                      "\nPress any key to continue");
                    date = String.Empty;
                    Console.ReadKey();
                }
                else
                {
                    try
                    {
                        returnDate = DateTime.Parse(date);
                        return returnDate;
                    }
                    catch
                    {
                        Console.WriteLine("Please enter the date in the correct format" +
                                          "\nPress any key to continue");
                        date = String.Empty;
                    }
                }
            } while (date == string.Empty);

            return returnDate;
        }

        private DateTime getTime(string TOD, DateTime date)
        {
            String holder;
            DateTime time;
            Boolean replay;
            do
            {
                replay = false;
                Console.WriteLine("\nPlease enter your {0} time in the format hh:mm", TOD);
                holder = Console.ReadLine();
                if (!DateTime.TryParse(holder, out time) && TOD != "finish")
                {
                    replay = true;
                    Console.Write("\nPlease enter in a valid time in the correct format" +
                                  "\nPress any key to continue\n");
                    Console.ReadKey();
                    Program.clearScreen();
                }
                else if (DateTime.Parse(holder) == DateTime.MinValue && TOD == "finish")
                {
                    return DateTime.MinValue;
                }
            } while (replay);

            string[] elements = time.ToString().Split(' ');
            string built = date.ToShortDateString() + " " + elements[1];
            time = Convert.ToDateTime(built);

            return time;
        }

        // Returns true if there re unsettled workdays
        // Returns false if there are NO unsettled workdays
        private Boolean displayUnsettled()
        {
            SQLiteCommand command =
                new SQLiteCommand(
                    "SELECT * FROM Workdays WHERE (User = (SELECT Id FROM Users WHERE (Username = @userlocal AND Settled = 0)))--");
            command.Parameters.AddWithValue("@userlocal", _username);
            database.readWorkdays(command);

            if (!checkUnsettled())
            {
                return false;
            }

            Console.WriteLine("Here are the unsettled workdays: \n");
            Console.WriteLine("{0,-20} {1,5}", "Field", "Data\n");
            for (int i = 0; i < recordId.Count; i++)
            {
                Console.WriteLine("{0,-20}{1,5}", "Id", i + 1);
                Console.WriteLine("{0,-20}{1,5}", workdayFields[0], recordId[i]);
                Console.WriteLine("{0,-20}{1,5}", workdayFields[1], _username);
                Console.WriteLine("{0,-20}{1,5}", workdayFields[2], date[i].ToLongDateString());
                Console.WriteLine("{0,-20}{1,5}", workdayFields[3], start[i].ToLongTimeString());
                if (finish[i] == DateTime.MinValue)
                {
                    Console.WriteLine("{0,-20}{1,5}", workdayFields[4], "Null");
                }
                else
                {
                    Console.WriteLine("{0,-20}{1,5}", workdayFields[4], finish[i].ToLongTimeString());
                }

                Console.WriteLine("{0,-20}{1,5}", workdayFields[5], settled);
                Console.WriteLine("\n----\n");
            }

            return true;
        }

        public static void listClear()
        {
            recordId.Clear();
            date.Clear();
            start.Clear();
            finish.Clear();
        }

        public static void setSettled(Boolean data)
        {
            settled = data;
        }

        public static void setFinish(DateTime data)
        {
            finish.Add(data);
        }

        public static void setStart(DateTime data)
        {
            start.Add(data);
        }

        public static void setDate(DateTime data)
        {
            date.Add(data);
        }

        public static void setId(int data)
        {
            recordId.Add(data);
        }

        public static void setUsername(string data)
        {
            _username = data;
        }

        public static void setFname(string data)
        {
            fname = data;
        }

        public static void setLname(string data)
        {
            lname = data;
        }

        public static void setHours(double data)
        {
            hours = data;
        }

        public static void setUserId(int data)
        {
            userId = data;
        }
    }
}