CREATE TABLE Users
(
    Id         INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Username   VARCHAR(50),
    Password   VARCHAR(250),
    First_Name VARCHAR(25),
    Last_Name  VARCHAR(40),
    Week_Hours DOUBLE(5),
    Overtime text
);--