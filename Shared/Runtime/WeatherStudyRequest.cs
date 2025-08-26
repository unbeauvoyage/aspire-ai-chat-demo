using System.Collections.Generic;

namespace Dtos
{
    public sealed class WeatherStudyRequest
    {
        public IReadOnlyList<WeatherForecastDto> Forecasts { get; set; }
        public string UserPrompt { get; set; }
        public string Purpose { get; set; }
    }
}

