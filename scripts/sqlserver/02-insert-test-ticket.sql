USE [TicketAutomationTest];
GO

/*
  Copy this INSERT for each new test.
  Keep Status as Draft while writing the ticket, then change it to AI_READY.
  Version may be NULL; when supplied it can be mapped to a release branch.
*/
INSERT INTO [support].[Tickets]
(
    [Title],
    [Description],
    [Status],
    [Version],
    [Priority],
    [RequestedBy],
    [EnvironmentName]
)
VALUES
(
    N'عنوان تیکت آزمایشی',
    N'شرح دقیق مشکل، مراحل بازتولید، رفتار فعلی، رفتار مورد انتظار و محدودیت‌های تغییر را اینجا بنویسید.',
    N'Draft',
    NULL,
    3,
    N'Local Tester',
    N'Local'
);

DECLARE @TicketId BIGINT = SCOPE_IDENTITY();

SELECT *
FROM [support].[Tickets]
WHERE [Id] = @TicketId;

/*
  After reviewing the text, activate this ticket with:

  UPDATE [support].[Tickets]
  SET [Status] = N'AI_READY'
  WHERE [Id] = @TicketId;
*/
GO
