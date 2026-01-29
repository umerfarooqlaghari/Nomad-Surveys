using System;
using System.Collections.Generic;

namespace Nomad.Api.DTOs.Request;

public class BulkEmailRequest
{
    public List<Guid> SubjectEvaluatorSurveyIds { get; set; } = new();
}
