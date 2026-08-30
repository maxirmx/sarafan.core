<!--
Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting)
All rights reserved.
This file is a part of the Sarafan application
-->

# Agent Guidelines for Sarafan Project

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

**Version:** 1.2

**Last Updated:** 2026-08-30

**Maintained by:** Development Team
