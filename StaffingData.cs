using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using System.Data;
using System.Data.SqlClient;

namespace TelestaffWorkdayDataShare
{
  public class StaffingData
  {
    public int Staffing_Primary_Key { get; set; }
    public DateTime Staffing_Timestamp { get; set; }
    public int EmployeeID { get; set; }
    public string Work_Type_Full { get; set; }
    public string Work_Type_Abrv { get; set; }
    public DateTime Work_Date { get; set; }
    public DateTime Shift_Start_Date { get; set; }
    public DateTime Shift_End_Date { get; set; }
    public decimal Staffing_Hours { get; set; }
    public string Employee_Type { get; set; }


    public static List<StaffingData> GetByPayPeriod(DateTime PayPeriodStart)
    {
      var ds = new DynamicParameters();
      ds.Add("@PayPeriodStart", PayPeriodStart);
      string query = @"
SELECT
  S.staffing_no_in Staffing_Primary_Key
  ,SEST.staffing_timestamp_est Staffing_Timestamp
  ,RMT.RscMaster_EmployeeID_Ch EmployeeID
  ,W.Wstat_Name_Ch Work_Type_Full
  ,CASE
     WHEN LTRIM(RTRIM(W.Wstat_Abrv_Ch)) = ''
     THEN 'STRAIGHT'
     ELSE UPPER(LTRIM(RTRIM(W.Wstat_Abrv_Ch)))
   END Work_Type_Abrv
  ,S.staffing_calendar_da Work_Date
  ,SEST.staffing_start_dt_est Shift_Start_Date
  ,SEST.staffing_end_dt_est Shift_End_Date
  ,( CAST(DATEDIFF(minute
                   ,SEST.Staffing_Start_Dt_est
                   ,SEST.Staffing_End_Dt_est) AS DECIMAL(10, 2)) / 60 ) Staffing_Hours
  ,CASE R.PayInfo_No_In
     WHEN 1
     THEN 'Field'
     WHEN 2
     THEN 'Office'
     WHEN 4
     THEN 'Dispatch'
     ELSE ''
   END Employee_Type

FROM
  WorkForceTelestaff.dbo.staffing_tbl S
  INNER JOIN WorkForceTelestaff.dbo.vw_staffing_tbl_est SEST ON S.staffing_no_in = SEST.staffing_no_in
  INNER JOIN WorkForceTelestaff.dbo.Resource_Tbl R ON S.rsc_no_in = R.Rsc_no_in
  INNER JOIN WorkForceTelestaff.dbo.Resource_Master_Tbl RMT ON R.RscMaster_No_in = RMT.RscMaster_No_In
  INNER JOIN WorkForceTelestaff.dbo.wstat_cde_tbl W ON W.wstat_no_in = S.wstat_no_in
  LEFT OUTER JOIN Job_Title_Tbl J ON J.Job_No_In = R.Job_No_In
WHERE
  S.staffing_calendar_da BETWEEN @PayPeriodStart AND DATEADD(DAY
                                                             ,13
                                                             ,@PayPeriodStart)
  AND W.Wstat_Abrv_Ch NOT IN ( 'OTR', 'OTRR', 'ORD', 'ORRD',
                               'OR', 'NO', 'DPRN', 'BR', 
                               'MWI', 'DMWI', 'SLOT')
  AND J.Job_Abrv_Ch NOT IN ('FC', 'MECH', 'DCO', 'LO', 'ADM', 'LTECH')  
  AND S.staffing_request_state <> 20 -- Removes Denied requests
ORDER  BY
   Shift_Start_Date ASC, EmployeeID ASC
";

      try
      {
        using (IDbConnection db = new SqlConnection(Program.GetCS("Telestaff")))
        {
          return (List<StaffingData>)db.Query<StaffingData>(query, ds);
        }
      }
      catch (Exception ex)
      {
        new ErrorLog(ex, query);
        return null;
      }

    }



