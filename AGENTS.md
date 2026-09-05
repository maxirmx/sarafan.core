<!--
Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
All rights reserved.
This file is a part of the Sarafan application
-->

# Agent Guidelines for Sarafan Project

## Specification and repository guidance

- Follow the current specification identified in the [specification README](https://github.com/sara-fan/sarafan.spec#source-of-truth). If an implementation issue conflicts with it, flag the discrepancy before implementing the affected behavior.
- In implementation PR descriptions, cite the governing specification version and section, and link the planning issue. Use any suitable format; the PR template is optional.
- When a change introduces or changes a lasting convention, API contract, domain invariant, security/privacy rule, workflow, or test pattern, update the nearest relevant `AGENTS.md` in the same PR. Keep entries concise and reusable.
- Otherwise, include `AGENTS.md: no durable change` in the PR description.
- Before editing documentation, read its current revision and preserve user-authored changes. Keep product requirements in the specification and task-specific discussion in the issue.
- Before enabling real orders or a real payment-system integration, replace and disable the predictable phone-suffix demo verification mechanism. A build/runtime environment named Production does not satisfy this requirement. Keep this release gate tracked in the [MVP delivery issue](https://github.com/sara-fan/sarafan.spec/issues/26).

## Code Standards and Requirements

### Controller Error Responses

- Represent every in-scope API error as an RFC 9457 Problem Details document and return it as `application/problem+json`.
- Use the single dedicated Sarafan problem-details model in `src/Sarafan.Core/RestModels`; do not introduce alternative error DTOs, anonymous error objects, raw error strings, or bodyless controller errors.
- Keep `type` stable and use it as the primary machine-readable problem identifier. Use `https://sarafan.sw.consulting/problems/{kebab-case-name}` while retaining the matching snake-case `code` extension for client compatibility; changing either identifier is a breaking contract change.
- Keep each problem's HTTP status in the centralized catalogue, keep the payload `status` identical to the actual response status, identify each occurrence with `urn:sarafan:problem:{traceId}`, and return Russian problem responses with `Content-Language: ru`.
- Write every user-facing `title`, `detail`, and validation message in Russian. Keep `title` short and stable for a problem type, and put occurrence-specific corrective information in `detail`.
- Define problem payloads in the centralized Sarafan problem-details factory/service. Services may signal an error condition but must not construct HTTP payloads or supply client-facing exception messages.
- Implement controller-action error paths through specifically named helpers in `src/Sarafan.Core/Controllers/SarafanControllerBase.cs`; those helpers must delegate to the centralized problem-details factory/service.
- Reuse an existing helper when it matches the response. Otherwise add a specifically named helper instead of constructing `StatusCode`, `BadRequest`, `NotFound`, `Conflict`, `Unauthorized`, `Forbid`, `Problem`, or another error result directly in a controller action.
- Route automatic model validation, authentication and authorization failures, empty error statuses, and unhandled exceptions through the same RFC 9457 contract.
- Preserve RFC 6750 semantics alongside RFC 9457: every JWT 401 challenge must include `WWW-Authenticate: Bearer`; add only the safe `error="invalid_token"` parameter for a supplied invalid token, and never expose token-validation exception details in challenge parameters.
- Never expose stack traces, database details, exception messages, credentials, tokens, or other implementation-sensitive information in a problem response.

### Test Coverage

- Maintain at least 95% patch coverage for all new or modified code.
- Add or expand tests until the changed-code coverage target is met before handing off a change.
- Keep convention tests that reject direct construction of controller error responses outside `SarafanControllerBase.cs` and reject RFC 9457 payload construction outside the centralized problem-details factory/service.

### Observability and Logging

- Emit application logs only through constructor-injected `ILogger<T>` (or a typed `ILogger<T>` resolved at the composition root) and the source-generated stable event catalogue in `src/Sarafan.Core/Observability/SarafanEvents.cs`; do not add vendor-specific loggers, string-based logger categories, ad-hoc event identifiers, interpolated log strings, or direct console output.
- Keep each event's numeric `EventId`, dotted `EventName`, severity, and fixed English human-readable message stable. Treat changes as an operational contract change and cover them with tests.
- Model records according to the OpenTelemetry Logs Data Model and use OpenTelemetry semantic-convention attribute names when defined. Use `sarafan.*` only for Sarafan-specific concepts.
- Keep the text console record human-readable and single-line, with an RFC 3339 UTC timestamp, severity, event name, W3C trace/span identifiers when available, and a meaningful message. Structured fields supplement the message and must never replace it with JSON or a code-only body.
- Propagate W3C Trace Context and correlate RFC 9457 `traceId` with `Activity.TraceId.ToHexString()` (32 lowercase hexadecimal characters). Do not use the complete `Activity.Id` as the public trace identifier.
- Log HTTP operations by low-cardinality route template, method, status, and duration only. Never log raw paths, query strings, full URLs, request/response bodies, headers, SQL parameters, localized Problem Details text, or serialized problem documents.
- Use an allowlist for log attributes. Never record credentials, tokens, cookies, verification codes, personal/customer data, free-form input, client IP addresses, exception messages, database connection strings, or other secrets. Unexpected exceptions are owned and recorded once by the centralized exception boundary.
- Keep framework Warning, Error, and Critical records visible, but pass them through the centralized privacy policy: emit a stable generic event and safe category only, and discard framework message state, attributes, and exceptions before console or OTLP output.
- Keep OTLP export optional and driven by standard `OTEL_*` configuration. Exporter or collector failure must not affect API behavior, and a deployment must choose either OTLP delivery or stdout collection to avoid duplicate ingestion.
- Add tests for every new event, severity, semantic attribute, trace-correlation path, redaction boundary, and filtering decision. New features must extend the stable catalogue rather than bypass the observability facility.

### Copyright Header Requirement

All source code files in the Sarafan project **MUST** include a copyright header at the top of the file.

#### C# Files

All `.cs` files must include the following copyright header:

```csharp
// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application
```

**Placement:** The copyright header must be the first lines in the file, before any `using` statements or namespace declarations.

**Example:**
```csharp
// Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of the Sarafan application

using System;
using System.Collections.Generic;

namespace Sarafan.Core
{
	// ... rest of the file
}
```

#### Other File Types

For other file types (XML, JSON, YAML, etc.), use the appropriate comment syntax for that language:

- **XML files (.xml, .csproj, .props):** Use `<!-- -->` comment style
- **JSON files (.json):** Cannot have comments in standard JSON; 
- **Markdown files (.md):** Use HTML comment style `<!-- -->`
- **PowerShell scripts (.ps1):** Use `#` comment style
- **Batch files (.bat, .cmd):** Use `REM` comment style

---

**Version:** 1.3

**Last Updated:** 2026-09-03

**Maintained by:** Development Team
