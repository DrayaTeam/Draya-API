using System;

namespace Draya.Domain.Exams;

public class StudentAnswer
{
    public Guid Id { get; private set; }
    public Guid StudentExamAttemptId { get; private set; }
    public Guid ExamQuestionId { get; private set; }
    public string AnswerText { get; private set; } = string.Empty;
    public Guid? SelectedOptionId { get; private set; }

    public AnswerGradingResult? GradingResult { get; private set; }

    private StudentAnswer() { }

    public StudentAnswer(Guid studentExamAttemptId, Guid examQuestionId, string answerText, Guid? selectedOptionId = null)
    {
        Id = Guid.NewGuid();
        StudentExamAttemptId = studentExamAttemptId;
        ExamQuestionId = examQuestionId;
        AnswerText = answerText;
        SelectedOptionId = selectedOptionId;
    }

    public void SetGradingResult(AnswerGradingResult result)
    {
        GradingResult = result;
    }
}
