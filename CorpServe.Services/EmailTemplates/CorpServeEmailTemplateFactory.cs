namespace CorpServe.Services.EmailTemplates
{
    public static class CorpServeEmailTemplateFactory
    {
        public static (string Subject, string Body) BuildResetPassword(string fullName, string resetLink, string lifespanMinutes)
        {
            var encodedName = Encode(fullName);
            var encodedLifespan = Encode(lifespanMinutes);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Locked out? No sweat.
              </p>
              <p style='margin:0 0 20px; color:#475569; line-height:1.7;'>
                Someone (hopefully you) asked to reset the password on this account. Hit the button and you'll be back in business.
              </p>
              {BuildActionButton(resetLink, "Reset My Password")}
              <div style='margin:22px 0 14px; padding:14px 16px; border-radius:12px; border:1px solid #dbeafe; background:#eff6ff; color:#1e3a8a; font-size:14px;'>
                <span style='font-size:15px;'>🔒</span> This link expires in <strong style='color:#0f172a;'>{encodedLifespan} minutes</strong> and can only be used once.
              </div>
              <p style='margin:20px 0 0; color:#475569; font-size:15px; line-height:1.7; text-align:center;'>
                <strong style='color:#0f172a;'>Wasn't you?</strong> No worries - just ignore this email and nothing changes. Your account is still safe.
              </p>";

            return BuildTemplate(
                subject: "Reset Your Password",
                preheader: "Secure account recovery",
                title: "Password Reset",
                subtitle: "Secure account recovery",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildVendorVerificationApproved(string vendorName)
        {
            var encodedVendorName = Encode(vendorName);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedVendorName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                You're approved and ready to go.
              </p>
              <p style='margin:0 0 20px; color:#475569; line-height:1.7;'>
                Your vendor verification request has been approved. You can now submit proposals, collaborate with clients, and access all vendor features on CorpServe.
              </p>
              <div style='margin:0 0 16px; padding:14px 16px; border-radius:12px; border:1px solid #bbf7d0; background:#f0fdf4; color:#166534; font-size:14px;'>
                <strong>Status:</strong> Approved
              </div>
              <p style='margin:20px 0 0; color:#475569; font-size:15px; line-height:1.7;'>
                Need help getting started? Reach out to <a href='mailto:corpserve.b2b@gmail.com' style='color:#4f46e5; text-decoration:underline;'>corpserve.b2b@gmail.com</a>.
              </p>";

            return BuildTemplate(
                subject: "Vendor Verification Approved",
                preheader: "Your vendor account is now active",
                title: "Verification Approved",
                subtitle: "Your CorpServe vendor profile is ready",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildVendorVerificationRejected(string vendorName, string rejectReason)
        {
            var encodedVendorName = Encode(vendorName);
            var encodedReason = Encode(rejectReason).Replace("\n", "<br />").Replace("\r", string.Empty);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedVendorName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Verification needs a quick update.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                After review, your vendor verification request was not approved.
              </p>
              <p style='margin:0 0 8px; color:#0f172a; font-weight:600;'>Reason provided by the reviewer</p>
              <div style='margin:0 0 18px; padding:14px 16px; border-radius:12px; border:1px solid #fecaca; background:#fef2f2; color:#991b1b; font-size:14px; line-height:1.6;'>
                {encodedReason}
              </div>
              <p style='margin:0; color:#475569; font-size:15px; line-height:1.7;'>
                Please update the required information and submit a new verification request.
              </p>
              <p style='margin:20px 0 0; color:#475569; font-size:15px; line-height:1.7;'>
                If you keep having trouble, reach out to <a href='mailto:corpserve.b2b@gmail.com' style='color:#4f46e5; text-decoration:underline;'>corpserve.b2b@gmail.com</a>.
              </p>";

            return BuildTemplate(
                subject: "Vendor Verification Update",
                preheader: "Action required for your vendor verification request",
                title: "Verification Requires Update",
                subtitle: "Please review and resubmit your request",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildVendorAcceptProposal(string clientName, string vendorName, string requestTitle, decimal? proposedPrice, DateTime? proposedDeadline, string? message)
        {
            var encodedClientName = Encode(clientName);
            var encodedVendorName = Encode(vendorName);
            var encodedRequestTitle = Encode(requestTitle);
            var encodedMessage = Encode(message);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedClientName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                New vendor acceptance proposal received.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                <strong>{encodedVendorName}</strong> submitted an acceptance proposal for request <strong>{encodedRequestTitle}</strong>.
              </p>
              <div style='margin:0 0 16px; padding:14px 16px; border-radius:12px; border:1px solid #dbeafe; background:#eff6ff; color:#1e3a8a; font-size:14px; line-height:1.6;'>
                <p style='margin:0 0 8px;'><strong>Proposed Price:</strong> {proposedPrice?.ToString("0.00") ?? "N/A"}</p>
                <p style='margin:0;'><strong>Proposed Deadline:</strong> {proposedDeadline?.ToString("yyyy-MM-dd") ?? "N/A"}</p>
              </div>
              {(string.IsNullOrWhiteSpace(encodedMessage) ? string.Empty : $"<p style='margin:0; color:#475569; line-height:1.7;'><strong>Message:</strong> {encodedMessage}</p>")}";

            return BuildTemplate(
                subject: "New Acceptance Proposal",
                preheader: "A vendor accepted your request terms",
                title: "Acceptance Proposal Received",
                subtitle: "Review the new offer in your dashboard",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildVendorNegotiateProposal(string clientName, string vendorName, string requestTitle, decimal? proposedPrice, DateTime? proposedDeadline, string? message)
        {
            var encodedClientName = Encode(clientName);
            var encodedVendorName = Encode(vendorName);
            var encodedRequestTitle = Encode(requestTitle);
            var encodedMessage = Encode(message);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedClientName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                New negotiation proposal received.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                <strong>{encodedVendorName}</strong> submitted a negotiation proposal for request <strong>{encodedRequestTitle}</strong>.
              </p>
              <div style='margin:0 0 16px; padding:14px 16px; border-radius:12px; border:1px solid #fde68a; background:#fffbeb; color:#92400e; font-size:14px; line-height:1.6;'>
                <p style='margin:0 0 8px;'><strong>Proposed Price:</strong> {proposedPrice?.ToString("0.00") ?? "N/A"}</p>
                <p style='margin:0;'><strong>Proposed Deadline:</strong> {proposedDeadline?.ToString("yyyy-MM-dd") ?? "N/A"}</p>
              </div>
              {(string.IsNullOrWhiteSpace(encodedMessage) ? string.Empty : $"<p style='margin:0; color:#475569; line-height:1.7;'><strong>Message:</strong> {encodedMessage}</p>")}";

            return BuildTemplate(
                subject: "New Negotiation Proposal",
                preheader: "A vendor sent a negotiation offer",
                title: "Negotiation Proposal Received",
                subtitle: "Review and decide in your dashboard",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildVendorRejectProposal(string clientName, string vendorName, string requestTitle, string? message)
        {
            var encodedClientName = Encode(clientName);
            var encodedVendorName = Encode(vendorName);
            var encodedRequestTitle = Encode(requestTitle);
            var encodedMessage = Encode(message);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedClientName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Vendor rejected the request.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                <strong>{encodedVendorName}</strong> submitted a rejection for request <strong>{encodedRequestTitle}</strong>.
              </p>
              {(string.IsNullOrWhiteSpace(encodedMessage) ? string.Empty : $"<p style='margin:0; color:#475569; line-height:1.7;'><strong>Reason:</strong> {encodedMessage}</p>")}";

            return BuildTemplate(
                subject: "Vendor Rejected Request",
                preheader: "A vendor rejected your request",
                title: "Request Rejection Received",
                subtitle: "Review update in your dashboard",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildClientAcceptedProposal(string vendorName, string clientName, string requestTitle, decimal contractPrice, DateTime deadline)
        {
            var encodedVendorName = Encode(vendorName);
            var encodedClientName = Encode(clientName);
            var encodedRequestTitle = Encode(requestTitle);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedVendorName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Your proposal has been accepted.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                Client <strong>{encodedClientName}</strong> accepted your proposal for request <strong>{encodedRequestTitle}</strong>.
              </p>
              <div style='margin:0 0 16px; padding:14px 16px; border-radius:12px; border:1px solid #bbf7d0; background:#f0fdf4; color:#166534; font-size:14px; line-height:1.6;'>
                <p style='margin:0 0 8px;'><strong>Contract Price:</strong> {contractPrice:0.00}</p>
                <p style='margin:0;'><strong>Deadline:</strong> {deadline:yyyy-MM-dd}</p>
              </div>
              <p style='margin:0; color:#475569; line-height:1.7;'>
                SLA contract is now active. Please proceed with delivery updates from your dashboard.
              </p>";

            return BuildTemplate(
                subject: "Proposal Accepted",
                preheader: "Your proposal was accepted and SLA is active",
                title: "Proposal Accepted",
                subtitle: "You can start working on the active request",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildSignupWelcomeAndProfileReminder(string fullName)
        {
            var encodedName = Encode(fullName);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Welcome to CorpServe.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                Your account has been created successfully. To get better visibility and trust from clients and vendors, please complete your profile details.
              </p>
              <div style='margin:0 0 16px; padding:14px 16px; border-radius:12px; border:1px solid #dbeafe; background:#eff6ff; color:#1e3a8a; font-size:14px; line-height:1.6;'>
                <strong>Next step:</strong> Open your profile page and complete your information.
              </div>
              <p style='margin:0; color:#475569; line-height:1.7;'>
                If you need help, contact <a href='mailto:corpserve.b2b@gmail.com' style='color:#4f46e5; text-decoration:underline;'>corpserve.b2b@gmail.com</a>.
              </p>";

            return BuildTemplate(
                subject: "Welcome to CorpServe",
                preheader: "Your account is ready - complete your profile",
                title: "Welcome to CorpServe",
                subtitle: "Complete your profile to get started",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildAccountSuspended(string fullName, string? reason = null)
        {
            var encodedName = Encode(fullName);
            var encodedReason = Encode(reason).Replace("\n", "<br />").Replace("\r", string.Empty);

            var reasonBlock = string.IsNullOrWhiteSpace(encodedReason)
                ? string.Empty
                : $@"
              <p style='margin:0 0 8px; color:#0f172a; font-weight:600;'>Reason</p>
              <div style='margin:0 0 18px; padding:14px 16px; border-radius:12px; border:1px solid #fecaca; background:#fef2f2; color:#991b1b; font-size:14px; line-height:1.6;'>
                {encodedReason}
              </div>";

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Your account has been suspended.
              </p>
              <div style='margin:0 0 16px; padding:14px 16px; border-radius:12px; border:1px solid #fecaca; background:#fef2f2; color:#991b1b; font-size:14px; line-height:1.6;'>
                <strong>Status:</strong> Suspended
              </div>
              {reasonBlock}
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                Your CorpServe account has been suspended. You currently cannot access normal account actions until this is reviewed.
              </p>
              <p style='margin:0; color:#475569; line-height:1.7;'>
                If you believe this was done by mistake, contact <a href='mailto:corpserve.b2b@gmail.com' style='color:#4f46e5; text-decoration:underline;'>corpserve.b2b@gmail.com</a>.
              </p>";

            return BuildTemplate(
                subject: "Account Suspended",
                preheader: "Your CorpServe account has been suspended",
                title: "Account Suspended",
                subtitle: "Please contact support for assistance",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildClientRejectProposal(string vendorName, string clientName, string requestTitle, string reason)
        {
            var encodedVendorName = Encode(vendorName);
            var encodedClientName = Encode(clientName);
            var encodedRequestTitle = Encode(requestTitle);
            var encodedReason = Encode(reason).Replace("\n", "<br />").Replace("\r", string.Empty);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedVendorName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Your proposal was rejected by the client.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                <strong>{encodedClientName}</strong> rejected your proposal for request <strong>{encodedRequestTitle}</strong>.
              </p>
              <p style='margin:0 0 8px; color:#0f172a; font-weight:600;'>Reason provided by the client</p>
              <div style='margin:0 0 18px; padding:14px 16px; border-radius:12px; border:1px solid #fecaca; background:#fef2f2; color:#991b1b; font-size:14px; line-height:1.6;'>
                {encodedReason}
              </div>
              <p style='margin:0; color:#475569; font-size:15px; line-height:1.7;'>
                You can review other available requests from your dashboard.
              </p>";

            return BuildTemplate(
                subject: "Proposal Rejected by Client",
                preheader: "Your proposal was rejected",
                title: "Proposal Rejected",
                subtitle: "Review the reason and explore other opportunities",
                contentHtml: content);
        }

        public static (string Subject, string Body) BuildPaymentOverdueWarning(string clientName, string requestTitle, decimal amount, int dueInHours = 24)
        {
            var encodedClientName = Encode(clientName);
            var encodedRequestTitle = Encode(requestTitle);

            var content = $@"
              <p style='margin:0 0 16px; color:#475569; font-size:16px; line-height:1.6;'>
                Hi <strong style='color:#0f172a;'>{encodedClientName}</strong>,
              </p>
              <p style='margin:0 0 18px; color:#0f172a; line-height:1.75; font-size:18px; font-weight:700;'>
                Payment overdue — action required.
              </p>
              <p style='margin:0 0 14px; color:#475569; line-height:1.7;'>
                Your payment of <strong>{amount:0.00}</strong> for request <strong>{encodedRequestTitle}</strong> is overdue.
              </p>
              <div style='margin:0 0 16px; padding:14px 16px; border-radius:12px; border:1px solid #fde68a; background:#fffbeb; color:#92400e; font-size:14px; line-height:1.6;'>
                <strong>Warning:</strong> Please complete the payment within <strong>{dueInHours} hours</strong> to avoid account suspension.
              </div>
              <p style='margin:0; color:#475569; font-size:15px; line-height:1.7;'>
                If you need help, contact <a href='mailto:corpserve.b2b@gmail.com' style='color:#4f46e5; text-decoration:underline;'>corpserve.b2b@gmail.com</a>.
              </p>";

            return BuildTemplate(
                subject: "Payment Overdue Warning",
                preheader: "Complete your payment to avoid suspension",
                title: "Payment Overdue",
                subtitle: "Immediate action required",
                contentHtml: content);
        }

        private static (string Subject, string Body) BuildTemplate(
            string subject,
            string preheader,
            string title,
            string subtitle,
            string contentHtml)
        {
            var encodedPreheader = Encode(preheader);
            var encodedTitle = Encode(title);
            var encodedSubtitle = Encode(subtitle);

            var body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>{Encode(subject)}</title>
</head>
<body style='margin:0; padding:0; background:#f1f5f9; font-family:Segoe UI, Arial, sans-serif;'>
  <div style='display:none; max-height:0; overflow:hidden; opacity:0;'>
    {encodedPreheader}
  </div>
  <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background:#f1f5f9; padding:36px 12px;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='640' cellspacing='0' cellpadding='0' style='max-width:640px; width:100%; background:#ffffff; border:1px solid #e2e8f0; border-radius:8px; overflow:hidden; box-shadow:0 12px 32px rgba(15,23,42,0.12);'>
          <tr>
            <td style='padding:30px 28px 8px; color:#0f172a;' align='center'>
              <img src='cid:corpserve-logo' alt='CorpServe' width='44' height='44' style='display:block; width:44px; height:44px; border:0; border-radius:8px;' />
              <h1 style='margin:16px 0 0; font-size:24px; line-height:1.3; font-weight:700; color:#0f172a;'>{encodedTitle}</h1>
              <p style='margin:8px 0 0; font-size:14px; line-height:1.55; color:#64748b;'>{encodedSubtitle}</p>
            </td>
          </tr>
          <tr>
            <td style='padding:16px 28px 28px;'>
              {contentHtml}
            </td>
          </tr>
          <tr>
            <td style='padding:0 28px 24px;'>
              <p style='margin:0; color:#64748b; font-size:12px; line-height:1.6;'>
                This is an automated message from CorpServe. Please do not reply directly to this email.
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            return (subject, body);
        }

        private static string BuildActionButton(string url, string label)
        {
            var encodedUrl = Encode(url);
            var encodedLabel = Encode(label);

            return $@"
              <p style='margin:0 0 10px; text-align:center;'>
                <a href='{encodedUrl}' style='display:block; width:100%; max-width:100%; box-sizing:border-box; min-height:44px; padding:12px 20px; border-radius:12px; background:#2563eb; background-image:linear-gradient(to right,#2563eb,#7c3aed); color:#ffffff; text-decoration:none; font-weight:700; font-size:14px; line-height:20px; text-align:center; box-shadow:0 12px 28px rgba(89,79,235,0.35);'>
                  {encodedLabel}
                </a>
              </p>";
        }

        private static string Encode(string? value)
        {
            return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}
