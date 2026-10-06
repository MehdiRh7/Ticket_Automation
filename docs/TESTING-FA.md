# راهنمای تست کامل Ticket AI Orchestrator

این سناریو ابتدا با `Push` خاموش اجرا می‌شود. Agent روی Clone موقت تغییر می‌دهد و Commit محلی می‌سازد، اما چیزی به مخزن اصلی ارسال نمی‌شود. بعد از مشاهده نتیجه می‌توانید Push را فعال کنید.

## ۱. ساخت دیتابیس آزمایشی SQL Server

فایل زیر را در SSMS باز کنید:

```text
scripts/sqlserver/01-create-test-database.sql
```

در SSMS از منوی `Query` گزینه `SQLCMD Mode` را فعال و اسکریپت را Execute کنید. مقادیر بالای فایل قابل تغییرند:

```sql
:setvar DatabaseName "TicketAutomationTest"
:setvar ReaderLogin "ticket_ai_reader"
:setvar ReaderPassword "LocalTest-ChangeMe-2026!"
```

برای تست محلی می‌توانید مقادیر پیش‌فرض را نگه دارید. برای هر محیط مشترک رمز را حتماً عوض کنید.

اجرای خط فرمان با Windows Authentication:

```powershell
sqlcmd -S "localhost" -E -i ".\scripts\sqlserver\01-create-test-database.sql"
```

