Shop Architecture:
1. Never use playfab price feature.
2. Store price config to CloudFare R2.
3. Shop purchases only edit PlayFab inventory. Never use PlayFab currency features.
4. Inventory-only offers use one atomic PlayFab Economy operation and do not create a saga record.
5. Offers cannot mix PlayFab inventory grants with Entity File grants.
6. Cross-system purchases use `shop-purchases.json` as one durable current-purchase recovery slot, never as purchase history. The purchase is `Pending`, `Completed`, `Rejected`, or `Reverted`, while payment and grant progress are stored separately.
7. Durable grants use generic kind-and-payload envelopes. Azure dependency injection constructs one Entity File handler per grant kind, and the dispatcher only routes records to those handlers.
8. The server records `Pending` before subtracting the cost. Every handler prepares its file mutation, then all grant files and `Completed` are committed in one Entity File upload. If preparation or upload fails, the server records compensation, refunds the cost with its own idempotency key, and records `Reverted`.
9. A repeated purchase key checks the current slot before loading current R2 config, and player-state loading recovers an unfinished current purchase.
10. Successful file-backed purchases tell the client to reload generic player state instead of returning grant-specific response fields.
11. A terminal purchase stays in the single slot for response-loss retries and is replaced when a different purchase starts.
12. Shopfront config is presentation-only. Purchase eligibility comes from the offer catalog and the offer's `enabled` state, never from whether the shopfront currently displays the offer.
