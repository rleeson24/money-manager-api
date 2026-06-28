# Azure AD (Microsoft Entra ID) Setup

One-time configuration in the Azure portal before running Money Manager with authentication.

## 1. App registration

1. Open [Microsoft Entra admin center](https://entra.microsoft.com) → **App registrations** → **New registration**.
2. Name: `Money Manager` (or your preferred name).
3. Supported account types: choose based on who may sign in (single tenant is typical for personal/family use).
4. Redirect URI:
   - Platform: **Single-page application**
   - URI: `https://localhost:5173` (Aspire / Vite dev port; avoid 6454–6553 on Windows — reserved by Hyper-V)
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
- Add that redirect URI in Azure AD.
- `AllowedOrigins` must include `https://localhost:5173`.
- Azure AD values can live in user secrets or `appsettings.Development.json` / `appsettings.Local.json`.

## 6. User identity and data

Each signed-in user is scoped by their Azure AD **object ID** (`oid` claim), stored as `UserId` on expense rows. Data from the Aspire seed user (`11111111-1111-1111-1111-111111111111`) is only used for unauthenticated Aspire dev requests without a token.

## 7. Troubleshooting 401 errors

Check API logs for `Azure AD JWT authentication failed.` Common causes:

| Symptom | Fix |
|---------|-----|
| `issuer ... is invalid` | Remove `AzureAd:Issuer` from user secrets; ensure `AllowWebApiCallsWithMultipleIssuers` is true |
| `audience ... is invalid` | Set `AzureAd:Audience` to `api://{client-id}` in user secrets |
| Token acquired but API still 401 | Restart API after changing secrets; confirm `Authorization: Bearer` header is sent |
| Client redirects to login repeatedly | Confirm redirect URI in Azure matches browser URL exactly |

Health checks remain anonymous in all environments. For platforms that probe frequently (App Service, load balancers), use `/alive` or `/health/live` so an auto-pause SQL database is not woken on every probe. Use `/health` or `/health/ready` for readiness; `/health/db` verifies database connectivity explicitly.
