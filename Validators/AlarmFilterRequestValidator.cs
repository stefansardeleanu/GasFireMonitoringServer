// File: Validators/AlarmFilterRequestValidator.cs
using FluentValidation;
using GasFireMonitoringServer.Models.DTOs.Requests;

namespace GasFireMonitoringServer.Validators
{
    public class AlarmFilterRequestValidator : AbstractValidator<AlarmFilterRequestDto>
    {
        public AlarmFilterRequestValidator()
        {
            RuleFor(x => x.SiteId)
                .GreaterThan(0).WithMessage("Site ID must be greater than 0")
                .When(x => x.SiteId.HasValue);

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Start date cannot be in the future")
                .When(x => x.StartDate.HasValue);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date")
                .When(x => x.EndDate.HasValue && x.StartDate.HasValue);

            RuleFor(x => x.MaxResults)
                .GreaterThan(0).WithMessage("MaxResults must be greater than 0")
                .LessThanOrEqualTo(1000).WithMessage("MaxResults cannot exceed 1000");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than 0")
                .LessThanOrEqualTo(500).WithMessage("PageSize cannot exceed 500");
        }
    }
}