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

    // Updating process to fit new requirements.
    // Requirements: Emit a file once every two weeks that has all of the data from the staffing table.
    // ie:  the same data that we use in Timestore.
    // We'll use the StaffingData object to model and query this data.
    // In the new process, we'll make this process run every day at 10 AM 
    // (that is when everyone is used to having their time done)
    // and if that day is also the start of a new pay period, we'll create the file.
    // I'll probably just do the new pay period logic in the sql statement
    // so if it returns rows, we'll create the file.
    static void Main()
    {
      DateTime original = DateTime.Parse("9/25/2013");
      DateTime today = DateTime.Today;
      int TotalDays = (int)today.Subtract(original).TotalDays;
      int ModTest = TotalDays % 14;
      DateTime PayPeriodStart = today.AddDays(-ModTest);
      if (today.Date != PayPeriodStart) return;

      string staffing_filename = "";
      string Remote_Login = ConfigurationManager.ConnectionStrings["Remote_Login"].ConnectionString;
      string Remote_Password = ConfigurationManager.ConnectionStrings["Remote_Password"].ConnectionString;

      using (NetworkShareAccesser.Access("ftp.claycountygov.com", "", Remote_Login, Remote_Password))
      {
        try
        {

          staffing_filename = File_Save_Path + GetFileName("staffing", PayPeriodStart);
          if (!File.Exists(staffing_filename))
          {
            var staffingdata = StaffingData.GetByPayPeriod(PayPeriodStart);
            var staffingtext = StaffingData.ToString(staffingdata);
            File.WriteAllText(staffing_filename, staffingtext);
          }


          //DateTime current = DateTime.Today.AddDays(-1);

          //string current_staffing_filename = File_Save_Path + GetFileName("staffing", current);
          //string current_changes_filename = File_Save_Path + GetFileName("changes", current);

          //if (!File.Exists(current_staffing_filename))
          //{
          //  var staffingdata = StaffingData.Get(current);
          //  var staffingtext = StaffingData.ToString(staffingdata);
          //  File.WriteAllText(current_staffing_filename, staffingtext);
          //}

          //if (!File.Exists(current_changes_filename))
          //{
          //  var changedata = ChangeData.Get(current);
          //  var changetext = ChangeData.ToString(changedata);
          //  File.WriteAllText(current_changes_filename, changetext);
          //}

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
      return "Telestaff_" + (filetype == "staffing" ? "Payperiod_Load" : "Changes") + "_" + workdate.ToString("yyyyMMdd") + ".txt";
    }

  }
}
