using System.Collections.Generic;

namespace Dtos
{
    public sealed class WeatherAnalysisDto
    {
        public System.Guid Id { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public string Analysis { get; set; }
        public string UserPrompt { get; set; }
    }
}

