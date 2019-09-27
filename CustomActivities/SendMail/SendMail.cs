using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace CustomActivities.SendMail
{
    // SendMail活动允许在Workflow应用程序中使用SMTP发送邮件
    // 为了实现此目标，SendMail活动使用System.Net.Mail中的功能
    // 要使用此活动，您将需要有权访问运行中的SMTP服务器
    [Designer(typeof(SendMailDesigner))]
    public sealed class SendMail : AsyncCodeActivity
    {
        [RequiredArgument]
        public InArgument<MailAddressCollection> To { get; set; }

        [RequiredArgument]
        public InArgument<MailAddress> From { get; set; }

        [RequiredArgument]
        public InArgument<string> Subject { get; set; }

        [DefaultValue(null)]
        public InArgument<MailAddress> TestMailTo { get; set; }

        public InArgument<Collection<Attachment>> Attachments { get; set; }
        public InArgument<MailAddressCollection> CC { get; set; }
        public InArgument<MailAddressCollection> Bcc { get; set; }
        public InArgument<IDictionary<string, string>> Tokens { get; set; }
        public string Body { get; set; }
        public string BodyTemplateFilePath { get; set; }
        public string TestDropPath { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }

        public SendMail()
        {
            this.Port = 25;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (string.IsNullOrEmpty(this.Host))
            {
                metadata.AddValidationError("Property 'Host' of SendMail activity cannot be null or empty");
            }

            if (this.From == null)
            {
                metadata.AddValidationError("Property 'From' of SendMail activity cannot be null or empty");
            }

            if (this.Port <= 0)
            {
                metadata.AddValidationError("The value of property 'Port' of SendMail activity must be larger than 0");
            }

            if (this.BodyTemplateFilePath != null && !File.Exists(this.BodyTemplateFilePath))
            {
                metadata.AddValidationError("The provided path for the body template (argument 'BodyTemplateFilePath') does not exist or access is denied.");
            }

            base.CacheMetadata(metadata);            
        }

        // 将正文中由用户在令牌词典中找到的标记替换为指定的值
        private void ReplaceTokensInBody(CodeActivityContext context)
        {
            IDictionary<string, string> t = Tokens.Get(context);

            foreach (string key in t.Keys)
            {
                this.Body = this.Body.Replace(key, t[key]);
            }
        }

        // 如果指定了 bodyTemplateFile 属性，则为邮件的正文加载模板
        private void LoadBodyTemplate(CodeActivityContext context)
        {
            if (!string.IsNullOrEmpty(this.BodyTemplateFilePath))
            {
                using (StreamReader re = File.OpenText(this.BodyTemplateFilePath))
                {
                    this.Body = re.ReadToEnd();
                }
            }
        }

        // 如果指定了 testMailToAdress，则 1）将邮件的收件人更改为该地址，2）在电子邮件底部添加注释
        private void AddTestInformationToBody(CodeActivityContext context)
        {
            StringBuilder buffer = new StringBuilder();

            buffer.Append("<br/>");
            buffer.Append("<hr/>");
            buffer.Append(string.Format("<b>Test Mode</b> - TestMailTo address is {0}", this.TestMailTo.Get(context).Address));
            buffer.Append("<hr/>");

            string bodyWithTestInfo = this.Body + buffer.ToString();

            this.Body = bodyWithTestInfo;
        }

        // 如果设置了testMailDropPath属性，则电子邮件将写入文件中
        // 在下列路径：
        //    xxxx.body.html with body 
        //    xxxx.data.txt with message data (from, to, cc, bcc, and subject)
        private void WriteMailInTestDropPath(CodeActivityContext context)
        {
            // create file with Html of the body
            string testDropBodyFileName = string.Format("{0}\\{1}.body.htm", this.TestDropPath, DateTime.Now.ToString("yyyyMMddhhmmssff"));
            using (TextWriter writer = new StreamWriter(testDropBodyFileName))
            {
                writer.Write(this.Body);
            }

            // 使用 from（来自）, to（到）, cc（抄送）, bcc（密件抄送）, subject（主题）来创建文件
            string testDropDataFileName = string.Format("{0}\\{1}.data.txt", this.TestDropPath, DateTime.Now.ToString("yyyyMMddhhmmssff"));
            MailAddressCollection toList = this.To.Get(context);
            MailAddressCollection bccList = this.Bcc.Get(context);
            MailAddressCollection ccList = this.CC.Get(context);

            using (TextWriter writer = new StreamWriter(testDropDataFileName))
            {
                writer.Write("From: {0}", this.From.Get(context).Address);

                writer.Write("\r\nTo: ");
                foreach (MailAddress address in toList)
                {
                    writer.Write(string.Format("{0} ", address.Address));
                }

                if (TestMailTo.Expression != null)
                {
                    writer.WriteLine("\r\nTest MailTo Mode Enable...Address: {0}", TestMailTo.Get(context).Address);
                }

                if (ccList != null)
                {
                    writer.Write("\r\nCc: ");
                    foreach (MailAddress address in ccList)
                    {
                        writer.Write("{0} ", address.Address);
                    }
                }

                if (bccList != null)
                {
                    writer.Write("\r\nBcc: ");
                    foreach (MailAddress address in bccList)
                    {
                        writer.Write("{0} ", address.Address);
                    }
                }

                writer.Write("\r\nSubject: {0}", this.Subject.Get(context));
            }
        }

        protected override void Cancel(AsyncCodeActivityContext context)
        {
            SendMailAsyncResult sendMailAsyncResult = (SendMailAsyncResult) context.UserState;
            sendMailAsyncResult.Client.SendAsyncCancel();
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            MailMessage message = new MailMessage();
            message.From = this.From.Get(context);

            if (TestMailTo.Expression == null)
            {
                foreach (MailAddress address in this.To.Get(context))
                {
                    message.To.Add(address);
                }

                MailAddressCollection ccList = this.CC.Get(context);
                if (ccList != null)
                {
                    foreach (MailAddress address in ccList)
                    {
                        message.CC.Add(address);
                    }
                }

                MailAddressCollection bccList = this.Bcc.Get(context);
                if (bccList != null)
                {
                    foreach (MailAddress address in bccList)
                    {
                        message.Bcc.Add(address);
                    }
                }
            }
            else
            {
                message.To.Add(TestMailTo.Get(context));
            }

            Collection<Attachment> attachments = this.Attachments.Get(context);
            if (attachments != null)
            {
                foreach (Attachment attachment in attachments)
                {
                    message.Attachments.Add(attachment);
                }
            }

            if (!string.IsNullOrEmpty(this.BodyTemplateFilePath))
            {
                LoadBodyTemplate(context);
            }

            if ((this.Tokens.Get(context) != null) && (this.Tokens.Get(context).Count > 0))
            {
                ReplaceTokensInBody(context);
            }

            if (this.TestMailTo.Expression != null)
            {
                AddTestInformationToBody(context);
            }

            message.Subject = this.Subject.Get(context);
            message.Body = this.Body;

            SmtpClient client = new SmtpClient();
            client.Host = this.Host;
            client.Port = this.Port;
            client.EnableSsl = this.EnableSsl;

            if (string.IsNullOrEmpty(this.UserName))
            {
                client.UseDefaultCredentials = true;
            }
            else
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(this.UserName, this.Password);
            }

            if (!string.IsNullOrEmpty(this.TestDropPath))
            {
                WriteMailInTestDropPath(context);
            }

            var sendMailAsyncResult = new SendMailAsyncResult(client, message, callback, state);
            context.UserState = sendMailAsyncResult;
            return sendMailAsyncResult;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            // 结束执行，不需要做任何事情
        }

        class SendMailAsyncResult : IAsyncResult
        {
            SmtpClient client;
            AsyncCallback callback;
            object asyncState;
            EventWaitHandle asyncWaitHandle;

            public bool CompletedSynchronously { get { return false; } }
            public object AsyncState { get { return this.asyncState; } }
            public WaitHandle AsyncWaitHandle { get { return this.asyncWaitHandle; } }
            public bool IsCompleted { get { return true; } }
            public SmtpClient Client { get { return client; } }

            public SendMailAsyncResult(SmtpClient client, MailMessage message, AsyncCallback callback, object state)
            {
                this.client = client;
                this.callback = callback;
                this.asyncState = state;
                this.asyncWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                client.SendCompleted += new SendCompletedEventHandler(SendCompleted);
                client.SendAsync(message, null);
            }

            void SendCompleted(object sender, AsyncCompletedEventArgs e)
            {
                this.asyncWaitHandle.Set();
                callback(this);
            }
        }
    }
}
