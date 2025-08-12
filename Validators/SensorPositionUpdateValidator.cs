// File: Validators/SensorPositionUpdateValidator.cs
using FluentValidation;
using GasFireMonitoringServer.Models.DTOs.Requests;

namespace GasFireMonitoringServer.Validators
{
    public class SensorPositionUpdateValidator : AbstractValidator<SensorPositionUpdateDto>
    {
        public SensorPositionUpdateValidator()
        {
            RuleFor(x => x.SiteId)
                .GreaterThan(0).WithMessage("Site ID must be greater than 0");

            RuleFor(x => x.SensorPositions)
                .NotEmpty().WithMessage("At least one sensor position is required");

            RuleForEach(x => x.SensorPositions)
                .ChildRules(sensor =>
                {
                    sensor.RuleFor(x => x.ChannelId)
                        .NotEmpty().WithMessage("Channel ID is required")
                        .MaximumLength(10).WithMessage("Channel ID cannot exceed 10 characters");

                    sensor.RuleFor(x => x.LayoutX)
                        .InclusiveBetween(0, 100).WithMessage("X coordinate must be between 0 and 100");

                    sensor.RuleFor(x => x.LayoutY)
                        .InclusiveBetween(0, 100).WithMessage("Y coordinate must be between 0 and 100");
                });
        }
    }
}