# RaceDay API - endpoint plan

All endpoints except register/login need an active session (log in first). Logged out = 401. Wrong role = 403. Unknown Id = 404.

| Group | Method & path | Who | Success |
|---|---|---|---|
| Auth | POST /api/auth/register | Anyone | 200 |
| Auth | POST /api/auth/login | Anyone | 200 |
| Auth | POST /api/auth/logout | Anyone | 200 |
| Profile | GET /api/profile | Logged in | 200 |
| Profile | PUT /api/profile | Logged in | 200 |
| Profile | POST /api/profile/picture (image upload) | Participant | 200 |
| Events | GET /api/events/types (distinct event types) | Logged in | 200 |
| Events | GET /api/events (filters: search, type, location, from, to, upcoming, mine), GET /api/events/{id} | Logged in | 200 |
| Events | POST /api/events/{id}/banner (image upload) | Organiser (own events) | 200 |
| Events | POST /api/events | Organiser | 201 |
| Events | PUT /api/events/{id}, DELETE /api/events/{id} | Organiser (own events) | 200 |
| Categories | GET /api/events/{eventId}/categories | Logged in | 200 |
| Categories | POST /api/events/{eventId}/categories | Organiser (own events) | 201 |
| Categories | PUT /api/categories/{id}, DELETE /api/categories/{id} | Organiser (own events) | 200 |
| Enrolments | POST /api/events/{eventId}/enrol | Participant | 200 |
| Enrolments | GET /api/enrolments/mine | Participant | 200 |
| Enrolments | GET /api/events/{eventId}/enrolments | Organiser (own events) | 200 |
| Enrolments | PUT /api/enrolments/{id}/status (pending / confirmed / cancelled) | Organiser (own events) | 200 |
| Results | GET /api/results/event/{eventId} | Organiser (own events) | 200 |
| Results | POST /api/results (only once the event day has arrived) | Organiser (own events) | 201 |
| Results | PUT /api/results/{id} | Organiser (own events) | 200 |
| Results | POST /api/results/event/{eventId}/publish | Organiser (own events) | 200 |
| Results | GET /api/results/mine (published only, includes total finishers) | Participant | 200 |
| Dashboard | GET /api/organiser/dashboard | Organiser | 200 |