برای SQL Express معمولاً Server به شکل زیر است:

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -i ".\scripts\sqlserver\01-create-test-database.sql"
```

اسکریپت این موارد را می‌سازد:

- دیتابیس `TicketAutomationTest`
- جدول `support.Tickets`
- Login فقط‌خواندنی `ticket_ai_reader`
- ایندکس مناسب Polling
- دو تیکت نمونه با وضعیت `Draft`

دسترسی کاربر Agent عمداً فقط `SELECT` است و `INSERT/UPDATE/DELETE` برای او Deny شده است.

## ۲. اجرای Codex و بررسی Git

در همان حساب ویندوزی که برنامه را اجرا می‌کند، این دستورات باید موفق باشند:

```powershell
codex --version
git --version
```

اگر Codex احراز هویت نشده است:

```powershell
codex login
```

برای تست با Claude باید `claude --version` نیز موفق باشد و Claude Code قبلاً احراز هویت شده باشد.

اگر Repository خصوصی است، قبل از اجرای Orchestrator یک بار Clone معمولی را با همان حساب سیستم امتحان کنید:

```powershell
git clone https://github.com/ORG/REPOSITORY.git
```

Orchestrator رمز GitHub را نگهداری نمی‌کند؛ از Git Credential Manager، GitHub App یا Credential حساب سرویس استفاده می‌کند.

## ۳. اجرای برنامه

از پوشه اصلی پروژه:

```powershell
dotnet restore --configfile NuGet.Config
dotnet run --project src\TicketAutomation.Web
```

آدرس نمایش‌داده‌شده در ترمینال را باز و وارد صفحه `تنظیمات` شوید.

## ۴. مقادیر دقیق صفحه تنظیمات

### اجرای خودکار

| فیلد | مقدار تست اولیه |
|---|---|
| Polling فعال باشد | فعلاً خاموش |
| فاصله بررسی | `30` |
| حداکثر تیکت | `20` |

### دیتابیس تیکتینگ

| فیلد | مقدار |
|---|---|
| نوع دیتابیس | `SqlServer` |
| Connection String | یکی از نمونه‌های پایین |
| نام جدول | `support.Tickets` |
| ستون شناسه | `Id` |
| ستون عنوان | `Title` |
| ستون شرح | `Description` |
| ستون زمان ایجاد | `CreatedAt` |
| ستون نسخه | `Version` |
| ستون وضعیت | `Status` |
| مقدار وضعیت قابل پردازش | `AI_READY` |

Connection String برای SQL Server پیش‌فرض:

```text
Server=localhost;Database=TicketAutomationTest;User Id=ticket_ai_reader;Password=LocalTest-ChangeMe-2026!;Encrypt=True;TrustServerCertificate=True;
```

برای SQL Express:

```text
Server=.\SQLEXPRESS;Database=TicketAutomationTest;User Id=ticket_ai_reader;Password=LocalTest-ChangeMe-2026!;Encrypt=True;TrustServerCertificate=True;
```

اگر SQL Authentication روی SQL Server شما غیرفعال است، برای تست محلی با حساب ویندوزی دارای دسترسی خواندن استفاده کنید:

```text
Server=localhost;Database=TicketAutomationTest;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;
```

### Git و Workspace

| فیلد | مقدار پیشنهادی |
|---|---|
| Repository URL | URL مخزن یا مسیر کامل یک Repository محلی |
| Base branch/tag | نام واقعی شاخه مبنا، معمولاً `main` یا `master` |
| قالب نسخه | برای تست اولیه خالی |
| پیشوند شاخه | `ai/test-ticket-` |
| مسیر Workspace | `C:\TicketAI\workspaces` |
| نام Committer | `Ticket Automation Test` |
| ایمیل Committer | `ticket-automation-test@localhost` |
| Push | برای تست اول خاموش |

نمونه Repository راه دور:

```text
https://github.com/company/project.git
```

نمونه Repository محلی در ویندوز:

```text
C:\Projects\MyProject
```

Repository محلی هم باید Git Repository معتبر باشد و شاخه مبنای واردشده را داشته باشد.

در ویندوز مسیر Workspace را کوتاه و خارج از Repository نگه دارید. قرار دادن Workspace داخل پوشه خود پروژه ممکن است به خطای `Filename too long` یا Clone تو در تو منجر شود. پوشه‌های `bin`، `obj`، `work`، `workspaces`، `.vs` و دیتابیس Runtime نباید Commit شوند.

### Agent Provider

برای Codex:

| فیلد | مقدار |
|---|---|
| Provider فعال | `Codex` |
| Timeout | `45` |
| Codex executable | `codex` |
| Codex arguments | `exec --sandbox workspace-write --ephemeral --json -` |

برای Claude:

| فیلد | مقدار |
|---|---|
| Provider فعال | `Claude` |
| Claude executable | `claude` |
| Claude arguments | `-p --output-format json --permission-mode acceptEdits` |

Provider در لحظه کشف تیکت روی Job ثبت می‌شود. برای آزمایش Provider دیگر، ابتدا Provider را عوض و سپس یک تیکت جدید ایجاد کنید.

پس از واردکردن مقادیر، دکمه `ذخیره تنظیمات` را بزنید.

## ۵. آماده‌کردن اولین تیکت امن

نمونه دوم ساخته‌شده در دیتابیس فقط README را تغییر می‌دهد و وضعیت اولیه‌اش `Draft` است. پس از تکمیل تنظیمات، فایل زیر را باز کنید:

```text
scripts/sqlserver/03-activate-and-monitor.sql
```

شناسه را روی `2` نگه دارید و اسکریپت را اجرا کنید. وضعیت تیکت `AI_READY` می‌شود.

اکنون در پنل `تیکت‌ها و گزارش‌ها` دکمه `بررسی همین حالا` را بزنید. جریان مورد انتظار:

```text
Pending → Running → Completed
```

در صفحه جزئیات Job باید این موارد را ببینید:

- گزارش Agent
- Branch ساخته‌شده
- فایل‌های تغییرکرده
- Commit SHA
- لاگ اجرا
- مسیر Workspace

چون Push خاموش است، Branch فقط در Clone موقت قرار دارد. برای مشاهده تغییر:

```powershell
git -C "WORKSPACE_PATH_FROM_REPORT" show --stat
git -C "WORKSPACE_PATH_FROM_REPORT" diff HEAD~1 HEAD
```

## ۶. تست Push کنترل‌شده

بعد از موفقیت تست محلی:

1. در تنظیمات گزینه Push را فعال کنید.
2. مطمئن شوید حساب اجرای برنامه اجازه Push به `ai/*` دارد.
3. یک تیکت جدید با `02-insert-test-ticket.sql` درج کنید.
4. متن آن را بازبینی کنید.
5. وضعیت تیکت جدید را به `AI_READY` تغییر دهید.
6. دکمه `بررسی همین حالا` را بزنید.

برای هر تیکت جدید Branch جدا ساخته می‌شود. برنامه روی `main` Push، Merge، PR، Publish یا Deploy انجام نمی‌دهد.

## ۷. نوشتن یک تیکت واقعی و قابل‌حل

شرح تیکت بهتر است این ساختار را داشته باشد:

```text
مشکل:
در صفحه ورود، بعد از منقضی‌شدن درخواست احراز هویت خطای عمومی نمایش داده می‌شود.

مراحل بازتولید:
1. برنامه را اجرا کنید.
2. Timeout سرویس احراز هویت را شبیه‌سازی کنید.
3. روی ورود کلیک کنید.

رفتار فعلی:
HTTP 500 و پیام عمومی.

رفتار مورد انتظار:
خطای کنترل‌شده و قابل‌فهم نمایش داده شود.

محدوده تغییر:
فقط مسیر مدیریت Timeout اصلاح شود. قرارداد API تغییر نکند.

تست پذیرش:
تست موجود و تست جدید Timeout پاس شوند.
```

از قراردادن رمز عبور، Token، Connection String واقعی، اطلاعات مشتری یا داده بانکی داخل تیکت خودداری کنید.

## ۸. اتصال به این گفت‌وگو

برنامه نمی‌تواند همین Chat یا حافظه مکالمه را به‌عنوان Provider انتخاب کند. گزینه `Codex`، Codex CLI احراز هویت‌شده روی سیستم را اجرا می‌کند. برای انتقال قواعد پروژه به هر اجرای جدید، یک فایل `AGENTS.md` در ریشه Repository قرار دهید؛ برای Claude می‌توانید قواعد متناظر را در `CLAUDE.md` نیز نگهداری کنید.

برای اینکه تنظیم Repository را دقیق کنیم، این سه مقدار لازم است:

1. مسیر محلی یا URL مخزن
2. نام شاخه مبنا
3. اینکه تست اول فقط Local Commit باشد یا Push به Remote نیز انجام شود
