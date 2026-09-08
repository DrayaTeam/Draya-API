BEGIN TRANSACTION;
CREATE TABLE [ClassroomFeedback] (
    [Id] uniqueidentifier NOT NULL,
    [ClassroomId] uniqueidentifier NOT NULL,
    [StudentId] uniqueidentifier NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ClassroomFeedback] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ClassroomFeedback_Rating] CHECK ([Rating] >= 1 AND [Rating] <= 5),
    CONSTRAINT [FK_ClassroomFeedback_Classrooms_ClassroomId] FOREIGN KEY ([ClassroomId]) REFERENCES [Classrooms] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_ClassroomFeedback_ClassroomId] ON [ClassroomFeedback] ([ClassroomId]);

CREATE UNIQUE INDEX [UQ_ClassroomFeedback_ClassroomId_StudentId] ON [ClassroomFeedback] ([ClassroomId], [StudentId]);


COMMIT;
GO

