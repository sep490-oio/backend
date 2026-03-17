# Moderation + Warehouse

Bao gom Reports, Disputes, admin ops, Notifications HTTP, Warehouse va Webhooks.

### POST /api/admin/auctions/{auctionId:guid}/alerts

- Muc dich: Thuc hien Flag Auction qua POST /api/admin/auctions/{auctionId:guid}/alerts.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [FlagAuctionEndpoint.Request](./schemas.md#schema-flagauctionendpoint-request)
- Success responses:
  - 200 OK: [MonitoringAlertDto](./schemas.md#schema-monitoringalertdto)
- Error statuses: 400 Bad Request, 404 Not Found

### POST /api/admin/auctions/{auctionId:guid}/bids/{bidId:guid}/cancel

- Muc dich: Thuc hien Cancel Invalid Bid qua POST /api/admin/auctions/{auctionId:guid}/bids/{bidId:guid}/cancel.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
  - bidId: Guid
- Query params: none
- Request body: [CancelInvalidBidEndpoint.Request](./schemas.md#schema-cancelinvalidbidendpoint-request)
- Success responses:
  - 200 OK: [BidDto](./schemas.md#schema-biddto)
- Error statuses: 400 Bad Request, 404 Not Found, 409 Conflict

### PUT /api/admin/auctions/{auctionId:guid}/curation

- Muc dich: Cap nhat Set Auction Curation qua PUT /api/admin/auctions/{auctionId:guid}/curation.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [SetAuctionCurationEndpoint.Request](./schemas.md#schema-setauctioncurationendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### POST /api/admin/auctions/{auctionId:guid}/emergencies

- Muc dich: Thuc hien Trigger Auction Emergency qua POST /api/admin/auctions/{auctionId:guid}/emergencies.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [TriggerAuctionEmergencyEndpoint.Request](./schemas.md#schema-triggerauctionemergencyendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### POST /api/admin/auctions/{auctionId:guid}/emergencies/{emergencyId:guid}/resolve

- Muc dich: Thuc hien Resolve Auction Emergency qua POST /api/admin/auctions/{auctionId:guid}/emergencies/{emergencyId:guid}/resolve.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
  - emergencyId: Guid
- Query params: none
- Request body: [ResolveAuctionEmergencyEndpoint.Request](./schemas.md#schema-resolveauctionemergencyendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### POST /api/admin/auctions/{auctionId:guid}/sealed-bids/{sealedBidId:guid}/reveal

- Muc dich: Thuc hien Reveal Sealed Bid qua POST /api/admin/auctions/{auctionId:guid}/sealed-bids/{sealedBidId:guid}/reveal.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
  - sealedBidId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### POST /api/admin/disputes/{disputeId:guid}/resolve

- Muc dich: Thuc hien Resolve Dispute qua POST /api/admin/disputes/{disputeId:guid}/resolve.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - disputeId: Guid
- Query params: none
- Request body: [ResolveDisputeEndpoint.Request](./schemas.md#schema-resolvedisputeendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### GET /api/admin/items/{itemId:guid}

- Muc dich: Lay Get Admin Item Detail qua GET /api/admin/items/{itemId:guid}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadItems
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented

### POST /api/admin/items/{itemId:guid}/approve

- Muc dich: Thuc hien Approve Item qua POST /api/admin/items/{itemId:guid}/approve.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### POST /api/admin/items/{itemId:guid}/assign

- Muc dich: Thuc hien Assign Item Reviewer qua POST /api/admin/items/{itemId:guid}/assign.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [AssignItemReviewerEndpoint.Request](./schemas.md#schema-assignitemreviewerendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### POST /api/admin/items/{itemId:guid}/reject

- Muc dich: Thuc hien Reject Item qua POST /api/admin/items/{itemId:guid}/reject.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [RejectItemEndpoint.Request](./schemas.md#schema-rejectitemendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### GET /api/admin/items/{itemId:guid}/reviews

- Muc dich: Lay Get Item Review History qua GET /api/admin/items/{itemId:guid}/reviews.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadItems
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented

### GET /api/admin/items/review-queue

- Muc dich: Lay Get Review Queue qua GET /api/admin/items/review-queue.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadItems
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetReviewQueueEndpoint.Parameters](./schemas.md#schema-getreviewqueueendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented

### GET /api/admin/monitoring-alerts

- Muc dich: Lay Get Monitoring Alerts qua GET /api/admin/monitoring-alerts.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadItems
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<MonitoringAlertDto>](./schemas.md#schema-ireadonlylist-monitoringalertdto)
- Error statuses: none documented

### POST /api/admin/monitoring-alerts/{alertId:guid}/acknowledge

- Muc dich: Thuc hien Acknowledge Monitoring Alert qua POST /api/admin/monitoring-alerts/{alertId:guid}/acknowledge.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - alertId: Guid
- Query params: none
- Request body: [AcknowledgeMonitoringAlertEndpoint.Request](./schemas.md#schema-acknowledgemonitoringalertendpoint-request)
- Success responses:
  - 200 OK: [MonitoringAlertDto](./schemas.md#schema-monitoringalertdto)
- Error statuses: 404 Not Found

### POST /api/admin/monitoring-alerts/{alertId:guid}/resolve

- Muc dich: Thuc hien Resolve Monitoring Alert qua POST /api/admin/monitoring-alerts/{alertId:guid}/resolve.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - alertId: Guid
- Query params: none
- Request body: [ResolveMonitoringAlertEndpoint.Request](./schemas.md#schema-resolvemonitoringalertendpoint-request)
- Success responses:
  - 200 OK: [MonitoringAlertDto](./schemas.md#schema-monitoringalertdto)
- Error statuses: 404 Not Found

### GET /api/admin/permissions

- Muc dich: Lay Get Permissions qua GET /api/admin/permissions.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPermissions
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetPermissionsEndpoint.Parameters](./schemas.md#schema-getpermissionsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<string>](./schemas.md#schema-pagedlist-string)
- Error statuses: none documented

### GET /api/admin/reports

- Muc dich: Lay Get Reports qua GET /api/admin/reports.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadItems
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<ReportDto>](./schemas.md#schema-ireadonlylist-reportdto)
- Error statuses: none documented

### POST /api/admin/reports/{reportId:guid}/assign

- Muc dich: Thuc hien Assign Report qua POST /api/admin/reports/{reportId:guid}/assign.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - reportId: Guid
- Query params: none
- Request body: [AssignReportEndpoint.Request](./schemas.md#schema-assignreportendpoint-request)
- Success responses:
  - 200 OK: [ReportDto](./schemas.md#schema-reportdto)
- Error statuses: 400 Bad Request, 404 Not Found

### POST /api/admin/reports/{reportId:guid}/escalate-emergency

- Muc dich: Thuc hien Escalate Report Emergency qua POST /api/admin/reports/{reportId:guid}/escalate-emergency.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - reportId: Guid
- Query params: none
- Request body: [EscalateReportEmergencyEndpoint.Request](./schemas.md#schema-escalatereportemergencyendpoint-request)
- Success responses:
  - 200 OK: [ReportDto](./schemas.md#schema-reportdto)
- Error statuses: 400 Bad Request, 404 Not Found

### POST /api/admin/reports/{reportId:guid}/resolve

- Muc dich: Thuc hien Resolve Report qua POST /api/admin/reports/{reportId:guid}/resolve.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageItems
- Headers: Authorization: Bearer <token>
- Path params:
  - reportId: Guid
- Query params: none
- Request body: [ResolveReportEndpoint.Request](./schemas.md#schema-resolvereportendpoint-request)
- Success responses:
  - 200 OK: [ReportDto](./schemas.md#schema-reportdto)
- Error statuses: 404 Not Found

### GET /api/admin/roles

- Muc dich: Lay Get All Roles qua GET /api/admin/roles.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadRoles
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<RoleDto>](./schemas.md#schema-ireadonlylist-roledto)
- Error statuses: none documented

### PUT /api/admin/roles/{role}/permissions/{permission}

- Muc dich: Cap nhat Toggle Permission From Role qua PUT /api/admin/roles/{role}/permissions/{permission}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManagePermissions
- Headers: Authorization: Bearer <token>
- Path params:
  - role: string
  - permission: string
- Query params: none
- Request body: [TogglePermissionFromRoleEndpoint.Request](./schemas.md#schema-togglepermissionfromroleendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/admin/seller-profiles

- Muc dich: Lay Get Seller Profiles qua GET /api/admin/seller-profiles.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadSellerProfiles
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyCollection<SellerProfileDto>](./schemas.md#schema-ireadonlycollection-sellerprofiledto)
- Error statuses: none documented

### POST /api/admin/seller-profiles/{id:guid}/reject

- Muc dich: Thuc hien Reject Seller Profile qua POST /api/admin/seller-profiles/{id:guid}/reject.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageSellerProfiles
- Headers: Authorization: Bearer <token>
- Path params:
  - id: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/admin/seller-profiles/{id:guid}/verify

- Muc dich: Thuc hien Verify Seller Profile qua POST /api/admin/seller-profiles/{id:guid}/verify.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageSellerProfiles
- Headers: Authorization: Bearer <token>
- Path params:
  - id: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/admin/terms

- Muc dich: Lay Get All Terms Documents qua GET /api/admin/terms.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadTerms
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<TermsDocumentDto>](./schemas.md#schema-ireadonlylist-termsdocumentdto)
- Error statuses: none documented

### POST /api/admin/terms

- Muc dich: Thuc hien Create Terms qua POST /api/admin/terms.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageTerms
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateTermsEndpoint.Request](./schemas.md#schema-createtermsendpoint-request)
- Success responses:
  - 201 Created: [TermsDocumentDto](./schemas.md#schema-termsdocumentdto)
- Error statuses: 400 Bad Request

### PUT /api/admin/terms/{id:guid}/activate

- Muc dich: Cap nhat Activate Terms Document qua PUT /api/admin/terms/{id:guid}/activate.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageTerms
- Headers: Authorization: Bearer <token>
- Path params:
  - id: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found

### GET /api/admin/users

- Muc dich: Lay Get Users qua GET /api/admin/users.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadUsers
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetUsersEndpoint.Parameters](./schemas.md#schema-getusersendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<UserListItemDto>](./schemas.md#schema-pagedlist-userlistitemdto)
- Error statuses: none documented

### DELETE /api/admin/users/{userId:guid}

- Muc dich: Xoa Delete User qua DELETE /api/admin/users/{userId:guid}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageUsers
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/admin/users/{userId:guid}

- Muc dich: Lay Get User qua GET /api/admin/users/{userId:guid}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadUsers
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [UserDto](./schemas.md#schema-userdto)
- Error statuses: none documented

### DELETE /api/admin/users/{userId:guid}/permissions/{permission}

- Muc dich: Xoa Revoke Permission From User qua DELETE /api/admin/users/{userId:guid}/permissions/{permission}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.RevokePermission
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
  - permission: string
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/admin/users/{userId:guid}/permissions/{permission}

- Muc dich: Thuc hien Grant Permission For User qua POST /api/admin/users/{userId:guid}/permissions/{permission}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.GrantPermission
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
  - permission: string
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### PUT /api/admin/users/{userId:guid}/permissions/{permission}

- Muc dich: Cap nhat Deny Permission From User qua PUT /api/admin/users/{userId:guid}/permissions/{permission}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.DenyPermission
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
  - permission: string
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/admin/users/{userId:guid}/risk-flags

- Muc dich: Thuc hien Flag User qua POST /api/admin/users/{userId:guid}/risk-flags.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageUsers
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
- Query params: none
- Request body: [FlagUserEndpoint.Request](./schemas.md#schema-flaguserendpoint-request)
- Success responses:
  - 200 OK: [UserRiskFlagDto](./schemas.md#schema-userriskflagdto)
- Error statuses: 400 Bad Request, 404 Not Found

### DELETE /api/admin/users/{userId:guid}/roles/{role}

- Muc dich: Xoa Revoke Role From User qua DELETE /api/admin/users/{userId:guid}/roles/{role}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.RevokeRole
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
  - role: string
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/admin/users/{userId:guid}/roles/{role}

- Muc dich: Thuc hien Assign Role To User qua POST /api/admin/users/{userId:guid}/roles/{role}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.AssignRole
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
  - role: string
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### PATCH /api/admin/users/{userId:guid}/status

- Muc dich: Cap nhat Change User Status qua PATCH /api/admin/users/{userId:guid}/status.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageUsers
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
- Query params: none
- Request body: [ChangeUserStatusEndpoint.Request](./schemas.md#schema-changeuserstatusendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### PATCH /api/admin/users/{userId:guid}/unlock

- Muc dich: Cap nhat Unlock User qua PATCH /api/admin/users/{userId:guid}/unlock.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageUsers
- Headers: Authorization: Bearer <token>
- Path params:
  - userId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/admin/verifications

- Muc dich: Lay Get Pending Verifications qua GET /api/admin/verifications.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadVerifications
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyCollection<VerificationSummaryDto>](./schemas.md#schema-ireadonlycollection-verificationsummarydto)
- Error statuses: none documented

### GET /api/admin/verifications/{verificationId:guid}

- Muc dich: Lay Get Verification By Id qua GET /api/admin/verifications/{verificationId:guid}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadVerifications
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [VerificationDto](./schemas.md#schema-verificationdto)
- Error statuses: none documented

### POST /api/admin/verifications/{verificationId:guid}/approve

- Muc dich: Thuc hien Approve Verification qua POST /api/admin/verifications/{verificationId:guid}/approve.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageVerifications
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/admin/verifications/{verificationId:guid}/reject

- Muc dich: Thuc hien Reject Verification qua POST /api/admin/verifications/{verificationId:guid}/reject.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManageVerifications
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: [RejectVerificationEndpoint.Request](./schemas.md#schema-rejectverificationendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/disputes

- Muc dich: Lay Get Accessible Disputes qua GET /api/disputes.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [PagedList<DisputeSummaryDto>](./schemas.md#schema-pagedlist-disputesummarydto)
- Error statuses: 401 Unauthorized

### GET /api/disputes/{disputeId:guid}

- Muc dich: Lay Get Dispute Thread qua GET /api/disputes/{disputeId:guid}.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - disputeId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [DisputeThreadDto](./schemas.md#schema-disputethreaddto)
- Error statuses: 401 Unauthorized, 403 Forbidden, 404 Not Found

### GET /api/disputes/{disputeId:guid}/messages

- Muc dich: Lay Get Dispute Messages qua GET /api/disputes/{disputeId:guid}/messages.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - disputeId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [DisputeMessagePageDto](./schemas.md#schema-disputemessagepagedto)
- Error statuses: 401 Unauthorized, 403 Forbidden, 404 Not Found

### POST /api/disputes/{disputeId:guid}/messages

- Muc dich: Thuc hien Send Dispute Message qua POST /api/disputes/{disputeId:guid}/messages.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>, Idempotency-Key: <unique-key> (required)
- Path params:
  - disputeId: Guid
- Query params: none
- Request body: [SendDisputeMessageEndpoint.Request](./schemas.md#schema-senddisputemessageendpoint-request)
- Success responses:
  - 201 Created: [DisputeMessageDto](./schemas.md#schema-disputemessagedto)
- Error statuses: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict
- Notes:
  - Endpoint nay duoc gate boi Idempotency filter.

### POST /api/disputes/{disputeId:guid}/read

- Muc dich: Thuc hien Mark Dispute Read qua POST /api/disputes/{disputeId:guid}/read.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - disputeId: Guid
- Query params: none
- Request body: [MarkDisputeReadEndpoint.Request](./schemas.md#schema-markdisputereadendpoint-request)
- Success responses:
  - 200 OK: [DisputeParticipantReadStateDto](./schemas.md#schema-disputeparticipantreadstatedto)
- Error statuses: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found

### GET /api/me

- Muc dich: Lay Get Current User qua GET /api/me.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.Read
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [UserDto](./schemas.md#schema-userdto)
- Error statuses: none documented

### GET /api/me/reports

- Muc dich: Lay Get My Reports qua GET /api/me/reports.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<ReportDto>](./schemas.md#schema-ireadonlylist-reportdto)
- Error statuses: 401 Unauthorized

### POST /api/media/confirm

- Muc dich: Thuc hien Confirm Upload qua POST /api/media/confirm.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Media.ConfirmUpload
- Headers: Authorization: Bearer <token>, Idempotency-Key: <unique-key> (required)
- Path params: none
- Query params: none
- Request body: [ConfirmUploadEndpoint.Request](./schemas.md#schema-confirmuploadendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request, 409 Conflict
- Notes:
  - Endpoint nay duoc gate boi Idempotency filter.

### GET /api/media/contexts

- Muc dich: Lay Get Upload Contexts qua GET /api/media/contexts.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Media.ReadContexts
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented

### POST /api/media/upload-signature

- Muc dich: Thuc hien Request Upload Signature qua POST /api/media/upload-signature.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Media.Upload
- Headers: Authorization: Bearer <token>, Idempotency-Key: <unique-key> (required)
- Path params: none
- Query params: none
- Request body: [RequestUploadSignatureEndpoint.Request](./schemas.md#schema-requestuploadsignatureendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request, 409 Conflict
- Notes:
  - Endpoint nay duoc gate boi Idempotency filter.

### GET /api/notifications

- Muc dich: Lay Get My Notifications qua GET /api/notifications.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetMyNotificationsEndpoint.Parameters](./schemas.md#schema-getmynotificationsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<NotificationDto>](./schemas.md#schema-pagedlist-notificationdto)
- Error statuses: 401 Unauthorized

### PATCH /api/notifications/{notificationId:guid}/read

- Muc dich: Cap nhat Mark As Read qua PATCH /api/notifications/{notificationId:guid}/read.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - notificationId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 401 Unauthorized, 404 Not Found

### PATCH /api/notifications/read-all

- Muc dich: Cap nhat Mark All As Read qua PATCH /api/notifications/read-all.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 401 Unauthorized

### GET /api/notifications/unread-count

- Muc dich: Lay Get Unread Count qua GET /api/notifications/unread-count.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 401 Unauthorized

### POST /api/reports

- Muc dich: Thuc hien Create Report qua POST /api/reports.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateReportEndpoint.Request](./schemas.md#schema-createreportendpoint-request)
- Success responses:
  - 200 OK: [ReportDto](./schemas.md#schema-reportdto)
- Error statuses: 400 Bad Request, 401 Unauthorized, 409 Conflict

### GET /api/warehouse/inbound-shipments

- Muc dich: Lay Get Inbound Shipments qua GET /api/warehouse/inbound-shipments.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ReadShipments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<InboundShipmentDto>](./schemas.md#schema-ireadonlylist-inboundshipmentdto)
- Error statuses: none documented

### POST /api/warehouse/inbound-shipments

- Muc dich: Thuc hien Book Inbound Shipment qua POST /api/warehouse/inbound-shipments.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.BookInbound
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### GET /api/warehouse/inbound-shipments/{shipmentId:guid}

- Muc dich: Lay Get Inbound Shipment By Id qua GET /api/warehouse/inbound-shipments/{shipmentId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ReadShipments
- Headers: Authorization: Bearer <token>
- Path params:
  - shipmentId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [InboundShipmentDto](./schemas.md#schema-inboundshipmentdto)
- Error statuses: 404 Not Found

### POST /api/warehouse/inbound-shipments/{shipmentId:guid}/cancel

- Muc dich: Thuc hien Cancel Inbound Shipment qua POST /api/warehouse/inbound-shipments/{shipmentId:guid}/cancel.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ReadShipments
- Headers: Authorization: Bearer <token>
- Path params:
  - shipmentId: Guid
- Query params: none
- Request body: [CancelInboundShipmentEndpoint.Request](./schemas.md#schema-cancelinboundshipmentendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found, 409 Conflict

### POST /api/warehouse/inbound-shipments/{shipmentId:guid}/inspect

- Muc dich: Thuc hien Inspect Warehouse Item qua POST /api/warehouse/inbound-shipments/{shipmentId:guid}/inspect.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.Inspect
- Headers: Authorization: Bearer <token>
- Path params:
  - shipmentId: Guid
- Query params: none
- Request body: [InspectWarehouseItemEndpoint.Request](./schemas.md#schema-inspectwarehouseitemendpoint-request)
- Success responses:
  - 201 Created: [WarehouseInspectionDto](./schemas.md#schema-warehouseinspectiondto)
- Error statuses: 400 Bad Request, 404 Not Found, 409 Conflict

### POST /api/warehouse/inbound-shipments/{shipmentId:guid}/review

- Muc dich: Thuc hien Review Warehouse Inspection qua POST /api/warehouse/inbound-shipments/{shipmentId:guid}/review.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.Inspect
- Headers: Authorization: Bearer <token>
- Path params:
  - shipmentId: Guid
- Query params: none
- Request body: [ReviewWarehouseInspectionEndpoint.Request](./schemas.md#schema-reviewwarehouseinspectionendpoint-request)
- Success responses:
  - 200 OK: [WarehouseInspectionDto](./schemas.md#schema-warehouseinspectiondto)
- Error statuses: 400 Bad Request, 404 Not Found, 409 Conflict

### GET /api/warehouse/inbound-shipments/inspection-queue

- Muc dich: Lay Get Inspection Queue qua GET /api/warehouse/inbound-shipments/inspection-queue.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ReadShipments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<InspectionQueueItemDto>](./schemas.md#schema-ireadonlylist-inspectionqueueitemdto)
- Error statuses: none documented

### GET /api/warehouse/outbound-shipments

- Muc dich: Lay Get Outbound Shipments qua GET /api/warehouse/outbound-shipments.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ReadShipments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<OutboundShipmentDto>](./schemas.md#schema-ireadonlylist-outboundshipmentdto)
- Error statuses: none documented

### POST /api/warehouse/outbound-shipments

- Muc dich: Thuc hien Book Outbound Shipment qua POST /api/warehouse/outbound-shipments.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.BookOutbound
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### GET /api/warehouse/outbound-shipments/{shipmentId:guid}

- Muc dich: Lay Get Outbound Shipment By Id qua GET /api/warehouse/outbound-shipments/{shipmentId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ReadShipments
- Headers: Authorization: Bearer <token>
- Path params:
  - shipmentId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [OutboundShipmentDto](./schemas.md#schema-outboundshipmentdto)
- Error statuses: 404 Not Found

### PUT /api/warehouse/shipping-provider-configs/{configId:guid}

- Muc dich: Cap nhat Update Shipping Provider Config qua PUT /api/warehouse/shipping-provider-configs/{configId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ManageLocations
- Headers: Authorization: Bearer <token>
- Path params:
  - configId: Guid
- Query params: none
- Request body: [UpdateShippingProviderConfigEndpoint.Request](./schemas.md#schema-updateshippingproviderconfigendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found

### GET /api/warehouse/storage-locations

- Muc dich: Lay Get Storage Locations qua GET /api/warehouse/storage-locations.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ManageLocations
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<StorageLocationDto>](./schemas.md#schema-ireadonlylist-storagelocationdto)
- Error statuses: none documented

### POST /api/warehouse/storage-locations

- Muc dich: Thuc hien Create Storage Location qua POST /api/warehouse/storage-locations.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ManageLocations
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 201 Created: [StorageLocationDto](./schemas.md#schema-storagelocationdto)
- Error statuses: 400 Bad Request, 409 Conflict

### DELETE /api/warehouse/storage-locations/{locationId:guid}

- Muc dich: Xoa Delete Storage Location qua DELETE /api/warehouse/storage-locations/{locationId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ManageLocations
- Headers: Authorization: Bearer <token>
- Path params:
  - locationId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found, 409 Conflict

### PUT /api/warehouse/storage-locations/{locationId:guid}

- Muc dich: Cap nhat Update Storage Location qua PUT /api/warehouse/storage-locations/{locationId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ManageLocations
- Headers: Authorization: Bearer <token>
- Path params:
  - locationId: Guid
- Query params: none
- Request body: [UpdateStorageLocationEndpoint.Request](./schemas.md#schema-updatestoragelocationendpoint-request)
- Success responses:
  - 200 OK: [StorageLocationDto](./schemas.md#schema-storagelocationdto)
- Error statuses: 404 Not Found, 409 Conflict

### GET /api/warehouse/warehouse-items

- Muc dich: Lay Get Warehouse Items qua GET /api/warehouse/warehouse-items.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.ReadShipments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<WarehouseItemDto>](./schemas.md#schema-ireadonlylist-warehouseitemdto)
- Error statuses: none documented

### POST /api/warehouse/warehouse-items/{warehouseItemId}/store

- Muc dich: Thuc hien Store Warehouse Item qua POST /api/warehouse/warehouse-items/{warehouseItemId}/store.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Warehouse.Store
- Headers: Authorization: Bearer <token>
- Path params:
  - warehouseItemId: Guid
- Query params: none
- Request body: [StoreWarehouseItemEndpoint.Request](./schemas.md#schema-storewarehouseitemendpoint-request)
- Success responses:
  - 200 OK: [WarehouseItemDto](./schemas.md#schema-warehouseitemdto)
- Error statuses: 400 Bad Request, 404 Not Found, 409 Conflict

### POST /webhooks/ghn

- Muc dich: Thuc hien Ghn Webhook qua POST /webhooks/ghn.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented
- Notes:
  - Route nay dang bi an khoi Swagger/Scalar va duoc document rieng.
  - Webhook carrier tu GHN. Luon tra 200 de tranh retry vo han.


