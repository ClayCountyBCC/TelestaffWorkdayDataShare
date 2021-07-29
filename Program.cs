using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;

namespace TelestaffWorkdayDataShare
{
  class Program
  {

    private const string File_Save_Path = @"\\ftp.claycountygov.com\workday\OUTGOING\";

    static void Main()
    {
      string Remote_Login = ConfigurationManager.ConnectionStrings["Remote_Login"].ConnectionString;
      string Remote_Password = ConfigurationManager.ConnectionStrings["Remote_Password"].ConnectionString;

      using (NetworkShareAccesser.Access("ftp.claycountygov.com", "", Remote_Login, Remote_Password))
      {
        try
        {

          DateTime current = DateTime.Today.AddDays(-1);

          string current_staffing_filename = File_Save_Path + GetFileName("staffing", current);
          string current_changes_filename = File_Save_Path + GetFileName("changes", current);

          if (!File.Exists(current_staffing_filename))
          {
            var staffingdata = StaffingData.Get(current);
            var staffingtext = StaffingData.ToString(staffingdata);
            File.WriteAllText(current_staffing_filename, staffingtext);
          }

          if (!File.Exists(current_changes_filename))
          {
            var changedata = ChangeData.Get(current);
            var changetext = ChangeData.ToString(changedata);
            File.WriteAllText(current_changes_filename, changetext);
          }


        }
        catch (Exception ex)
        {
          new ErrorLog(ex);
        }
      }
    }


    public static string GetCS(string cs)
    {
      return ConfigurationManager.ConnectionStrings[cs.ToString()].ConnectionString;
    }

    public static string GetFileName(string filetype, DateTime workdate)
    {
      return "Telestaff_" + (filetype == "staffing" ? "Daily_Load" : "Changes") + "_" + workdate.ToString("yyyyMMdd") + ".txt";
    }

  }
}
