USE [TicketAutomationTest];
GO

DECLARE @TicketId BIGINT = 2; -- شناسه موردنظر را تغییر دهید

UPDATE [support].[Tickets]
SET [Status] = N'AI_READY'
WHERE [Id] = @TicketId
  AND [Status] = N'Draft';

SELECT [Id], [Title], [Description], [Status], [Version], [CreatedAt]
FROM [support].[Tickets]
WHERE [Id] = @TicketId;
GO

/*
  Orchestrator عمداً وضعیت تیکت را تغییر نمی‌دهد.
  پس از پایان تست و بررسی گزارش، برای خارج‌شدن از صف نمایشی اجرا کنید:

  UPDATE [support].[Tickets]
  SET [Status] = N'Closed'
  WHERE [Id] = 2;
*/
