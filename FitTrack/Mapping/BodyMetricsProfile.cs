using AutoMapper;
using FitTrack.DTOs.Metrics;
using FitTrack.Model;

namespace FitTrack.Mapping;

public sealed class BodyMetricsProfile : Profile
{
    public BodyMetricsProfile()
    {
        CreateMap<BodyMetrics, BodyMetricsDto>()
            .ForMember(dto => dto.Bmi, option => option.MapFrom(
                metrics => Math.Round(CalculateBmi(metrics), 1, MidpointRounding.AwayFromZero)))
            .ForMember(dto => dto.BmiCategory, option => option.MapFrom(
                metrics => GetBmiCategory(CalculateBmi(metrics))));
    }

    private static decimal CalculateBmi(BodyMetrics metrics)
    {
        var heightMeters = metrics.HeightCm / 100m;
        return metrics.WeightKg / (heightMeters * heightMeters);
    }

    private static string GetBmiCategory(decimal bmi) => bmi switch
    {
        < 18.5m => "Underweight",
        < 25m => "Normal",
        < 30m => "Overweight",
        _ => "Obese"
    };
}
