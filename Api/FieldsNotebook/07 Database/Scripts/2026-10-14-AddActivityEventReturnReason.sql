-- Run once against the Field Notebook database before deploying the review-and-close workflow.
-- An affected crew's Foreman can send proof of work back; the reason is kept on the history event.
IF COL_LENGTH('dbo.ActivityEvents', 'ReturnReason') IS NULL
BEGIN
    ALTER TABLE dbo.ActivityEvents ADD ReturnReason NVARCHAR(1000) NULL;
END;
