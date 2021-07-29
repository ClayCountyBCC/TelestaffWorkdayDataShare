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
  public class ChangeData
  {
    public DateTime Audit_Date { get; set; }
    public int Audit_Primary_Key { get; set; }
    public string Operation_Type { get; set; }
    public int Staffing_Primary_Key { get; set; }
    public DateTime? Staffing_Timestamp { get; set; }
    public int? EmployeeID { get; set; }
    public string Work_Type_Full { get; set; }
    public string Work_Type_Abrv { get; set; }
    public DateTime? Work_Date { get; set; }
    public DateTime? Shift_Start_Date { get; set; }
    public DateTime? Shift_End_Date { get; set; }
    public decimal? Staffing_Hours { get; set; }
    public string Employee_Type { get; set; }



    public static List<ChangeData> Get(DateTime WorkDate)
    {
      var ds = new DynamicParameters();
      ds.Add("@WorkDate", WorkDate);
      string query = @"

--DECLARE @Start DATE = '7/2/2021';
--DECLARE @End DATE = '7/5/2021';

WITH StaffingView
     AS (SELECT
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
              THEN 'Dispatch'
              WHEN 4
              THEN 'Office'
              ELSE ''
            END Employee_Type
         FROM
           staffing_tbl S
           LEFT OUTER JOIN vw_staffing_tbl_est SEST ON S.staffing_no_in = SEST.staffing_no_in
           LEFT OUTER JOIN Resource_Tbl R ON S.rsc_no_in = R.Rsc_no_in
           LEFT OUTER JOIN Resource_Master_Tbl RMT ON R.RscMaster_No_in = RMT.RscMaster_No_In
           LEFT OUTER JOIN wstat_cde_tbl W ON W.wstat_no_in = S.wstat_no_in)
    ,AuditView
     AS (SELECT
           CONVERT(DATETIME, SWITCHOFFSET(Audit_When_Dt
                                          ,DATEPART(TZOFFSET
                                                    ,Audit_When_Dt AT TIME ZONE 'Eastern Standard Time'))) Audit_Date
           ,Audit_No_In Audit_Primary_Key
           ,A.Key_No_In Staffing_Primary_Key
           ,ATYPE.AuditType_Name_Ch Operation_Type
           ,S.Staffing_Timestamp
           ,S.EmployeeID
           ,ISNULL(S.Work_Type_Full
                   ,'') Work_Type_Full
           ,ISNULL(S.Work_Type_Abrv
                   ,'') Work_Type_Abrv
           ,S.Work_Date
           ,S.Shift_Start_Date
           ,S.Shift_End_Date
           ,S.Staffing_Hours
           ,ISNULL(S.Employee_Type
                   ,'') Employee_Type
         FROM
           Audit_Tbl A
           INNER JOIN Audit_Type_Tbl ATYPE ON A.AuditType_No_In = ATYPE.AuditType_No_In
           LEFT OUTER JOIN StaffingView S ON A.Key_No_In = S.Staffing_Primary_Key
         WHERE
          A.AuditTarget_No_In = 501
          AND A.AuditType_No_In IN ( 2, 4 ))
SELECT
  Audit_Date
  ,Audit_Primary_Key
  ,Staffing_Primary_Key
  ,Operation_Type
  ,Staffing_Timestamp
  ,EmployeeID
  ,Work_Type_Full
  ,Work_Type_Abrv
  ,Work_Date
  ,Shift_Start_Date
  ,Shift_End_Date
  ,Staffing_Hours
  ,Employee_Type
FROM
  AuditView
WHERE
  CAST(Audit_Date AS DATE) = @WorkDate
ORDER  BY
  Audit_Date ASC
";

      try
      {
        using (IDbConnection db = new SqlConnection(Program.GetCS("Telestaff")))
        {
          return (List<ChangeData>)db.Query<ChangeData>(query, ds);
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
      return "Audit_Date\tAudit_Primary_Key\tStaffing_Primary_Key\tOperation_Type\tStaffing_Timestamp\tEmployeeID\tWork_Type_Full\tWork_Type_Abrv\tWork_Date\tShift_Start_Date\tShift_End_Date\tStaffing_Hours\tEmployee_Type\r\n";
    }

    public static string ToString(List<ChangeData> data)
    {
      try
      {
        var sb = new StringBuilder();
        sb.Append(CreateHeaderRow());
        foreach (ChangeData d in data)
        {
          sb.Append(d.Audit_Date.ToString());
          sb.Append("\t");
          sb.Append(d.Audit_Primary_Key.ToString());
          sb.Append("\t");
          sb.Append(d.Staffing_Primary_Key.ToString());
          sb.Append("\t");
          sb.Append(d.Operation_Type);
          sb.Append("\t");
          sb.Append(d.Staffing_Timestamp.ToString() ?? "");
          sb.Append("\t");
          sb.Append(d.EmployeeID.ToString() ?? "");
          sb.Append("\t");
          sb.Append(d.Work_Type_Full);
          sb.Append("\t");
          sb.Append(d.Work_Type_Abrv);
          sb.Append("\t");
          if (d.Work_Date.HasValue)
          {
            sb.Append(d.Work_Date.Value.ToShortDateString());
          }          
          sb.Append("\t");
          sb.Append(d.Shift_Start_Date.ToString() ?? "");
          sb.Append("\t");
          sb.Append(d.Shift_End_Date.ToString() ?? "");
          sb.Append("\t");
          sb.Append(d.Staffing_Hours.ToString() ?? "");
          sb.Append("\t");
          sb.AppendLine(d.Employee_Type);
          //if (!d.EmployeeID.HasValue)
          //{
          //  sb.AppendLine("\t\t\t\t\t\t\t\t");
          //}
          //else
          //{
          //  try
          //  {
          //    sb.Append(d.Staffing_Timestamp.Value.ToString());
          //    sb.Append("\t");
          //    sb.Append(d.EmployeeID.Value.ToString());
          //    sb.Append("\t");
          //    sb.Append(d.Work_Type_Full);
          //    sb.Append("\t");
          //    sb.Append(d.Work_Type_Abrv);
          //    sb.Append("\t");
          //    sb.Append(d.Work_Date.Value.Date.ToShortDateString());
          //    sb.Append("\t");
          //    sb.Append(d.Shift_Start_Date.Value.ToString());
          //    sb.Append("\t");
          //    sb.Append(d.Shift_End_Date.Value.ToString());
          //    sb.Append("\t");
          //    sb.Append(d.Staffing_Hours.Value.ToString());
          //    sb.Append("\t");
          //    sb.AppendLine(d.Employee_Type);
          //  }
          //  catch(Exception ex)
          //  {
          //    new ErrorLog(ex);
          //  }
          //}

        }
        return sb.ToString();
      }
      catch(Exception ex)
      {
        new ErrorLog(ex);
        return "";
      }
    }


  }
}
