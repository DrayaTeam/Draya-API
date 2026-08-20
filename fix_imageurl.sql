BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818174459_AddImageUrlToClassroomAndQuestions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818174459_AddImageUrlToClassroomAndQuestions', N'10.0.10');
END;

COMMIT;
GO

