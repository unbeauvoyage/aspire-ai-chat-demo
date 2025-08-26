using System;
using System.Collections.Generic;

namespace Dtos
{
    public sealed class StudyMessageDto
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public sealed class StudySessionDto
    {
        public Guid Id { get; set; }
        public string Topic { get; set; }
        public IReadOnlyList<StudyMessageDto> Messages { get; set; }
    }

    public sealed class StartStudyRequest
    {
        public string Topic { get; set; }
        public string Level { get; set; }
        public string Exam { get; set; }
    }

    public sealed class SendMessageRequest
    {
        public Guid SessionId { get; set; }
        public string Message { get; set; }
    }
}


