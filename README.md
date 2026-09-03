<!-- Copyright (C) 2026 Maxim [maxirmx] Samsonov (www.sw.consulting) -->
<!-- All rights reserved. -->
<!-- This file is a part of Sarafan application -->

# Sarafan Core

> Customer-facing purchasing and parcel-forwarding services with a trusted **Сарафан** brand identity.

**Status:** product definition and first-demo plan

## Product vision

Sarafan Core will provide and progressively automate international assisted-purchasing and parcel-forwarding services similar in scope to [Qwintry](https://qwintry.com/). A customer will be able to identify a product in an online shop, send its link to Sarafan, complete the information required for purchase and delivery, and follow the resulting order.

The product is a sibling of **Logibooks**. It should reuse Logibooks engineering conventions and shared capabilities where that reduces cost, while keeping a separate customer-facing product boundary.

Longer term, the platform may cover:

- assisted purchasing from a product link;
- personal warehouse addresses;
- receipt and inspection of incoming parcels;
- storage and parcel consolidation;
- delivery quotations and payments;
- customs information and declarations;
- international shipment creation and tracking;
- customer notifications and support.

The first demo intentionally implements only the smallest complete customer journey.

## Brand direction

The working brand association is **Сарафан**, drawing on _«сарафанное радио»_: trusted recommendations passed between people.

Possible product expressions include **Сарафан Доставка** and **Сарафан Global**. A possible positioning line is:

> Покупки по всему миру — просто передайте ссылку.

The final name, trademark position, domains, and relationship to the existing Сарафан brand architecture remain to be confirmed.

## Product boundaries

Sarafan Core should be treated as a sibling of Logibooks rather than as a consumer feature hidden inside it.

- Reuse the proven application stack, deployment conventions, monitoring, and UI foundations where practical.
- Keep the consumer application and its data boundary separate.
- Reuse identity infrastructure only if it supports phone-first customer accounts cleanly.
- Integrate with Logibooks through explicit interfaces for customers, orders, shipments, payments, and accounting events.
- Do not couple the first demo to unfinished warehouse, carrier, customs, or payment integrations.

## First demo

### Goal

Demonstrate one complete journey:

> Phone registration → product link → full customer details → submitted order → visible order status

### 1. Preliminary registration

The customer:

1. Enters a phone number.
2. Accepts the required terms and personal-data consent.
3. Enters an SMS verification code.
4. Receives an authenticated preliminary account.

For this demo, no SMS is delivered. The verification code is the last four digits of the
normalized phone number in every environment, including Production.

Expected behaviour:

- normalize and validate the phone number;
- apply basic rate limiting even though SMS is stubbed;
- reject codes that do not match the phone number's last four digits;
- create an authenticated session after successful verification;
- preserve the customer's draft work between sessions.

### 2. Product request

An authenticated preliminary customer can paste a product-page URL and create a draft request.

Initial fields:

- product URL — required;
- quantity — required, default `1`;
- product name — optional;
- variant, size, or color — optional;
- expected price and currency — optional;
- customer comment — optional.

The demo should save the URL and customer-entered information. Automatic extraction of product details should not be a dependency: many stores block or render unreliable automated parsing. Metadata extraction can be introduced later as a best-effort enhancement.

### 3. Full registration

Before submitting the first order, the customer completes the profile required for fulfilment.

Initial fields:

- legal name;
- phone number;
- email;
- delivery address;
- INN;
- date of birth, if operationally required;
- passport or identity fields only if genuinely required by delivery or customs processes;
- required consent confirmations.

Exact identity fields, retention periods, access controls, and consent wording must be confirmed with operations and legal review before production use.

### 4. Review and submission

The customer sees a final review containing:

- product information;
- quantities and options;
- customer contact information;
- delivery address;
- accepted terms.

The customer can return to either section, correct it, and submit the order. Submission assigns a human-readable number such as **`SRF-000123`** and records an immutable creation timestamp.

### 5. Orders

The customer can open **My orders** and see:

- order number;
- creation date;
- store or product domain;
- product summary;
- expected amount, when supplied;
- current status;
- most recent status update.

An order-details view shows the original product link, customer comment, delivery address, and status history.

### 6. Minimal operator capability

A small protected operator view makes the customer demo operationally complete. An operator can:

- list and search submitted orders;
- open the customer and product details;
- update an order status;
- add an internal note;
- add a customer-visible comment.

This is not intended to be a complete back office.

## Order status model

| Status | Meaning in the first demo |
| --- | --- |
| `draft` | Saved by the customer but not submitted |
| `submitted` | Submitted and awaiting operator attention |
| `under_review` | Being checked by an operator |
| `awaiting_payment` | Reviewed and waiting for payment; simulated in the demo |
| `purchased` | Purchased from the store; future workflow |
| `at_warehouse` | Received at a warehouse; future workflow |
| `in_transit` | International shipment is moving; future workflow |
| `delivered` | Delivered to the customer; future workflow |
| `cancelled` | Cancelled by an authorized actor with a recorded reason |

Only `draft`, `submitted`, `under_review`, and optionally `awaiting_payment` require real behaviour in the first demo. Later states may appear in seeded demonstration data.

Every transition should record its timestamp and actor. Invalid transitions should be rejected.

## Principal data objects

- **Customer** — phone-based identity and account state.
- **Customer profile** — legal, contact, tax, and delivery information.
- **Address** — structured delivery address owned by a customer.
- **Order** — customer request, number, status, totals, and timestamps.
- **Order item** — product URL, quantity, options, price, currency, and comment.
- **Order status event** — transition, actor, timestamp, and visible comment.
- **Operator note** — internal information that is never exposed to the customer.
- **Consent record** — accepted document version and timestamp.

Sensitive profile data should not be copied unnecessarily into logs, analytics, or status events.

## Suggested screens

### Customer

1. Welcome / phone entry
2. SMS-code verification
3. Product-link entry
4. Draft product details
5. Full-registration form
6. Review and submit
7. Order confirmation
8. My orders
9. Order details
10. Profile and delivery address

### Operator

1. Sign-in
2. Order queue
3. Order details
4. Status and comment update

Mobile layouts are first-class because product links are likely to be shared from a phone.

## Architecture principles

- Begin with one deployable application unless existing Logibooks boundaries make a separate service clearly cheaper.
- Keep domain modules explicit: identity, customer profile, ordering, and operator workflow.
- Keep phone verification behind a provider interface; the demo implementation uses the phone number's last four digits in every environment.
- Treat order status changes as auditable events rather than overwriting status without history.
- Use role-based access for customers and operators.
- Encrypt transport and stored sensitive data using the platform's established facilities.
- Keep personal information out of application logs.
- Add API versioning only when an external consumer exists; avoid speculative infrastructure.

## Delivery plan

Durations are indicative and assume that the Logibooks stack and reusable components are already understood.

| Phase | Duration | Deliverable |
| --- | ---: | --- |
| 0. Product definition | 2–3 days | Confirm operational fields, first shipping corridor, reusable Logibooks components, wireframes, and status transitions |
| 1. Walking skeleton | 3–4 days | Application shell, visual identity, phone authentication with the number's last four digits, sessions, and preliminary profiles |
| 2. Customer journey | 5–7 days | Product-link drafts, full registration, review, submission, order list, and order details |
| 3. Operator demo | 2–3 days | Protected order queue, status updates, comments, and seeded demo orders |
| 4. Hardening | 2–3 days | End-to-end tests, access-control review, responsive QA, demo deployment, and a repeatable demo script |

Expected effort for the primitive demo: approximately **2–3 weeks**, depending on how much can be reused from Logibooks.

## Acceptance criteria

The first demo is complete when:

- [ ] A new customer can register with a phone number and its last four digits as the code.
- [ ] Codes other than the phone number's last four digits are rejected.
- [ ] The customer can paste and save a valid product URL.
- [ ] A saved draft survives logout and a later sign-in.
- [ ] The customer can enter all required personal and delivery information.
- [ ] The customer can review and correct information before submission.
- [ ] Submission creates an order with a unique readable number.
- [ ] The order appears in **My orders**.
- [ ] The customer can open the order and see its status history.
- [ ] An operator can locate the order and change its status.
- [ ] The customer sees the updated status.
- [ ] Internal operator notes are never visible to the customer.
- [ ] Customer and operator access controls are covered by automated tests.
- [ ] The complete journey works on a representative mobile viewport.

## Explicitly outside the first demo

- real SMS delivery;
- production identity verification;
- payments and refunds;
- automatic purchasing from stores;
- dependable product-page scraping;
- personal warehouse-address allocation;
- parcel reception and inspection;
- storage and consolidation;
- customs declarations;
- carrier booking and live tracking;
- production notifications;
- customer-support tooling;
- native mobile applications.

## Roadmap after the demo

1. Validate the workflow with operators and a small group of customers.
2. Replace the demo phone-suffix code with a real provider and abuse controls.
3. Introduce quotations, payment, and assisted-purchase operations.
4. Add warehouse addresses, incoming parcels, inspection, and storage.
5. Add consolidation, customs data, shipping selection, and carrier integration.
6. Add notifications, customer support, refunds, reporting, and operational analytics.
7. Add referrals and recommendation mechanics that express the Сарафан brand promise.

## Decisions required before implementation

- Which source and destination countries form the first shipping corridor?
- Is the first demo assisted purchasing only, parcel forwarding only, or both?
- Which personal and identity fields are operationally mandatory?
- Which Logibooks repositories, services, and design components can be reused?
- Is an order initially a non-binding request, a quotation request, or a purchase commitment?
- Who may perform each order-status transition?
- Which currency and language are primary?
- What is the approved customer-facing product name?

These decisions should be recorded before Phase 1 finishes so that the demo does not encode temporary assumptions as permanent business rules.

## Core service

[![ci](https://github.com/maxirmx/sarafan.core/actions/workflows/ci.yml/badge.svg)](https://github.com/maxirmx/sarafan.core/actions/workflows/ci.yml)
[![publish](https://github.com/maxirmx/sarafan.core/actions/workflows/publish.yml/badge.svg)](https://github.com/maxirmx/sarafan.core/actions/workflows/publish.yml)

ASP.NET Core identity and customer-profile service for Sarafan. It targets .NET 10 LTS, uses PostgreSQL from the first migration, and is packaged as a Linux container.

## Prerequisites

- .NET 10 SDK
- Docker with Docker Compose

## Local development

```bash
docker compose up -d --wait db adminer
dotnet restore Sarafan.sln
dotnet run --project src/Sarafan.Core/Sarafan.Core.csproj
```

The development connection uses PostgreSQL on host port `5433`. The machine-specific Compose override stores database data in `R:/Projects/30.Projects/sarafan/.runtime/postgres`. Adminer is available only in the development compose stack at <http://localhost:8088>; use server `db` from inside Compose or `host.docker.internal:5433` when connecting through the browser-hosted Adminer container.

The v1 API status endpoint is <http://localhost:5080/api/v1/status/status> when the development launch profile is used. Registration and login use the normalized phone number's last four digits as the verification code in Development, Testing, and Production.

### Identity endpoints

| Method | Endpoint | Purpose |
| --- | --- | --- |
| `POST` | `/api/v1/auth/code/request` | Request a registration or login code |
| `POST` | `/api/v1/auth/code/verify` | Register or log in and issue an access/refresh session |
| `POST` | `/api/v1/auth/refresh` | Rotate the HttpOnly refresh cookie and issue a new access token |
| `POST` | `/api/v1/auth/logout` | Revoke the refresh-token family and clear the cookie |
| `GET`, `PUT` | `/api/v1/customers/me` | Read or update the authenticated customer profile |
| `GET`, `PUT`, `DELETE` | `/api/v1/customers/me/photo` | Manage a JPEG, PNG, or WebP profile photo up to 5 MiB |

### API error contract (0.0.4)

Version 0.0.4 replaces the demo's ad-hoc error responses with RFC 9457 Problem Details. API errors use `application/problem+json`; `type` is the canonical machine-readable identifier, while `code` remains an explicit compatibility extension. Clients should branch on `type`, display the safe Russian `detail`, and retain the structured `errors` extension for field-level validation. Clients must not make decisions by parsing localized `title` or `detail` text.

## Docker

```bash
docker compose up --build
```

The containerized API is available at <http://localhost:8080/api/v1/status/status>, PostgreSQL at `127.0.0.1:5433`, and Adminer at `127.0.0.1:8088` by default.

`Sarafan.sln` also contains a `docker-compose` project for Visual Studio, matching the Docker Compose workflow used by Logibooks Core. Select the **docker-compose** startup project to build and start the development stack; no `.env` file is required for development. The `sarafan.env.example` settings, including the required durable host paths, are used by cloud deployment.

## Verification

```bash
dotnet build Sarafan.sln --configuration Release
# Start PostgreSQL first; tests create and remove an isolated database.
SARAFAN_TEST_POSTGRES='Host=127.0.0.1;Port=5433;Database=postgres;Username=postgres;Password=postgres' \
  dotnet test Sarafan.sln --configuration Release --collect:"XPlat Code Coverage"
docker compose --profile test run --rm --build tests
docker compose up -d --build --wait
curl --fail http://localhost:8080/api/v1/status/status
docker compose down
```

## Observability

Sarafan Core writes one human-readable record per line to stdout. Each line uses an RFC 3339 UTC timestamp and includes severity, a stable dotted event name, W3C trace/span identifiers when present, and a fixed English diagnostic message. Application code continues to use `ILogger<T>` through the source-generated Sarafan event catalogue; structured fields follow the OpenTelemetry Logs Data Model and semantic conventions.

ASP.NET Core accepts and propagates the W3C `traceparent` header. RFC 9457 responses expose the same 32-character lowercase hexadecimal trace ID in `traceId` and `instance=urn:sarafan:problem:{traceId}`. To troubleshoot a reported failure, search Core logs or the telemetry backend for that `trace_id` value.

OTLP log and trace export is disabled unless `OTEL_LOGS_EXPORTER=otlp` or `OTEL_TRACES_EXPORTER=otlp` is configured (an `OTEL_EXPORTER_OTLP_ENDPOINT` also enables the corresponding exporters when their selectors are absent). Standard `OTEL_EXPORTER_OTLP_*` variables configure protocol, endpoint, headers, timeout, and batching. Export failure is isolated from request handling.

Do not enable both OTLP export and infrastructure collection of stdout into the same backend: choose one delivery path to avoid duplicate records. Set the signal exporter to `none` when stdout is collected. Detailed Debug events remain disabled in Production by the normal `Logging:LogLevel` configuration.

Logs intentionally exclude raw URLs and query strings, HTTP headers and bodies, tokens/cookies/codes, personal data, localized Problem Details text, connection strings, SQL values, and client IP addresses. New events must use the stable catalogue and allowlisted low-cardinality attributes described in `AGENTS.md`.

## Cloud deployment

The cloud stack contains the UI and Sarafan Core without publishing either
container directly on the host. Choose exactly one deployment overlay:

- `edge` attaches the UI to the external `sw-consulting-edge` network using the
  alias `sarafan-ui`;
- `production` starts a dedicated TLS edge on ports 80 and 443 for
  `sarafan.sw.consulting`.

```bash
cp sarafan.env.example sarafan.env
chmod 600 sarafan.env
chmod +x scripts/bootstrap-cloud.sh scripts/update-cloud.sh

# Shared server
scripts/bootstrap-cloud.sh edge

# Dedicated production server
scripts/bootstrap-cloud.sh production
```

For a dedicated server, place `s.crt` and `s.key` in
`/srv/sarafan/certificate` (or set `SARAFAN_CERTIFICATE_DIR`). The certificate
must cover `sarafan.sw.consulting`. For the shared server, start the
`sw-consulting-edge` project before Sarafan so the external Docker network
exists.

Update the selected deployment with `scripts/update-cloud.sh edge` or
`scripts/update-cloud.sh production`. UI and Core image tags are independent so
the two repositories do not need synchronized release numbers.

The production stack starts `ghcr.io/sw-consulting/db-backup:latest`, matching Logibooks' `tooling.db-backup` setup. Configure durable `SARAFAN_BACKUP_DATA_DIR` and `SARAFAN_BACKUP_LOG_DIR` host paths plus the retention period in `sarafan.env`; bootstrap validates all database and backup paths before deployment.

Production migrations run in a dedicated one-shot `migrate` service before the API starts. The long-running API has startup migration disabled. Core defaults to the framework's restrictive loopback proxy trust; the cloud Compose stack explicitly trusts private Docker network ranges and processes the two proxy hops (`edge` and `ui`) used by both deployment modes. Docker assigns container addresses dynamically, so the stack does not require a custom IPAM subnet. Other deployments must configure `ForwardedHeaders__KnownNetworks__N` or `ForwardedHeaders__KnownProxies__N` for their own trusted proxy boundary.
