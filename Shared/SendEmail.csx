public static void SendMailAsync(string subject, string body, TraceWriter log)
{
    MailMessage msg = new MailMessage();
    msg.To.Add(new MailAddress(ConfigurationManager.AppSettings["vacationToMail"], ConfigurationManager.AppSettings["vacationToName"]));
    msg.From = new MailAddress(ConfigurationManager.AppSettings["vacationFromMail"], ConfigurationManager.AppSettings["vacationFromName"]);
    msg.Subject = subject;
    msg.Body = body;
    msg.IsBodyHtml = true;

    using (SmtpClient client = new SmtpClient())
    {
        client.UseDefaultCredentials = false;
        client.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["o365User"], ConfigurationManager.AppSettings["o365Password"]);
        client.Port = 587; // You can use Port 25 if 587 is blocked (mine is!)
        client.Host = "smtp.office365.com";
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.EnableSsl = true;
        try
        {
            client.Send(msg);
        }
        catch (Exception ex)
        {
            
            log.Verbose("Error while sending email: " + ex.ToString ());
        }
    }
}