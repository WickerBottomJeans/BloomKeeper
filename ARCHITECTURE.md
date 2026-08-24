Shop Architecture:
1. Never use playfab price feature.
2. Store price config to CloudFare R2.
3. Shop purchases only edit PlayFab inventory. Never use PlayFab currency features.
4. Shop purchases use a durable saga. Every completed purchase step must have a compensating action so the entire purchase can be reverted if a later step fails.
