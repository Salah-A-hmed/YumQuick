*Access the Swagger UI at: `https://yumquick.tryasp.net/swagger/index.html`*

# YumQuick API 🍔🚀

YumQuick is a robust, full-featured backend API for a modern single-restaurant food delivery application. Built with **ASP.NET Core**, it provides a complete ecosystem for customers, delivery drivers, and restaurant managers.

This project demonstrates advanced backend concepts including N-Tier architecture, secure third-party payment integration, real-time communication, and push notifications.

## 🌟 Key Features

*   **Role-Based Access Control (RBAC):** Secure endpoints for Customers, Drivers, and Restaurant Managers using JWT authentication.
*   **Smart Catalog & Cart:** Manage products, categories, variants, and a persistent shopping cart.
*   **Advanced Order Management:** State-machine based order tracking (Pending -> Preparing -> Ready -> OnTheWay -> Delivered).
*   **Payment Gateway Integration:** Integrated with Stripe for secure card payments, supporting tokenized saved cards, PaymentIntent architecture, and asynchronous webhook validation.
*   **Real-Time Tracking:** SignalR integration for instant order status updates and driver location tracking.
*   **Push Notifications:** Firebase Cloud Messaging (FCM) integration for offline notifications.
*   **Customer Support Ticketing:** Built-in system for users to raise issues and managers to resolve them.
*   **Enterprise Patterns:** Implements Soft Deletion, Global Exception Handling Middleware, and Clean N-Tier Architecture.

## 🛠️ Tech Stack

*   **Framework:** .NET 8 / ASP.NET Core Web API
*   **Database:** SQL Server & Entity Framework Core
*   **Authentication:** ASP.NET Core Identity & JWT Bearer Tokens
*   **Real-Time:** SignalR
*   **Push Notifications:** Firebase Admin SDK
*   **Architecture:** N-Tier (Core, Data, Services, API)

## 🚀 Getting Started (Local Development)

### Prerequisites
*   [.NET 8 SDK](https://dotnet.microsoft.com/download)
*   SQL Server
*   Firebase Account (for push notifications)
*   Stripe Account (for payment processing and Webhooks)

### Installation

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/yourusername/YumQuick.git](https://github.com/yourusername/YumQuick.git)
    cd YumQuick
    ```
2.  **Configure Database & Secrets:**
    Update the `DefaultConnection` string in `YumQuick.Api/appsettings.json` to point to your local SQL Server instance.
    Add your Stripe API keys (`SecretKey` and `WebhookSecret`) to `appsettings.json`.

3.  **Apply Migrations:**
    ```bash
    dotnet ef database update --project YumQuick.Data --startup-project YumQuick.Api
    ```
4.  **Firebase Setup:**
    *   Create a Firebase project.
    *   Download the Service Account JSON file.
    *   Rename it to `firebase-config.json` and place it in the `YumQuick.Api` root folder.

5.  **Run the Application:**
    ```bash
    cd YumQuick.Api
    dotnet run
    ```
    *Access the Swagger UI at: `https://localhost:5001/swagger`*

---

## 📱 Frontend Integration Guide (For Web & Mobile Developers)

Welcome to the YumQuick API! This section outlines the standard flow to consume the API endpoints effectively.

### 1. Authentication & Setup
*   **Register/Login:** Call `POST /api/auth/register` or `/login`. You will receive a `token` (JWT).
*   **Attach Token:** Include this token in the header of all subsequent requests: `Authorization: Bearer {token}`.
*   **Device Token (FCM):** On app startup, generate a Firebase Device Token and send it to `POST /api/notifications/save-token` to enable Push Notifications.

### 2. Browsing & Cart Management
*   **Fetch Menu:** Use `GET /api/products` to load the catalog.
*   **Manage Cart:**
    *   Add items: `POST /api/cart/add` (Specify productId, quantity, and variant IDs if any).
    *   View cart: `GET /api/cart`
    *   *Note: The cart is linked to the user's account, so it persists across devices.*

### 3. Checkout & Payment Flow (Stripe Integration)
*   **Place Order:** Call `POST /api/Orders/checkout` with the delivery address and payment method (`Cash`, `NewCard`, or `SavedCard`).
*   **Payment Handling:**
    *   If paying by card, the API generates a PaymentIntent and returns a `ClientSecret`.
    *   Use the Stripe SDK on the frontend (e.g., `initPaymentSheet`) passing the `ClientSecret` to securely handle card inputs.
    *   The API handles payment success or failure asynchronously by listening to Stripe events via Webhooks (`POST /api/payment/stripe-webhook`).
*   **Save Card for Future Use:** Generate a `PaymentMethodId` using Stripe SDK and POST it to `api/PaymentMethods` along with `lastFourDigits` and `brand`.

### 4. Real-Time Order Tracking (SignalR)
*   **Connect:** Establish a SignalR connection to `wss://yourdomain.com/hubs/notifications`.
*   **Listen:** Listen for the `ReceiveNotification` event. You will receive real-time JSON payloads whenever the manager accepts the order or the driver is on the way.

### 5. Support & Profile
*   **Tickets:** Users can open a ticket via `POST /api/tickets`. Managers will reply, triggering a real-time notification to the user.
*   **Profile:** Use `GET /api/profile` and `PUT /api/profile` to view and update user information. Soft delete is supported via `DELETE /api/profile`.

---
*Developed By Salah Ahmed with ❤️ as a showcase of modern .NET backend engineering.*