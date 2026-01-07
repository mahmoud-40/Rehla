namespace BreastCancer.Templates
{
    public class EmailTemplates
    {
        private static string GetBaseTemplate(string title, string body)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <body style='font-family: Arial; max-width: 500px; margin: auto; padding: 20px;'>
                <div style='text-align: center; margin-bottom: 30px;'>
                    <h2 style='color: #4a6fa5;'>{title}</h2>
                </div>

                {body}

                <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>

                <p style='text-align: center; color: #999; font-size: 12px;'>
                    © Rehla Support<br>
                    Automated email - do not reply
                </p>
            </body>
            </html>";
        }

        // 1️⃣ Email Confirmation
        public static string GetConfirmationEmail(string userName, string code)
        {
            var body = $@"
                <p>Hello {userName},</p>
                <p>Confirm your email for <strong>Rehla</strong> using this code:</p>

                <div style='background: #f5f5f5; padding: 20px; text-align: center; margin: 25px 0; border-radius: 5px;'>
                    <h1 style='letter-spacing: 8px;'>{code}</h1>
                </div>

                <p style='color: #666; font-size: 14px;'>
                    This code expires in 5 minutes.<br>
                    If you didn't request this, ignore this email.
                </p>";

            return GetBaseTemplate("Rehla", body);
        }

        // 2️⃣ Forget Password (send code)
        public static string GetForgetPasswordEmail(string userName, string code)
        {
            var body = $@"
                <p>Hello {userName},</p>
                <p>Use the following code to reset your password:</p>

                <div style='background: #f5f5f5; padding: 20px; text-align: center; margin: 25px 0; border-radius: 5px;'>
                    <h1 style='letter-spacing: 8px;'>{code}</h1>
                </div>

                <p style='color: #666; font-size: 14px;'>
                    This code expires in 5 minutes.<br>
                    If you didn't request this, please secure your account.
                </p>";

            return GetBaseTemplate("Rehla - Reset Password", body);
        }

        // 3️⃣ Password Successfully Reset (NO CODE)
        public static string GetPasswordResetSuccessEmail(string userName)
        {
            var body = $@"
                <p>Hello {userName},</p>

                <p>Your password has been <strong>successfully changed</strong>.</p>

                <p style='color: #666; font-size: 14px;'>
                    If you did not perform this action, please contact support immediately.
                </p>";

            return GetBaseTemplate("Rehla - Password Changed", body);
        }
    }
}