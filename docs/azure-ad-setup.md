# Azure AD (Microsoft Entra ID) Setup

One-time configuration in the Azure portal before running Money Manager with authentication.

## 1. App registration

1. Open [Microsoft Entra admin center](https://entra.microsoft.com) → **App registrations** → **New registration**.
2. Name: `Money Manager` (or your preferred name).
3. Supported account types: choose based on who may sign in (single tenant is typical for personal/family use).
4. Redirect URI (see [§8 Redirect URI and Cloudflare Pages](#8-redirect-uri-and-cloudflare-pages) for the full rules):
   - Platform: **Single-page application**
   - URI: `https://localhost:5173/auth/redirect`
5. Register and note the **Application (client) ID** and **Directory (tenant) ID**.

## 2. Expose an API scope

1. In the app registration → **Expose an API**.
2. Set **Application ID URI** to `api://{client-id}` (use your Application ID).
3. **Add a scope**:
   - Scope name: `access_as_user`
   - Who can consent: **Admins and users**
   - Admin consent display name: `Access Money Manager API`
   - Admin consent description: `Allows the app to access the Money Manager API on behalf of the signed-in user.`
   - State: **Enabled**

The full scope value is: `api://{client-id}/access_as_user`

## 3. API permissions

Under **API permissions** → **Add a permission**:

| API | Permission | Type |
|-----|------------|------|
| Microsoft Graph | `User.Read` | Delegated |
| Money Manager (your app) | `access_as_user` | Delegated |

Grant admin consent if your tenant requires it.

## 4. Configuration values

### API (`appsettings.*.json`, user secrets, or Key Vault)

| Key | Example |
|-----|---------|
| `AzureAd:TenantId` | `{tenant-guid}` |
| `AzureAd:ClientId` | `{client-id}` |
| `AzureAd:Audience` | `api://{client-id}` |
| `AzureAd:AllowWebApiCallsWithMultipleIssuers` | `true` (accepts both v1 and v2 token issuers) |
| `AllowedOrigins` | `https://localhost:5173` (semicolon-separated if you add more) |

Do **not** set `AzureAd:Issuer` or leave `AzureAd:Audience` empty in user secrets — those override `appsettings.*.json` and cause `401` token validation failures.

Verify user secrets:

```bash
dotnet user-secrets list --project src/main/MoneyManager.API/MoneyManager.API.csproj
```

Remove stale entries if present:

```bash
dotnet user-secrets remove "AzureAd:Issuer" --project src/main/MoneyManager.API/MoneyManager.API.csproj
dotnet user-secrets remove "AzureAd:Authority" --project src/main/MoneyManager.API/MoneyManager.API.csproj
```

### Client (`.env.local`)

```
VITE_AZURE_CLIENT_ID={client-id}
VITE_AZURE_TENANT_ID={tenant-id}
VITE_AZURE_API_SCOPE=api://{client-id}/access_as_user
VITE_USE_API=true
VITE_API_URL=https://localhost:7016
```

If `VITE_USE_API=true` but Azure variables are missing, the client shows a configuration error instead of making unauthenticated API calls.

## 5. Local development with Aspire

When running via `MoneyManager.AppHost`:

- Client runs at `https://localhost:5173` (fixed in AppHost).
- Register **`https://localhost:5173/auth/redirect`** as a SPA redirect URI in Azure AD (must match `msalConfig.ts` exactly).
- `AllowedOrigins` must include `https://localhost:5173`.
- Azure AD values can live in user secrets or `appsettings.Development.json` / `appsettings.Local.json`.

## 6. User identity and data

Each signed-in user is scoped by their Azure AD **object ID** (`oid` claim), stored as `UserId` on expense rows. Data from the Aspire seed user (`11111111-1111-1111-1111-111111111111`) is only used for unauthenticated Aspire dev requests without a token.

## 7. Troubleshooting authentication

### API 401 errors

Check API logs for `Azure AD JWT authentication failed.` Common causes:

| Symptom | Fix |
|---------|-----|
| `issuer ... is invalid` | Remove `AzureAd:Issuer` from user secrets; ensure `AllowWebApiCallsWithMultipleIssuers` is true |
| `audience ... is invalid` | Set `AzureAd:Audience` to `api://{client-id}` in user secrets |
| Token acquired but API still 401 | Restart API after changing secrets; confirm `Authorization: Bearer` header is sent |

### Client redirect / login errors

| Symptom | Fix |
|---------|-----|
| Blank page after Microsoft login | See [§8](#8-redirect-uri-and-cloudflare-pages); check browser console for CSP `connect-src` errors on `/auth/redirect` |
| `ERR_TOO_MANY_REDIRECTS` on `/auth/redirect` | A **301/308** rule is fighting Cloudflare's URL normalization — use **200 rewrites only** in `_redirects` (see §8) |
| `post_request_failed` / CSP blocks `login.microsoftonline.com` | Path-specific `_headers` for `/auth/redirect` must include `connect-src https://login.microsoftonline.com` |
| Client redirects to login repeatedly | Confirm Azure redirect URI matches `msalConfig.ts` exactly: `{origin}/auth/redirect` (no `.html`, no trailing slash) |
| `timed_out` / silent auth fails | Ensure `/auth/redirect` serves the minimal MSAL handler (`auth/redirect/index.html`), **not** the React app — check `_redirects` |

## 8. Redirect URI and Cloudflare Pages

This section documents the **final, locked-in** auth callback design. Do not alternate between `/auth/redirect`, `/auth/redirect/`, and `/auth/redirect.html` — that caused infinite redirect loops on Cloudflare Pages.

### Architecture

MSAL uses a **dedicated blank redirect page**, not the React app:

| Flow | Path | Page |
|------|------|------|
| Interactive login | `/auth/redirect?code=...` | Minimal MSAL handler (`auth/redirect/index.html` via Vite MPA build) |
| Silent token refresh | `/auth/redirect` (hidden iframe) | Same minimal page — must **not** load the React SPA |
| Normal app routes | `/`, `/expenses`, etc. | React SPA (`index.html`) |

After interactive login, `src/auth-redirect.ts` calls `handleRedirectPromise()` on the redirect page, then navigates to `/`.

The redirect URI in code (`money-manager-client/src/auth/msalConfig.ts`):

```typescript
const redirectUri = `${window.location.origin}/auth/redirect`;
```

### Azure AD redirect URIs to register

Register **only** these SPA redirect URIs (platform: Single-page application):

| Environment | Redirect URI |
|-------------|--------------|
| Local (Aspire / Vite) | `https://localhost:5173/auth/redirect` |
| Production | `https://mm.leesontechnologies.com/auth/redirect` |
| Cloudflare Pages preview (optional) | `https://money-manager-client.pages.dev/auth/redirect` |

**Do not register** these — they cause confusion or redirect loops:

- `{origin}/auth/redirect.html` — Cloudflare 308-strips `.html` back to `/auth/redirect`, creating loops if combined with rewrite rules
- `{origin}` (root) — the React app is not an OAuth callback handler
- Both `/auth/redirect` and `/auth/redirect.html` for the same environment

Azure redirect URIs must match the code **exactly** (no trailing slash, no `.html`).

### Cloudflare Pages `_redirects`

File: `money-manager-client/public/_redirects`

```
# Auth callback: 200 rewrite only — never 301/302/308 between /auth/redirect and
# /auth/redirect.html (Cloudflare strips .html with 308, causing infinite loops).
/auth/redirect   /auth/redirect/index.html  200
/auth/redirect/  /auth/redirect/index.html  200
/*               /index.html                200
```

Rules:

- **`200` rewrites only** for the auth callback — never `301`/`302`/`308` between `/auth/redirect` and `/auth/redirect.html`
- Both `/auth/redirect` and `/auth/redirect/` must serve the handler (Cloudflare may 308 once to add a trailing slash; that single redirect is OK)
- The catch-all `/* → /index.html` must **not** apply to `/auth/redirect` — the explicit rules above take precedence

Build output: Vite MPA entry `auth/redirect/index.html` → `dist/auth/redirect/index.html`.

### Cloudflare Pages `_headers`

The auth callback paths need **relaxed framing** (for MSAL silent auth iframes) and **`connect-src` for Microsoft** (for the token exchange POST):

File: `money-manager-client/public/_headers`

```
/auth/redirect
  X-Frame-Options: SAMEORIGIN
  Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' https://login.microsoftonline.com; frame-src 'self' https://login.microsoftonline.com; frame-ancestors 'self'

/auth/redirect/
  X-Frame-Options: SAMEORIGIN
  Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self' https://login.microsoftonline.com; frame-src 'self' https://login.microsoftonline.com; frame-ancestors 'self'
```

Path-specific headers **replace** the global `/*` CSP for those URLs. If `connect-src` is missing, MSAL fails with `post_request_failed` and the page stays blank.

### Verify after deploy

In a fresh private browser window, sign in and check the Network tab:

1. One request to `/auth/redirect?code=...` (or `/auth/redirect/?code=...`) with status **200**
2. No alternating **301/308** between `redirect` and `redirect.html`
3. A POST to `login.microsoftonline.com/.../oauth2/v2.0/token` with status **200**
4. Navigation to `/` with the user signed in

## 9. Health checks

Health checks remain anonymous in all environments. For platforms that probe frequently (App Service, load balancers), use `/alive` or `/health/live` so an auto-pause SQL database is not woken on every probe. Use `/health` or `/health/ready` for readiness; `/health/db` verifies database connectivity explicitly.
