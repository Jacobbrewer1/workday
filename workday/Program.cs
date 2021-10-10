using System;
using System.Threading;

namespace workday
{
    internal class Program
    {
        private static DateTime loginTime;
        private static string user;

        public static void Main(string[] args)
        {
            Console.Title = "Workday Tracker Program";
            Program local = new Program();
            database db = new database();
            timeManager tm = new timeManager();
            do
            {
            } while (!local.login(db));
            
            tm.newWorkday();
        }

        private Boolean login(database db)
        {
            string username = null, password = string.Empty;

            Console.Write("\nTo create a new user type \"Create User\"" +
                          "\nTo exit the program, pleast type \"Exit1\"\n");
            Console.Write("\nPlease enter your username: ");
            username = Console.ReadLine();
            switch (username)
            {
                case "Create User":
                    db.createUser();
                    user = username;
                    loginTime = DateTime.Now;
                    clearScreen();
                    return true;
                case "Exit1":
                    Environment.Exit(0);
                    break;
            }

            Console.Write("\nPlease enter the password to {0}: ", username);
            ConsoleKeyInfo info = Console.ReadKey(true);

            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)

                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }

                info = Console.ReadKey(true);
            }

            // Breaking from the previous line
            Console.WriteLine();

            if (db.login(username, password))
            {
                user = username;
                loginTime = DateTime.Now;
                clearScreen();
                return true;
            }

            return false;
        }

        public static void clearScreen()
        {
            Console.Clear();
            string logginText = "\nUser: " + user + " - Logged in since: " + loginTime + "\n";
            Console.Write("\n{0, " + ((Console.WindowWidth / 2) + (logginText.Length / 2)) + "}", logginText);
        }
    }
}