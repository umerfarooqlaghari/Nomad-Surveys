using System;
using System.Collections.Generic;

namespace Alpha.Api.DTOs.Request;

public class BulkEmailRequest
{
    public List<Guid> SubjectEvaluatorSurveyIds { get; set; } = new();
}
