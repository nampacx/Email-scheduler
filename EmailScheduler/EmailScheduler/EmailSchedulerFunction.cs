using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FunctionApp
{
    public static class EmailSchedulerFunction
    {
        private static IConfiguration Configuration { get; set; }

        static EmailSchedulerFunction()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets(typeof(EmailSchedulerFunction).Assembly);

            Configuration = builder.Build();
        }

        [FunctionName(nameof(EmailSchedulerFunction))]
        public static async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer,
                                     [Table("test", Connection = "AzureWebJobsStorage")] TableClient table,
                                     ILogger log,
                                     ExecutionContext context)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Read the sender email and password from the App Settings
            string senderEmail = Configuration.GetValue<string>("SenderEmail");
            string senderPassword = Configuration.GetValue<string>("SenderPassword");

            SmtpClient client = new SmtpClient("smtp-mail.outlook.com", 587);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(senderEmail, senderPassword);
            client.EnableSsl = true;

            foreach (DataEntity entity in table.Query<DataEntity>(d => d.SendDate == DateTime.Now.ToString("yyyy-MM-dd")))
            {
                log.LogInformation($"Sending email to recipient: {entity.Recipient}");

                // Create and send the email
                MailMessage message = new MailMessage();
                message.From = new MailAddress(senderEmail);
                message.To.Add(entity.Recipient);
                message.Subject = entity.Subject;
                message.Body = entity.Body;

                client.Send(message);

                log.LogInformation($"Email sent successfully to recipient: {entity.Recipient}");
            }
        }

        public class DataEntity : ITableEntity
        {
            public string Subject { get; set; }
            public string Recipient { get; set; }
            public string Body { get; set; }
            public string SendDate { get; set; }
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }
    }
}
