# Project Zenith: Feature-to-CQRS Component Mapping

This document maps the major functional requirements of Project Zenith to their corresponding components within our Command Query Responsibility Segregation (CQRS) architecture. It serves as a high-level technical blueprint for understanding how data flows through the system for each feature.

## Mapping Table

| Feature Area                | Write Stack (Commands Handled)                                                                                                                                                                                            | Kafka Events Published                                                                                                                             | Read Stack (Queries Handled)                                                                                                                                                                        |
| :-------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **6.1 User Management**     | `RegisterUserCommand`<br>`VerifyUserEmailCommand`<br>`LoginUserCommand`<br>`RefreshTokenCommand`<br>`LogoutUserCommand`<br>`RequestPasswordResetCommand`<br>`UpdateUserProfileCommand`<br>`RequestDeveloperStatusCommand` | `UserRegisteredEvent`<br>`UserEmailVerifiedEvent`<br>`PasswordResetRequestedEvent`<br>`UserProfileUpdatedEvent`<br>`DeveloperStatusRequestedEvent` | `GetUserProfileQuery`<br>`GetPublicDeveloperProfileQuery`                                                                                                                                           |
| **6.2 App Store Core**      | _N/A (This feature is primarily Read-focused)_<br>`RecordAppDownloadCommand` (for tracking)                                                                                                                               | `AppDownloadedEvent`                                                                                                                               | `ListAppsQuery` (with filters/sorts)<br>`SearchAppsQuery`<br>`GetAppDetailsQuery`<br>`ListAppVersionsQuery`<br>`GetAppChangelogQuery`<br>`GenerateSecureDownloadUrlQuery`<br>`GetFeaturedAppsQuery` |
| **6.3 Reviews & Ratings**   | `SubmitReviewCommand`<br>`UpdateReviewCommand`<br>`DeleteReviewCommand`<br>`ReportReviewCommand`                                                                                                                          | `ReviewSubmittedEvent`<br>`ReviewUpdatedEvent`<br>`ReviewDeletedEvent`<br>`RatingAggregateUpdatedEvent`<br>`ReviewReportedEvent`                   | `ListReviewsForAppQuery`<br>`GetAppRatingSummaryQuery`                                                                                                                                              |
| **6.4 Developer Dashboard** | `SubmitAppCommand`<br>`UpdateAppMetadataCommand`<br>`UploadNewAppVersionCommand`<br>`SetAppPriceCommand`<br>`RequestPayoutCommand` (mock)                                                                                 | `AppSubmittedEvent`<br>`AppMetadataUpdatedEvent`<br>`AppVersionAddedEvent`<br>`AppPriceChangedEvent`<br>`AppPublishedEvent`<br>`AppRejectedEvent`  | `GetDeveloperDashboardAnalyticsQuery`<br>`ListDeveloperAppsQuery`<br`GetDeveloperRevenueQuery`<br>`GetAppModerationStatusQuery`                                                                     |
| **6.5 Admin Tools**         | `ApproveAppCommand`<br`RejectAppCommand`<br>`BanAppCommand`<br>`BanUserCommand`<br>`PromoteUserToDeveloperCommand`<br>`DeleteReviewAsAdminCommand`<br>`ResolveReportCommand`                                              | `AppApprovedEvent`<br>`AppRejectedEvent`<br>`AppBannedEvent`<br>`UserBannedEvent`<br>`UserPromotedToDeveloperEvent`<br>`ReviewDeletedByAdminEvent` | `ListPendingAppsQuery`<br>`ListAllUsersQuery`<br>`ListReportedContentQuery`<br>`GetSystemAnalyticsQuery`                                                                                            |
| **6.6 Malware Scanner**     | `MarkFileAsInfectedCommand`<br>`MarkFileAsCleanCommand`                                                                                                                                                                   | `FileScanCompletedEvent` (with clean/infected status)                                                                                              | `GetFileScanResultQuery`                                                                                                                                                                            |
| **6.7 Recommendation**      | `RecordUserBehaviorCommand` (e.g., clicks, views)                                                                                                                                                                         | `UserBehaviorRecordedEvent`                                                                                                                        | `GetPersonalizedRecommendationsQuery`<br>`GetSimilarAppsQuery`<br>`GetPopularAppsQuery`                                                                                                             |
| **6.8 Monetization**        | `CreateCheckoutSessionCommand`<br>`RecordPurchaseCommand`                                                                                                                                                                 | `PurchaseMadeEvent`                                                                                                                                | `GetUserPurchaseHistoryQuery`<br>`CheckAppOwnershipQuery`                                                                                                                                           |

## Detailed Flow Descriptions

### User Registration Flow:

1.  **Write:** A `RegisterUserCommand` is sent to the Write API.
2.  **Event:** A `UserRegisteredEvent` is published to Kafka.
3.  **Read:** A consumer updates the `Users` read model. The user is initially marked as "unverified." When the `VerifyUserEmailCommand` is processed and a `UserEmailVerifiedEvent` is published, the consumer updates the user's status in the read model.

### App Browsing and Download Flow:

1.  **Read:** The WPF client sends a `ListAppsQuery` (with potential filters) to the Read API.
2.  **Read:** The Read API fetches the denormalized app data from the Read DB (or Redis cache) and returns it.
3.  **Read:** The user clicks "Download." The client sends a `GenerateSecureDownloadUrlQuery` to the Read API, which returns a temporary, signed URL.
4.  **Write:** After the download completes, the client can optionally send a `RecordAppDownloadCommand` to the Write API to update download statistics.
5.  **Event:** An `AppDownloadedEvent` is published.
6.  **Read:** A consumer processes this event to update the download count in the `Apps` read model, which can affect its "trending" status.

### App Submission and Moderation Flow:

1.  **Write:** A developer sends a `SubmitAppCommand` via the Developer Dashboard.
2.  **Event:** An `AppSubmittedEvent` is published to Kafka.
3.  **Read:** A consumer creates the app record in the Read DB with a status of `PendingModeration`.
4.  **Read:** An Admin views the moderation queue by sending a `ListPendingAppsQuery` to the Read API.
5.  **Write:** The Admin approves the app by sending an `ApproveAppCommand` to the Write API.
6.  **Event:** An `AppApprovedEvent` is published.
7.  **Read:** A consumer updates the app's status in the Read DB to `Published`, making it visible to the public.