    public static List<StaffingData>GetByCreateDate(DateTime WorkDate)
    {
      /* 
       * This function has not been updated and is currently unused.
       * If it is ever to be used again, the output should be changed 
       * to mirror the GetByPayperiod function
       * 
       */
      var ds = new DynamicParameters();
      ds.Add("@WorkDate", WorkDate);
      string query = @"

SELECT 
  S.staffing_no_in Staffing_Primary_Key
  ,SEST.staffing_timestamp_est Staffing_Timestamp
  ,RMT.RscMaster_EmployeeID_Ch EmployeeID
  ,W.Wstat_Name_Ch Work_Type_Full
  ,CASE
     WHEN LTRIM(RTRIM(W.Wstat_Abrv_Ch)) = ''
     THEN 'STRAIGHT'
     ELSE UPPER(LTRIM(RTRIM(W.Wstat_Abrv_Ch)))
   END Work_Type_Abrv  
  ,S.staffing_calendar_da Work_Date
  ,SEST.staffing_start_dt_est Shift_Start_Date
  ,SEST.staffing_end_dt_est Shift_End_Date
  ,( CAST(DATEDIFF(minute
                   ,SEST.Staffing_Start_Dt_est
                   ,SEST.Staffing_End_Dt_est) AS DECIMAL(10, 2)) / 60 ) Staffing_Hours

  ,CASE R.PayInfo_No_In
    WHEN 1 THEN 'Field'
    WHEN 2 THEN 'Dispatch'
    WHEN 4 THEN 'Office'
    ELSE ''
    END Employee_Type
    
FROM staffing_tbl S
INNER JOIN vw_staffing_tbl_est SEST ON S.staffing_no_in = SEST.staffing_no_in
INNER JOIN Resource_Tbl R ON S.rsc_no_in = R.Rsc_no_in
INNER JOIN Resource_Master_Tbl RMT ON R.RscMaster_No_in = RMT.RscMaster_No_In
INNER JOIN wstat_cde_tbl W ON W.wstat_no_in = S.wstat_no_in
WHERE
  CAST(SEST.staffing_timestamp_est AS DATE) = @WorkDate
    AND W.Wstat_Abrv_Ch NOT IN ( 'OTR', 'OTRR', 'ORD', 'ORRD','OR',
                               'NO', 'DPRN')
ORDER BY SEST.staffing_timestamp_est
";

      try
      {
        using (IDbConnection db = new SqlConnection(Program.GetCS("Telestaff")))
        {
          return (List<StaffingData>)db.Query<StaffingData>(query, ds);
        }
      }
      catch (Exception ex)
      {
        new ErrorLog(ex, query);
        return null;
      }

    }

    private static string CreateHeaderRow()
    {
      return "Staffing_Primary_Key\tStaffing_Timestamp\tEmployeeID\tWork_Type_Full\tWork_Type_Abrv\tWork_Date\tShift_Start_Date\tShift_End_Date\tStaffing_Hours\tEmployee_Type\r\n";
    }

    public static string ToString(List<StaffingData> data)
    {
      var sb = new StringBuilder();
      sb.Append(CreateHeaderRow());
      foreach(StaffingData d in data)
      {
        sb.Append(d.Staffing_Primary_Key.ToString());
        sb.Append("\t");
        sb.Append(d.Staffing_Timestamp.ToString());
        sb.Append("\t");
        sb.Append(d.EmployeeID.ToString());
        sb.Append("\t");
        sb.Append(d.Work_Type_Full);
        sb.Append("\t");
        sb.Append(d.Work_Type_Abrv);
        sb.Append("\t");
        sb.Append(d.Work_Date.Date.ToShortDateString());
        sb.Append("\t");
        sb.Append(d.Shift_Start_Date.ToString());
        sb.Append("\t");
        sb.Append(d.Shift_End_Date.ToString());
        sb.Append("\t");
        sb.Append(d.Staffing_Hours.ToString());
        sb.Append("\t");
        sb.AppendLine(d.Employee_Type);
      }
      return sb.ToString();
    }


  }
}
