CREATE TABLE Workdays
(
    Id          INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    User        INTEGER NOT NULL,
    Date        text NOT NULL,
    Start_Time  text NOT NULL,
    Finish_Time text,
    Settled     BIT NOT NULL
);--