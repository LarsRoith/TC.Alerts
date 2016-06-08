#load "..\Shared\SendEmail.csx"

#r "System"
#r "System.Configuration"
#r "Newtonsoft.Json"
 
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using Newtonsoft.Json;



const string timeCockpitBaseUrl = "https://api.timecockpit.com";
const string MessageBody = @"<!DOCTYPE html>
                             <html xmlns=""http://www.w3.org/1999/xhtml\"">
                                <head>
                                    <meta charset = ""utf-8"" />
                                    <title> Pending vacation approvals</title>
                                    <style>
                                        table, th, td {
                                            border: 1px solid black;
                                            border - collapse: collapse;
                                        }
                                        th, td {
                                            padding: 5px;
                                        }
                                    </style>
                                </head>
                                <body>
                                    <table style = ""width: 100%"" >
                                        <tr>
                                            <th> User </ th >
                                            <th> User Email</th>
                                            <th>Start date</th>
                                            <th>End date</th>
                                        </tr>
                                        %REPLACEMENTTOKEN%
                                    </table>
                                </body>
                             </html>";
const string VacationLine = @"<tr>
                                <td>{0}</td>
                                <td>{1}</td>
                                <td>{2}</td>
                                <td>{3}</td>
                            </tr>";
const string ReplacementToken = "%REPLACEMENTTOKEN%";
 
public static string Base64Encode(string plainText) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
 
public static async Task Run(TimerInfo myTimer, TraceWriter log)
{
    log.Verbose("Starting custom function handling...");
    string timeCockpitAuth = ConfigurationManager.AppSettings["tcUser"] + ":" + ConfigurationManager.AppSettings["tcPassword"];
    using (var client = new HttpClient())
    {
        log.Verbose("Build query...");
        var queryObject = new
        {
            query = @"From V In Vacation.Include('UserDetail') Where V.VacationApproved <> True Select New  With { 
                                	.User = V.UserDetail.DisplayName,
                                	.UserEmail = V.UserDetail.Username,
                                	.StartDate = V.BeginTime,
                                	.EndDate = V.EndTime
                                }"
        };
        log.Verbose("Run query: " + queryObject.query);
        using (var request = new HttpRequestMessage
        {
            RequestUri = new Uri(timeCockpitBaseUrl + "/select"),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonConvert.SerializeObject(queryObject), Encoding.UTF8, "application/json")
        })
        {
            log.Verbose("Add authorization headers...");
            request.Headers.Add("Authorization", "Basic " + Base64Encode(timeCockpitAuth));

            log.Verbose("Run request...");
            using (var response = await client.SendAsync(request))
            {
                string content = await response.Content.ReadAsStringAsync();
                log.Verbose(content);

                dynamic vacations = JsonConvert.DeserializeObject(content);
                if (vacations.value.Count == 0)
                {
                    log.Verbose("No Vacations to approve!");
                }
                else
                {
                    var msgLines = new StringBuilder();
                    for (var counter = 0; counter < vacations.value.Count; counter++)
                    {
                        var entry = vacations.value[counter];
                        msgLines.AppendFormat(VacationLine, entry.USR_User, entry.USR_UserEmail, entry.USR_StartDate, entry.USR_EndDate);
                    }
                    var msgBody = MessageBody.Replace(ReplacementToken, msgLines.ToString());

                    SendMailAsync("Vacation approval reminder", msgBody, log);
                }
            }
        }
    }
}
