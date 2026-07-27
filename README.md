# Unified Intake service

A user chooses the service and request type, gives their details, optionally
attaches supporting documents, and submits. The request — user details and files — is forwarded to a
Power Automate flow for processing.

Built as an ASP.NET Core Razor Pages application using the GOV.UK Design System
([GovUk.Frontend.AspNetCore](https://github.com/x-govuk/govuk-frontend-aspnetcore)).

## Requirements

- .NET 10.0 SDK

Front-end styles and scripts are provided by the `GovUk.Frontend.AspNetCore` NuGet package, so there is
no Node/npm build step.

## Development setup

- Run `dotnet restore` from the repository root to restore dependencies.
- Run `dotnet run` from the repository root to start the application.

The app then listens on:

- `https://localhost:7074`
- `http://localhost:5123`

Every page requires an authenticated user, so you will need the Azure AD configuration below to sign in
locally.

## Configuration

Configuration is read from `appsettings.json` / `appsettings.Development.json` and can be overridden with
user secrets (the project has a `UserSecretsId` configured) or environment variables.

| Key | Description |
|-----|-------------|
| `AzureAd:Instance` | Azure AD instance, e.g. `https://login.microsoftonline.com`. |
| `AzureAd:Domain` | The tenant domain. |
| `AzureAd:TenantId` | The Azure AD tenant (directory) ID. |
| `AzureAd:ClientId` | The app registration (client) ID used to sign users in. |
| `PowerAutomateUrl` | The Power Automate flow URL the completed request is POSTed to. |

For local development, user secrets can be set with:

```bash
dotnet user-secrets set "key" "value"
```

## Journey

The form is a linear GOV.UK "question pages" journey. Answers are held in session between pages so a
partially completed request survives navigation and validation failures.

1. **Index** – choose which service the request is for and what it is about.
2. **About you** – full name, email address, request details, whether we can contact you, and optional
   supporting documents.
3. **Check your answers** – review, then submit. Submitting POSTs the request to Power Automate.
4. **Request submitted** – confirmation with the reference number returned by the backend.

## Supporting documents

Uploads are validated server-side (the enhanced file-upload component cannot enforce these client-side):

- Up to **20 files**.
- Each file no larger than **25MB** (`AboutYouModel.MaxFileSizeMb`).
- Accepted types: PNG, JPG, PDF, DOCX, XLSX.

File contents are held in session (base64) so they are still available at the point of submission.

## Project structure

```
Pages/                  # Razor Pages (Index, AboutYou, CheckYourAnswers, RequestSubmitted, ...)
├── Helpers/            # Session accessors and SupportingDocuments store
├── Models/             # SubmissionRequest / SubmissionResponse payload records
docs/                   # Sample Power Automate request/response payloads
wwwroot/                # Static assets (site.js, site.css, images)
Dfe.Unified.Intake.Tests/   # NUnit test project
```

The payload sent to Power Automate mirrors `docs/power-automate-request.json`.

## Testing

Tests are written with NUnit (using Moq and AutoFixture) and live in `Dfe.Unified.Intake.Tests`.

```bash
dotnet test
```
