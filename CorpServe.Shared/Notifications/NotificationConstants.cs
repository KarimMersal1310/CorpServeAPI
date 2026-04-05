namespace CorpServe.Shared.Notifications
{
    public static class NotificationTypes
    {
        public const string Info = "Info";
        public const string Success = "Success";
        public const string Warning = "Warning";
        public const string Error = "Error";
    }

    public static class NotificationEventKeys
    {
        public const string NotificationReceived = "notificationReceived";
    }

    public static class NotificationTitles
    {
        public const string WelcomeToCorpServe = "Welcome to CorpServe";
        public const string ProfileUpdated = "Profile updated";
        public const string PasswordChanged = "Password changed";
        public const string PasswordReset = "Password reset";

        public const string RequestCreated = "Request created";
        public const string RequestUpdated = "Request updated";
        public const string RequestDeleted = "Request deleted";
        public const string RequestProgressUpdated = "Request progress updated";
        public const string NewRequestAvailable = "New request available";

        public const string NewProposalReceived = "New proposal received";
        public const string ProposalAccepted = "Proposal accepted";
        public const string ProposalRejected = "Proposal rejected";
        public const string SlaCreated = "SLA created";

        public const string VerificationSubmitted = "Verification submitted";
        public const string NewVendorVerification = "New vendor verification";
        public const string VerificationApproved = "Verification approved";
        public const string VerificationRejected = "Verification rejected";

        public const string SlaCompleted = "SLA completed";
        public const string SlaBlocked = "SLA blocked";
        public const string SlaDelayed = "SLA delayed";
        public const string SlaDeadlineWarning = "SLA deadline warning";
    }
}
