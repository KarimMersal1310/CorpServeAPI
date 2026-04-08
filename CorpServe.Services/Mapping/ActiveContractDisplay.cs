using CorpServe.Domain.Entities.IdentityModule;
using CorpServe.Domain.Entities.ProposalModule;
using CorpServe.Shared.DTOs.ProposalDTOs;
using System;
using System.Globalization;
using System.Linq;

namespace CorpServe.Services.Mapping
{
    /// <summary>
    /// Builds <see cref="ActiveRequestDTO"/> from a loaded <see cref="SLAContract"/> graph (no AutoMapper expression trees).
    /// </summary>
    public static class ActiveContractDisplay
    {
        public static ActiveRequestDTO ToActiveRequestDto(SLAContract c)
        {
            var dto = new ActiveRequestDTO
            {
                RequestId = c.RequestId ?? string.Empty,
                Price = c.ContractPrice,
                Deadline = c.Deadline,
                ClientName = SanitizeDisplayText(c.Client?.FullName),
                VendorName = SanitizeDisplayText(c.Vendor?.FullName),
            };

            if (c.Request != null)
            {
                dto.Title = SanitizeDisplayText(c.Request.Title);
                dto.Description = SanitizeDisplayText(c.Request.Discription);
                var rp = c.Request.RequestProgress;
                dto.ProgressPercentage = rp?.Percentage ?? 0;
                dto.LatestWorkUpdate = string.IsNullOrWhiteSpace(rp?.Description)
                    ? null
                    : rp.Description.Trim();
            }
            else
            {
                dto.Title = string.Empty;
                dto.Description = string.Empty;
            }

            dto.RemainingTimeDisplay = FormatRemainingDisplay(c.Deadline);
            dto.TaskState = ResolveTaskState(c);
            dto.SlaLabel = ResolveSlaLabel(c);
            return dto;
        }

        private static DateTime DeadlineAsUtc(DateTime deadline) => deadline.Kind switch
        {
            DateTimeKind.Utc => deadline,
            DateTimeKind.Local => deadline.ToUniversalTime(),
            _ => DateTime.SpecifyKind(deadline, DateTimeKind.Utc),
        };

        private static string ResolveTaskState(SLAContract contract)
        {
            var deadline = DeadlineAsUtc(contract.Deadline);
            var now = DateTime.UtcNow;

            if (contract.SLAStatus == SLAStatus.Completed)
                return "Completed";
            if (contract.SLAStatus == SLAStatus.Delayed)
                return "Delayed";
            if (contract.SLAStatus == SLAStatus.Inprogress && deadline <= now)
                return "Delayed";
            if (contract.SLAStatus == SLAStatus.Inprogress)
                return "In Progress";
            return contract.SLAStatus.ToString();
        }

        private static string ResolveSlaLabel(SLAContract contract)
        {
            var deadline = DeadlineAsUtc(contract.Deadline);
            var now = DateTime.UtcNow;

            if (contract.SLAStatus == SLAStatus.Delayed)
            {
                if (contract.Client?.Status == UserStatus.Suspended || contract.Vendor?.Status == UserStatus.Suspended)
                    return "Blocked";
                return "Delayed";
            }

            if (contract.SLAStatus == SLAStatus.Inprogress)
            {
                if (deadline <= now)
                    return "Delayed";

                var remainingHours = (deadline - now).TotalHours;
                if (remainingHours <= 72)
                    return "Warning";

                return "On Track";
            }

            return contract.SLAStatus.ToString();
        }

        private static string FormatRemainingDisplay(DateTime deadline)
        {
            var deadlineUtc = DeadlineAsUtc(deadline);
            var now = DateTime.UtcNow;
            var remaining = deadlineUtc - now;

            if (remaining.TotalSeconds <= 0)
            {
                var overdue = now - deadlineUtc;
                if (overdue.TotalHours < 24)
                {
                    var h = Math.Max(1, (int)Math.Ceiling(overdue.TotalHours));
                    return h == 1 ? "Overdue by 1 hour" : $"Overdue by {h} hours";
                }

                var days = Math.Max(1, (int)Math.Ceiling(overdue.TotalDays));
                return days == 1 ? "Overdue by 1 day" : $"Overdue by {days} days";
            }

            if (remaining.TotalHours < 48)
            {
                var h = Math.Max(1, (int)Math.Ceiling(remaining.TotalHours));
                return h == 1 ? "1 hour left" : $"{h} hours left";
            }

            var daysLeft = Math.Max(1, (int)Math.Ceiling(remaining.TotalDays));
            return daysLeft == 1 ? "1 day left" : $"{daysLeft} days left";
        }

        private static string SanitizeDisplayText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = new string(value
                .Where(ch => char.GetUnicodeCategory(ch) != UnicodeCategory.Format)
                .ToArray());

            return cleaned.Trim();
        }
    }
}
