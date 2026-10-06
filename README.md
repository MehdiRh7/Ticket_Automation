# Ticket AI Orchestrator

سامانه وب `.NET 8` برای خواندن دوره‌ای تیکت‌ها، اجرای Codex یا Claude روی یک Clone موقت، ساخت Feature Branch و نگهداری گزارش در دیتابیس داخلی. این برنامه هیچ پاسخ یا تغییری در دیتابیس تیکتینگ ثبت نمی‌کند.

## رفتار سیستم

1. `TicketPollingWorker` با حساب فقط‌خواندنی، تیکت‌های دارای وضعیت تنظیم‌شده را می‌خواند.
2. شناسه تیکت در SQLite داخلی Unique است و از پردازش دوباره جلوگیری می‌کند.
3. Provider انتخاب‌شده در زمان کشف تیکت روی Job ثبت می‌شود.
4. مخزن Clone و Branch با پیشوند اجباری `ai/` ساخته می‌شود و Remote پیش از اجرای Agent موقتاً حذف می‌شود.
5. Codex یا Claude در Workspace همان Job اجرا می‌شود. Agent اجازه Commit، Push، Merge یا Deploy ندارد.
6. خود Orchestrator تغییرات را Commit می‌کند و در صورت فعال بودن گزینه Push، فقط همان Branch را Push می‌کند.
7. گزارش، فایل‌های تغییرکرده، SHA و خطاها فقط در دیتابیس داخلی ذخیره می‌شوند.

## پیش‌نیازها

- .NET SDK 8
- Git CLI و Credential موردنیاز برای Clone/Push
- یکی یا هر دو CLI زیر روی ماشین Runner:
  - Codex CLI؛ قبل از اجرای سرویس باید احراز هویت شده باشد.
  - Claude Code CLI؛ قبل از اجرای سرویس باید احراز هویت شده باشد.
- حساب دیتابیس تیکت با دسترسی `SELECT` و بدون دسترسی نوشتن

## اجرا

```powershell
dotnet restore --configfile NuGet.Config
dotnet run --project src/TicketAutomation.Web
```

سپس آدرس نمایش‌داده‌شده در ترمینال را باز کنید و از صفحه «تنظیمات» این موارد را وارد کنید:

- نوع دیتابیس و Connection String فقط‌خواندنی
- نام جدول و ستون‌های شناسه، عنوان، شرح، زمان، وضعیت و نسخه
- وضعیت قابل پردازش، برای مثال `New` یا `AI_READY`
- URL مخزن، شاخه مبنا و مسیر Workspace
- Provider فعال: `Codex` یا `Claude`
- مسیر و آرگومان CLIها

Polling در تنظیمات اولیه خاموش است. پس از تکمیل و آزمایش اتصال آن را فعال کنید.

برای سناریوی تست SQL Server، ساخت دیتابیس نمونه و مقادیر دقیق همه فیلدها، فایل [docs/TESTING-FA.md](docs/TESTING-FA.md) را بخوانید.

## نگاشت نسخه به شاخه

اگر ستون نسخه تیکت مقدار `2.7.3` دارد و Releaseهای شما با `release/2.7.3` نام‌گذاری شده‌اند، مقدار `VersionBaseRefTemplate` را به شکل زیر تنظیم کنید:

```text
release/{version}
```

در غیر این صورت `DefaultBaseRef`، مثلاً `main`، استفاده می‌شود.

## انتخاب Provider

اتصال‌ها پشت `IAgentProvider` هستند. تنظیم پیش‌فرض Codex:

```text
Executable: codex
Arguments: exec --sandbox workspace-write --ephemeral --json -
```

تنظیم پیش‌فرض Claude:

```text
Executable: claude
Arguments: -p --output-format json --permission-mode acceptEdits
```

آرگومان‌ها از پنل قابل تغییرند تا با نسخه CLI و سیاست سازمان شما هماهنگ شوند. برای افزودن Provider جدید، `IAgentProvider` را پیاده‌سازی و در `Program.cs` ثبت کنید.

## GitHub و Git Server خصوصی

Orchestrator از Git CLI نصب‌شده روی Runner استفاده می‌کند؛ بنابراین GitHub، GitHub Enterprise و Git Server داخلی قابل استفاده‌اند. Credential را در حساب سرویس/Runner پیکربندی کنید. دسترسی پیشنهادی:

- Read repository
- Create branch
- Push فقط به الگوی `ai/*`
- بدون دسترسی Merge، Release یا Administration

برنامه Pull Request، Merge، Publish یا Deploy انجام نمی‌دهد.

## فعال‌سازی ورود مدیریتی

به‌صورت پیش‌فرض برای اجرای محلی خاموش است. در محیط استقرار، مقادیر را با Environment Variable تنظیم کنید:

```powershell
$env:Admin__Enabled="true"
$env:Admin__Username="admin"
$env:Admin__Password="use-a-long-random-secret"
dotnet run --project src/TicketAutomation.Web
```

در محیط بانکی بهتر است برنامه پشت Reverse Proxy و SSO سازمان قرار گیرد و HTTPS اجباری باشد.

## نکات امنیتی

- متن تیکت به‌عنوان داده غیرقابل‌اعتماد داخل مرز مشخص Prompt قرار می‌گیرد.
- Connection String با ASP.NET Core Data Protection رمزگذاری می‌شود.
- نام جدول و ستون‌ها Validate و Quote می‌شوند؛ مقدار وضعیت Parameterized است.
- Branch پیش از Push دوباره بررسی می‌شود و باید با `ai/` آغاز شود.
- Tokenهای رایج Git از محیط Agent حذف می‌شوند؛ Remote فقط بعد از اتمام Agent توسط میزبان برگردانده می‌شود.
- اگر Agent خودش Commit ایجاد کند Job رد می‌شود؛ Commit و Push فقط در لایه Orchestrator انجام می‌شوند.
- Agent روی Workspace جدا اجرا می‌شود، اما ایزولاسیون سیستم‌عامل باید توسط Container/VM یا حساب سرویس محدود Runner تکمیل شود.
- در Runner هیچ Credential مربوط به Production قرار ندهید.
- پوشه `data-protection-keys` را همراه دیتابیس پشتیبان بگیرید؛ بدون کلیدها Connection String قابل بازیابی نیست.

## ساختار پروژه

```text
src/TicketAutomation.Web/
  Controllers/       پنل، Jobها، تنظیمات و ورود
  Data/              SQLite داخلی
  Models/            تنظیمات و وضعیت Job
  Services/          Polling، Agentها، Git و پردازش
  Views/             رابط فارسی RTL
tests/TicketAutomation.Tests/
```
