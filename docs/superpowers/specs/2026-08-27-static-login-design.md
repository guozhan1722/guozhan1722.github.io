# Static Login Design

## Purpose

Add a simple shared-credential login screen to the existing Blazor WebAssembly site while retaining its current static GitHub Pages deployment.

This feature is a casual access barrier only. Because all application files execute in and are downloaded by the browser, a determined visitor can read the configured credentials or bypass the client-side checks. The design does not claim to provide secure authentication or authorization.

## Credential Configuration

The shared username and password will be stored as plain text in `MyGithubPage/wwwroot/login-settings.json`:

```json
{
  "username": "wccac",
  "password": "123456"
}
```

The application will request this file with cache-busting behavior so a newly deployed configuration is used after the page is refreshed. Changing the file and redeploying the site changes the accepted credentials without requiring a source-code change.

The application will derive a fingerprint from the current configuration. A browser session is valid only when its stored fingerprint matches the current configuration, so changing either configured value invalidates sessions after the site is refreshed.

## Components

- `LoginSettings` models and validates the two required configuration values.
- `LoginSettingsService` loads the configuration and makes it available to authentication code.
- `StaticAuthenticationStateProvider` owns the signed-in state and persists only the configuration fingerprint in browser session storage. It never persists the password.
- A small JavaScript session-storage wrapper provides storage access from Blazor.
- `Login.razor` presents the username/password form and a generic invalid-credentials error.
- `App.razor` uses Blazor authorization routing. Unauthenticated navigation to protected routes is redirected to `/login`.
- The main navigation displays a logout action for signed-in users.

## Data Flow

On application startup, the credential configuration is fetched from the deployed static JSON file. The authentication provider compares its fingerprint with the value in session storage. A match restores the signed-in state for the current browser session; a missing or different value leaves the visitor signed out.

When the visitor submits the login form, the entered values are compared exactly with the configured username and password. Success stores the current configuration fingerprint in session storage, updates authentication state, and navigates to the original requested page or the home page. Failure clears the password field and shows `Invalid username or password.`

Logging out removes the session-storage value, updates authentication state, and navigates to `/login`. Closing the browser session also removes the sign-in state because persistent local storage is not used.

## Routing and User Experience

All existing content routes require authentication. `/login` remains anonymous. When a protected route redirects to login, the intended local route is carried as a return URL and used after successful login. Return URLs are accepted only when they are local application paths to prevent unsafe redirects.

The form will use the site's existing Bootstrap styling, include labels and appropriate autocomplete attributes, disable submission while processing, and support keyboard submission. Missing settings or a failed settings request will show a configuration error rather than accepting any credentials.

## Error Handling

- Incorrect values produce one generic error so the page does not disclose which field differed.
- Missing or blank configuration values prevent login and produce a configuration error.
- Session-storage failures leave the user signed out and present a useful error instead of silently granting access.
- Invalid return URLs fall back to the home page.

## Testing and Verification

Automated tests will cover configuration loading and validation, successful and failed credential checks, session restoration, session invalidation after a credential change, logout, and safe return-URL handling.

The implementation will also be verified with a release build and a browser smoke test covering redirect to login, rejection of incorrect credentials, successful login with `wccac` / `123456`, access to protected pages, logout, and use of updated values from `login-settings.json`.

## Deployment

The existing GitHub Pages workflow remains in place. Credential changes take effect only after the edited configuration file is committed, deployed, and the visitor refreshes the application. No SQLite database, backend, or external service is introduced.
