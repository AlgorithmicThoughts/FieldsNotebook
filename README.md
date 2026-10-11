# Field Notebook

Field Notebook helps construction crews report blockers, track progress, and access site documents. It reduces manual reporting and keeps foremen and superintendents informed about issues delaying work.

The project includes a web application, an ASP.NET Core API, and a native iPad application. The web reporting flow uses **Microsoft Cognitive Services**, while iOS uses **Apple Speech and Apple’s on-device LLM** to turn spoken reports into structured blocker details.

## The problem it solves

A missing delivery, broken crane, or pending inspection can stop several crews at once. When reports are scattered across conversations and handwritten notes, teams lose track of who owns the issue, which work is affected, and whether work can resume.

Field Notebook brings those details into a project workspace so teams can report an issue, follow its progress, and review its impact.

## Features

| Area | What it does |
| --- | --- |
| Home | Shows project activity, blocker summaries, and upcoming work. |
| Report blocker | Captures a description, reason, responsible crew, affected crews and work, photos, a site pin, and people to notify. |
| Tracking | Lets teams review blockers, their status, and the next action in the resolution workflow. |
| Documents | Provides access to safety PDFs, schedules, and site plans. |
| Site map | Displays the site layout and supports locating a reported blocker. |
| Upcoming | Shows planned work and dependencies that may prevent it from starting. |
| Reports | Summarizes delays across selected projects and dates. |
| Notifications and settings | Helps users review updates and manage account and notification preferences. |

The iPad interface supports **landscape orientation only** and uses a light appearance.

## Speech-assisted reporting

1. Open **Report Blocker** and start recording.
2. Describe what happened and explicitly name the responsible crew when known.
3. Stop the recording and select **Send**.
4. Review the suggested title, description, responsible crew, and reason before submitting.

**Web:** The browser records audio and sends it to the API. Microsoft Cognitive Services Speech transcribes the recording, and the API maps the transcript to the project’s report fields.

**iOS:** Apple Speech transcribes the recording on the device. Apple Intelligence, through the Foundation Models framework, suggests the report fields. When the language model is unavailable, an explicit-phrase fallback fills only recognizable details. Apple Intelligence requires a supported device with its model available.

Other fields, including affected work, location, and recipients, are selected manually. Users can cancel a recording or edit the suggestions.

For example, select **Omaha Fabrication** and say:

> We can’t pour the slab today. The rebar delivery never arrived, and Crew C was supposed to order it.

The expected suggestions include **Crew C** as the responsible crew and **Missing material** as the reason. The suggested title may vary.

## Technology and structure

| Part | Technology | Location |
| --- | --- | --- |
| Web | React 19, TypeScript, Vite, Tailwind CSS | [Ui/fieldsNoteBook](Ui/fieldsNoteBook) |
| API | ASP.NET Core 10, Entity Framework Core, SQL Server | [Api/FieldsNotebook](Api/FieldsNotebook) |
| Cloud services | Microsoft Cognitive Services Speech and Azure Blob Storage | API utilities |
| iPad | SwiftUI, MVVM, FactoryKit, Apple Speech, Foundation Models, PDFKit | [Mobile/iOS/FieldsNotebook](Mobile/iOS/FieldsNotebook) |

The API separates controllers, data contracts, managers, business-rule engines, utilities, and database accessors. The iPad app keeps views in `01 View`, reusable UI in `01 View/Components`, models in `02 Data Contracts`, view models in `03 ViewModels`, and services in `04 Services`. FactoryKit provides dependency injection.

## Run locally

### Requirements

- Node.js 22.12 or later and npm for the web application.
- .NET 10 SDK and access to SQL Server for the API.
- An Azure Speech resource for web transcription and Azure Blob Storage for report photos.
- macOS with Xcode 27 and an iPad or simulator running iPadOS 27 or later for the native app.

### API

The API expects an existing Field Notebook database with the project’s schema and data. The SQL creation and seed script is not included in this repository.

From the repository root, configure local values with .NET user secrets:

```sh
cd Api/FieldsNotebook
dotnet user-secrets set "ConnectionStrings:FieldNotebook" "<SQL Server connection string>"
dotnet user-secrets set "Speech:Key" "<Azure Speech key>"
dotnet user-secrets set "Speech:Region" "<Azure Speech region>"
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<Azure Storage connection string>"
dotnet run --launch-profile http
```

The API runs at `http://localhost:5240`. Its development Swagger UI is available at `http://localhost:5240/swagger`. Speech defaults to `en-US`, and the photo container defaults to `report-delays`.

Keep service keys and database credentials in user secrets or environment variables.

### Web application

In another terminal, from the repository root:

```sh
cd Ui/fieldsNoteBook
npm ci
npm run dev
```

Open `http://localhost:5173`. The development configuration points `VITE_API_BASE_URL` to `http://localhost:5240`, and the API allows the web origin `http://localhost:5173`.

For the development sign-in, use a user ID from `1` through `8` with password `admin`. The API uses the `X-Demo-User-Id` header for local identity; production authentication is not configured.

### iPad application

Open [FieldsNotebook.xcodeproj](Mobile/iOS/FieldsNotebook/FieldsNotebook.xcodeproj) in Xcode, select the **FieldsNotebook** scheme, and choose an iPad simulator or connected iPad. For a physical device, configure your development team and a matching provisioning profile under **Signing & Capabilities**, then run the app.

Sign in with user ID `admin` and password `admin`. Use the app in landscape orientation and allow microphone access when testing speech reporting.

## Development commands

From `Ui/fieldsNoteBook`:

```sh
npm run build
npm run lint
npm run preview
```

From the repository root, build the API solution:

```sh
dotnet build Api/FieldsNotebook/FieldsNotebook.slnx
```
