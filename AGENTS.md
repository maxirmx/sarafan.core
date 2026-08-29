# Agent Guidelines for Sarafan Project

## Code Standards and Requirements

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

**Version:** 1.0  
**Last Updated:** 2026  
**Maintained by:** Development Team
