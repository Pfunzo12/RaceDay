/* OPTIONAL: give the 4 existing users a real, working password.
   Your Users table currently holds placeholder text ("hashed_pw_..."), which the login cannot verify.
   This changes ONLY the password_hash column of these 4 rows. New password for all four:  Password123!
   Skip this file if you prefer to register fresh accounts through the app instead. */
USE RaceDayDB;
GO
UPDATE Users SET password_hash = 'AQAAAAEAAYagAAAAEIj3YdhUNWn/to3B38+51NWlcBtLaM6ckBxE1ZabvnJNUzz7MjPrBTOvAQ7NJ8TsjA==' WHERE email = 'thandiwe.nkosi@raceday.co.za';
UPDATE Users SET password_hash = 'AQAAAAEAAYagAAAAEGoAgEH1o8E/SUNmrgGZPDCyNCJiig4LoplNTtcwgJpqtoDvdeuFavgdhlBNEjA2Rg==' WHERE email = 'pieter.vanwyk@raceday.co.za';
UPDATE Users SET password_hash = 'AQAAAAEAAYagAAAAEMlHA+CJNARuiJk2SnLzbt38N4WxMHG8fgTEZTBaYhysDMDDl9BAWnenlQin9kZibg==' WHERE email = 'lindiwe.dube@example.com';
UPDATE Users SET password_hash = 'AQAAAAEAAYagAAAAEP/P35b66kyq4mw2HH/+fIZlH4uYIraFadXQhLpqLsRI1Lpgo1lc/e+3gpoI7Mod+g==' WHERE email = 'sipho.mahlangu@example.com';
GO
SELECT user_id, email, role, LEN(password_hash) AS hash_length FROM Users;
