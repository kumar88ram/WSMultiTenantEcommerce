# Multi-tenant Store .NET MAUI App

This project delivers a multi-tenant Nazmart-style storefront built with .NET MAUI. The app ships with mock data and offline-friendly services so you can exercise the full experience (authentication, home feed, catalog, cart, checkout, order management, profile, support, and refunds) before wiring it to a production backend.

## Project structure

```
MultiTenantEcommerce.Maui/
├── App.xaml / App.xaml.cs        # Application bootstrap, theme resources, deep-link handler
├── AppShell.xaml                 # Shell navigation with tab layout and routes
├── Converters/                   # UI converters
├── Models/                       # DTOs shared across services and viewmodels
├── Services/                     # Api, auth, cart, notification helpers
├── ViewModels/                   # MVVM logic per screen
└── Views/                        # XAML pages bound to the viewmodels
```

## Local development

The app targets `net8.0-android`, `net8.0-ios`, and `net8.0-maccatalyst`. Install the [.NET MAUI workloads](https://learn.microsoft.com/dotnet/maui/get-started/installation) and run the project from Visual Studio 2022 or via CLI:

```bash
dotnet build MultiTenantEcommerce.Maui.csproj
# dotnet build -t:Run -f net8.0-android
```

Mock data is enabled by default (`ApiService.UseMockData = true`). This allows you to explore the app without a backend.

## Connecting to a backend

1. **Update the API base address** – In `MauiProgram.cs`, adjust the `HttpClient` base URI to your tenant store API domain.
2. **Disable mock mode** – Set `ApiService.UseMockData = false` after registering the service (e.g. in `MauiProgram.CreateMauiApp` or in an environment-specific build section).
3. **Authentication** – `AuthService` delegates OTP requests to `ApiService.RequestOtpAsync` and `VerifyOtpAsync`. Ensure your backend exposes `/api/auth/request-otp` and `/api/auth/verify-otp` endpoints that return the `TokenBundle` payload (`accessToken`, `refreshToken`, `expiresAt`, `tenant`). Tokens are persisted using `SecureStorage` in `TokenStorageService`.
4. **Catalog & profile endpoints** – Implement the following routes (or update `ApiService` if yours differ):
   - `GET /api/storefront/home` → `HomeFeed`
   - `GET /api/catalog/categories/{id}/products`
   - `GET /api/catalog/products/{id}`
   - `GET /api/account/wishlist`
   - `GET /api/account/profile`
   - `GET /api/account/orders` and `GET /api/account/orders/{id}`
   - `POST /api/account/refunds`
   - `GET /api/support/tickets`
   - `GET /api/account/addresses`
   - `GET /api/checkout/shipping-options`

   Adjust or extend `ApiService` with additional models to match your backend schema.
5. **Payments and deep links** – The checkout flow expects a deep-link callback using the scheme `appscheme://payment?status={status}&orderId={id}`. Hook your payment gateway return URL to call this scheme on success/failure. `App.OnAppLinkRequestReceived` publishes the result to `CheckoutViewModel` which clears the cart on success.
6. **Push notifications** – `NotificationService` currently stubs registration. Integrate with Firebase/APNs/OneSignal by extending `RegisterForPushNotificationsAsync` and wiring platform-specific handlers to call `HandleIncomingNotificationAsync`.
7. **Environment configuration** – For separate staging/production endpoints, you can register different `HttpClient` base addresses via `#if` directives or `AppSettings` injection.

## Extending the app

- **Feature flags** – Toggle tabs or modules by adjusting `AppShell.xaml` and service registrations.
- **State management** – The MVVM implementation uses `CommunityToolkit.Mvvm`. Additional observable properties or commands can be added via `[ObservableProperty]` and `[RelayCommand]` attributes.
- **Testing** – Add unit tests around services and viewmodels by injecting fake services (thanks to dependency injection registration in `MauiProgram`).

## Troubleshooting

- **OTP not delivered** – Confirm `AuthService.UseMockData` state. When connected to a backend, ensure the API responds with HTTP 200 and the expected JSON payload.
- **Secure storage on emulator** – Emulators sometimes require a device PIN/biometric setup for secure storage. Configure the emulator or swap to an in-memory implementation while developing.
- **Deep link testing** – Use platform tools (adb `am start -a android.intent.action.VIEW -d "appscheme://payment?status=success&orderId=ORD-123"`) to simulate payment callbacks.

Enjoy building your tenant-specific storefront!
