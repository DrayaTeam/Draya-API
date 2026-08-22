using System;

namespace Draya.Domain.Exams.Exceptions;

public class ExamAttemptSubmissionException(string message) : Exception(message);
