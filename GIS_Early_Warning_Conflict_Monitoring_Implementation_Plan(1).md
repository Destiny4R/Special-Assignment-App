# GIS-Based Early Warning and Real-Time Framework for Farmer–Herder Conflict Resolution

> **Implementation Plan — ASP.NET Core Razor Pages + MySQL + HTML/CSS/JavaScript + SignalR + Google Maps**

## Technology Baseline

This implementation plan is specifically aligned to the required technology stack:

| Layer | Technology |
|---|---|
| Application framework | ASP.NET Core Razor Pages |
| Backend language | C# |
| UI | Razor Pages + HTML5 |
| Styling | CSS3 + Bootstrap 5 (recommended) |
| Client-side logic | Vanilla JavaScript (jQuery optional) |
| Database | MySQL |
| ORM | Entity Framework Core |
| MySQL provider | Pomelo.EntityFrameworkCore.MySql |
| Real-time communication | ASP.NET Core SignalR |
| Mapping | Google Maps JavaScript API |
| GPS | Browser Geolocation API (`watchPosition`) |
| Authentication | ASP.NET Core Identity |
| Spatial data | MySQL spatial types / latitude-longitude columns |
| Charts | Chart.js (optional) |

**Important:** PostgreSQL/PostGIS and Leaflet/OpenStreetMap are not part of this implementation baseline. Google Maps is the required map provider, and MySQL is the required database.

---

## 1. Core Product Workflow

The most important requirement is that a reporter should **not complete an incident form**. The reporting interaction is intentionally reduced to location sharing.

```text
PUBLIC REPORTER
      |
      v
[ REPORT / SHARE LOCATION ]
      |
      v
TERMS & CONDITIONS
      |
      v
[ ACCEPT & SHARE ]
      |
      v
BROWSER GPS PERMISSION
      |
      v
LIVE LOCATION TRACKING
      |
      +----> ASP.NET Core API
      |            |
      |            +----> MySQL
      |            |
      |            +----> SignalR
      |                       |
      |                       v
      |                OPERATIONS DASHBOARD
      |                       |
      |                [ VIEW LOCATION ]
      |                       |
      |                       v
      |                  GOOGLE MAPS
      |                       |
      |                 LIVE MARKER + TRAIL
      v
[ STOP SHARING ]
```

### Non-negotiable reporting rules

1. The reporter does not fill a form.
2. The reporter must accept location-sharing terms before tracking begins.
3. The browser requests GPS permission.
4. The system creates a report/session identifier.
5. JavaScript continuously captures location changes.
6. Coordinates are transmitted to the ASP.NET Core application.
7. The dashboard receives updates without page refresh.
8. **View Location** opens the selected reporter's live Google Maps view.
9. The marker moves as new coordinates arrive.
10. The system records the movement history for the active session.
11. The reporter can explicitly stop location sharing.

---

## 2. Recommended ASP.NET Core Razor Pages Architecture

```text
FarmerHerderConflict
|
+-- Data/
|   +-- ApplicationDbContext.cs
|   +-- DbInitializer.cs
|
+-- Models/
|   +-- ReportSession.cs
|   +-- LocationPoint.cs
|   +-- ConsentRecord.cs
|   +-- Alert.cs
|   +-- RiskZone.cs
|   +-- AuditLog.cs
|   +-- ApplicationUser.cs
|
+-- DTOs/
|   +-- StartReportRequest.cs
|   +-- LocationUpdateRequest.cs
|   +-- LiveReportDto.cs
|   +-- LocationPointDto.cs
|   +-- AlertDto.cs
|
+-- Services/
|   +-- IReportService.cs
|   +-- ReportService.cs
|   +-- ILocationService.cs
|   +-- LocationService.cs
|   +-- IRiskAssessmentService.cs
|   +-- RiskAssessmentService.cs
|   +-- IAlertService.cs
|   +-- AlertService.cs
|   +-- IGisService.cs
|   +-- GisService.cs
|
+-- Hubs/
|   +-- MonitoringHub.cs
|
+-- Pages/
|   +-- Report/
|   |   +-- Index.cshtml
|   |   +-- Index.cshtml.cs
|   |
|   +-- Dashboard/
|   |   +-- Index.cshtml
|   |   +-- Index.cshtml.cs
|   |
|   +-- Map/
|   |   +-- Location.cshtml
|   |   +-- Location.cshtml.cs
|   |
|   +-- Incidents/
|   |   +-- Index.cshtml
|   |   +-- Details.cshtml
|   |   +-- History.cshtml
|   |
|   +-- Alerts/
|   |   +-- Index.cshtml
|   |
|   +-- Admin/
|       +-- Users/
|       +-- RiskZones/
|       +-- Settings/
|
+-- wwwroot/
|   +-- css/
|   |   +-- site.css
|   |   +-- reporter.css
|   |   +-- dashboard.css
|   |   +-- map.css
|   |
|   +-- js/
|       +-- reporter-location.js
|       +-- live-dashboard.js
|       +-- live-map.js
|       +-- signalr-monitoring.js
|
+-- Program.cs
+-- appsettings.json
```

---

## 3. MySQL Database Design

The database should support both current live tracking and historical analysis.

### 3.1 ReportSessions

```text
ReportSessions
-----------------------------
Id                  BIGINT PK
PublicReportId      VARCHAR(50) UNIQUE
Status              VARCHAR(20)
ConsentAccepted     BOOLEAN
ConsentVersion      VARCHAR(30)
ConsentAcceptedAt   DATETIME
StartedAt           DATETIME
EndedAt             DATETIME NULL
LastSeenAt          DATETIME NULL
LastLatitude       DECIMAL(10,7) NULL
LastLongitude      DECIMAL(10,7) NULL
LastAccuracy       DECIMAL(10,2) NULL
CreatedAt           DATETIME
```

### 3.2 LocationPoints

```text
LocationPoints
-----------------------------
Id                  BIGINT PK
ReportSessionId     BIGINT FK
Latitude            DECIMAL(10,7)
Longitude           DECIMAL(10,7)
Accuracy            DECIMAL(10,2)
Altitude            DECIMAL(10,2) NULL
Speed               DECIMAL(10,2) NULL
Heading             DECIMAL(10,2) NULL
CapturedAt          DATETIME
ReceivedAt          DATETIME
```

If MySQL spatial functionality is enabled, a `POINT` column can additionally be maintained for geographic queries. Latitude/longitude columns should still be retained because they make API payloads and ordinary application operations straightforward.

### 3.3 ConsentRecords

```text
ConsentRecords
-----------------------------
Id                  BIGINT PK
ReportSessionId     BIGINT FK
TermsVersion        VARCHAR(30)
Accepted            BOOLEAN
AcceptedAt          DATETIME
```

### 3.4 Alerts

```text
Alerts
-----------------------------
Id                  BIGINT PK
AlertType           VARCHAR(50)
Severity            VARCHAR(20)
Title               VARCHAR(200)
Message             TEXT
ReportSessionId     BIGINT NULL
Latitude            DECIMAL(10,7) NULL
Longitude           DECIMAL(10,7) NULL
Status              VARCHAR(20)
CreatedAt           DATETIME
AcknowledgedAt      DATETIME NULL
AcknowledgedBy      VARCHAR(100) NULL
```

### 3.5 RiskZones

```text
RiskZones
-----------------------------
Id                  BIGINT PK
Name                VARCHAR(150)
RiskLevel           VARCHAR(20)
CenterLatitude      DECIMAL(10,7)
CenterLongitude     DECIMAL(10,7)
RadiusMeters        DECIMAL(12,2)
IsActive            BOOLEAN
CreatedAt           DATETIME
```

### 3.6 AuditLogs

```text
AuditLogs
-----------------------------
Id                  BIGINT PK
UserId              VARCHAR(450) NULL
Action              VARCHAR(100)
ReportSessionId     BIGINT NULL
Description         TEXT
CreatedAt           DATETIME
```

---

## 4. Reporter Page — Razor Pages

Recommended route:

```text
/Report
```

The page should initially contain only:

```text
+----------------------------------+
|            SYSTEM LOGO           |
|                                  |
|       REPORT AN EMERGENCY        |
|                                  |
| Share your live location with    |
| authorized response personnel.   |
|                                  |
|     [ SHARE MY LOCATION ]        |
|                                  |
| Your location is shared only     |
| after you provide consent.       |
+----------------------------------+
```

### Active state

After consent and GPS permission:

```text
+----------------------------------+
|        LOCATION SHARING          |
|             ACTIVE               |
|                                  |
|             ● LIVE               |
|                                  |
| Last update: 5 seconds ago       |
| GPS accuracy: 9 metres            |
|                                  |
|      [ STOP SHARING ]            |
+----------------------------------+
```

---

## 5. Terms and Conditions Flow

Clicking **Share My Location** opens a Bootstrap/Razor modal.

The modal should explain:

- The application will collect GPS coordinates.
- Coordinates may be transmitted continuously while the session is active.
- Authorized personnel may view the live location.
- The location is collected for conflict early-warning/response purposes.
- The reporter can stop sharing.
- Location retention is governed by the system's privacy policy.

Actions:

```text
[ CANCEL ]     [ I AGREE & SHARE LOCATION ]
```

The application must record the terms version and acceptance timestamp before starting the report session.

---

## 6. JavaScript GPS Tracking

Use the browser Geolocation API:

```javascript
const watchId = navigator.geolocation.watchPosition(
    handlePosition,
    handleLocationError,
    {
        enableHighAccuracy: true,
        maximumAge: 5000,
        timeout: 15000
    }
);
```

Every accepted position should be transformed into a payload such as:

```javascript
{
    reportId: reportId,
    latitude: position.coords.latitude,
    longitude: position.coords.longitude,
    accuracy: position.coords.accuracy,
    altitude: position.coords.altitude,
    speed: position.coords.speed,
    heading: position.coords.heading,
    capturedAt: new Date(position.timestamp).toISOString()
}
```

The client then sends the location to the ASP.NET Core endpoint.

Do not create an uncontrolled request loop. The client should use configurable movement/time thresholds and avoid sending redundant points when the position has not meaningfully changed.

---

## 7. Razor Page/API Endpoints

Recommended endpoints:

### Start report

```http
POST /api/report/start
```

Creates the report session after consent has been accepted.

### Send location

```http
POST /api/report/{reportId}/location
```

Receives the current GPS coordinate.

### End report

```http
POST /api/report/{reportId}/stop
```

Ends the location-sharing session.

### Active reports

```http
GET /api/dashboard/reports/active
```

Returns active reporters for initial dashboard loading.

### Report locations

```http
GET /api/dashboard/reports/{reportId}/locations
```

Returns the movement history needed to draw the trail.

### Latest location

```http
GET /api/dashboard/reports/{reportId}/location/latest
```

Returns the current position.

---

## 8. ASP.NET Core SignalR

Create:

```text
Hubs/MonitoringHub.cs
```

Register it in `Program.cs`:

```csharp
builder.Services.AddSignalR();

app.MapHub<MonitoringHub>("/hubs/monitoring");
```

When a location is received, the server should broadcast a `LocationUpdated` event to authorized dashboard clients.

```text
Reporter
   |
   | POST location
   v
ASP.NET Core
   |
   +--> MySQL
   |
   +--> SignalR
          |
          v
    Dashboard clients
```

The dashboard therefore does **not** need to refresh or repeatedly poll the database.

---

## 9. Google Maps Integration

The dashboard's map must use **Google Maps JavaScript API**.

Recommended map features:

- Current reporter marker
- Marker movement animation where appropriate
- Accuracy indication
- Movement trail/polyline
- Map auto-pan
- Zoom controls
- Full-screen map
- Optional traffic layer
- Optional operational layers

### Live marker flow

```javascript
function updateReporterMarker(location) {
    const position = {
        lat: location.latitude,
        lng: location.longitude
    };

    reporterMarker.position = position;
    map.panTo(position);
}
```

The marker should be updated from SignalR events rather than from the dashboard polling the database.

---

## 10. Dashboard Razor Page

Recommended route:

```text
/Dashboard
```

The dashboard should follow the UI/UX Pro Max principles requested for the project: clear hierarchy, restrained visual language, strong status states, accessible interactions, responsive layouts, and an operational map-first experience.

### Recommended layout

```text
+----------------------------------------------------------------+
| LOGO | EARLY WARNING MONITORING                 Alerts | User  |
+-------------+--------------------------------------------------+
|             |                                                  |
| Dashboard   |                LIVE GIS MAP                       |
| Live Reports|                                                  |
| Alerts      |       ● Report 001                              |
| History     |                         ● Report 002               |
| Risk Zones  |                                                  |
| Analytics   |                                                  |
| Users       |                                                  |
| Settings    |                                                  |
|             +--------------------------------------------------+
|             | LIVE REPORTS                                     |
|             | ID       STATUS    LAST UPDATE   ACTION         |
|             | REP-001  LIVE      4 sec ago     View Location  |
|             | REP-002  LIVE      8 sec ago     View Location  |
+-------------+--------------------------------------------------+
```

The map should receive the largest visual area because geographic awareness is the central purpose of the application.

---

## 11. View Location Workflow

When the dashboard user clicks **View Location**:

1. Validate dashboard authorization.
2. Load the selected report's latest coordinate.
3. Load recent movement history.
4. Open the Google Maps tracking view.
5. Place the reporter marker.
6. Draw the movement trail.
7. Subscribe the dashboard client to that report's SignalR updates.
8. Move the marker whenever a new coordinate arrives.
9. Update last-seen, accuracy, speed, heading, and status.

The operator should see:

```text
REP-2026-000001
● LIVE

Last update: 3 seconds ago
Accuracy: 8 m
Speed: 12 km/h
Heading: NE

[ CENTER ON REPORTER ]
[ STOP MONITORING ]
```

---

## 12. Live Report Status

Use both visual and textual status indicators.

| Status | Meaning |
|---|---|
| LIVE | Recent GPS update received |
| SIGNAL WEAK | GPS/network quality is poor |
| STALE | No recent coordinate within configured threshold |
| OFFLINE | Connection appears lost |
| ENDED | Reporter stopped sharing |

Do not communicate status through color alone.

---

## 13. Location Update Strategy

The system should balance responsiveness, GPS accuracy, network use, and battery consumption.

Initial configurable values:

```text
Target update interval: 5–10 seconds
Minimum movement: 10–20 metres
Stale threshold: 30–60 seconds
```

These are starting values, not fixed requirements. Field testing should determine the final configuration.

Each location record should include:

- Latitude
- Longitude
- Accuracy
- Timestamp
- Speed where available
- Heading where available
- Altitude where available

---

## 14. Real-Time Movement Trail

Do not only store the current coordinate.

For an active session:

```text
Point 1 → Point 2 → Point 3 → Point 4 → Point 5
```

The dashboard can represent these points as a Google Maps polyline.

```text
              ● Current
             /
            /
       ●---●
      /
 ●---●
```

The system should be able to display either:

- The complete session trail, or
- A configurable recent trail such as the last 30/60/120 minutes.

---

## 15. MySQL Spatial/GIS Capability

Although the primary application can operate using latitude and longitude columns, MySQL spatial capabilities should be used for the GIS functions that require geographic queries.

Potential operations include:

- Distance between two coordinates
- Reports within a radius
- Report clustering
- Risk-zone proximity
- Geofencing
- Hotspot detection

For example, the application can determine whether an incoming coordinate falls within a configured risk-zone radius before creating an alert.

The GIS service should isolate these calculations from the Razor Page code.

---

## 16. Early Warning Engine

The first release should focus on reliable real-time reporting. Once that is stable, introduce rules for early warning.

### Initial warning rules

#### Rule A — Multiple reports nearby

```text
IF
3 or more active reports
WITHIN 1 km
WITHIN 5 minutes
THEN
Generate WARNING alert
```

#### Rule B — High-risk zone

```text
IF
reporter enters high-risk zone
THEN
Generate HIGH alert
```

#### Rule C — Critical cluster

```text
IF
5 or more reports
WITHIN configured radius
WITHIN configured time window
THEN
Generate CRITICAL alert
```

All thresholds should be configurable rather than hard-coded.

---

## 17. Risk Zones

Authorized administrators should be able to configure geographic risk zones.

Example:

```text
LOW
MEDIUM
HIGH
CRITICAL
```

A risk zone may be represented by a center coordinate and radius for the MVP. More advanced polygon-based zones can be added later.

Dashboard layer controls:

```text
☑ Live Reports
☑ High-Risk Zones
☐ Historical Incidents
☐ Administrative Boundaries
☐ Roads
```

---

## 18. Alert Dashboard

Alerts should be immediately visible without overwhelming the operator.

Example:

```text
CRITICAL ALERT

5 live reports detected within 1.2 km
in the last 4 minutes.

[ VIEW CLUSTER ]
[ ACKNOWLEDGE ]
```

Alert severity:

```text
INFO
NOTICE
WARNING
HIGH
CRITICAL
```

---

## 19. Authentication and Authorization

Use ASP.NET Core Identity for dashboard users.

Suggested roles:

```text
System Administrator
Operations Manager
Response Officer
GIS Analyst
Viewer
```

Examples:

| Action | Admin | Manager | Officer | Analyst | Viewer |
|---|---:|---:|---:|---:|---:|
| View dashboard | ✓ | ✓ | ✓ | ✓ | ✓ |
| View live location | ✓ | ✓ | ✓ | ✓ | configurable |
| Manage alerts | ✓ | ✓ | ✓ | - | - |
| Manage risk zones | ✓ | ✓ | - | ✓ | - |
| Manage users | ✓ | - | - | - | - |
| View audit logs | ✓ | ✓ | configurable | ✓ | - |

SignalR connections must also be authorized. Do not assume that because a user can load a Razor Page they can automatically subscribe to every live location stream.

---

## 20. Privacy and Consent

Real-time location is sensitive information and must be treated accordingly.

Required controls:

- Explicit consent
- Versioned Terms and Conditions
- HTTPS
- Authenticated dashboard access
- Authorization checks
- Audit logs
- Location retention policy
- Secure database access
- Rate limiting
- No public access to live coordinates
- Ability for reporter to stop sharing

The public report page should not expose the reporter's coordinates to other public users.

---

## 21. Poor Network / Field Conditions

The system should expect unreliable mobile networks.

When connectivity drops:

```text
GPS continues
    |
    v
Temporary client queue
    |
    v
Network restored
    |
    v
Retry queued points
```

The server must retain the original GPS capture timestamp so that delayed points are not incorrectly presented as real-time points.

The dashboard should distinguish:

```text
LIVE
DELAYED
STALE
OFFLINE
```

---

## 22. Error Handling

### Permission denied

```text
Location access is required to share your live location.
Please enable location access in your browser/device settings.
```

### GPS unavailable

```text
We cannot determine your current location.
Please move to an area with better GPS reception.
```

### Network unavailable

```text
Connection interrupted.
We are trying to reconnect automatically.
```

### Session ended

```text
Location sharing has ended.
Your live location is no longer being transmitted.
```

---

## 23. Security Requirements for the API

Every location endpoint should validate:

- Report ID exists
- Report is active
- Session is not expired
- Latitude is valid
- Longitude is valid
- Accuracy is reasonable
- Timestamp is valid
- Request rate is within limits

Do not trust coordinates supplied by the browser.

Example validation:

```text
Latitude:  -90 to +90
Longitude: -180 to +180
Accuracy:  >= 0
```

---

## 24. Google Maps API Key Management

Use separate keys/configuration for:

```text
Development
Staging
Production
```

Store configuration securely using ASP.NET Core configuration/secrets mechanisms. Do not commit unrestricted production keys to source control.

The production key should be restricted to the appropriate application origins/APIs.

---

## 25. UI/UX Implementation Rules

The interface should follow the requested UI/UX Pro Max principles.

### Reporter

- Mobile-first
- One dominant action
- Large touch target
- Minimal text
- High contrast
- Clear state transitions
- No unnecessary fields
- No decorative clutter

### Dashboard

- Map-first layout
- Clear hierarchy
- Consistent spacing
- Strong status indicators
- Responsive sidebar
- Search/filter where useful
- Alerts visually prioritized
- Keyboard accessible controls
- Avoid excessive cards and decorative gradients

### Interaction principle

Every screen should answer:

1. **What is happening?**
2. **Where is it happening?**
3. **How serious is it?**
4. **What can I do next?**

---

## 26. Development Phases

### Phase 1 — ASP.NET Core Foundation

- Create Razor Pages project
- Configure MySQL
- Configure EF Core
- Configure Identity
- Create base layout
- Create navigation
- Create UI design system

### Phase 2 — Reporter Module

- `/Report`
- Report button
- Terms modal
- Consent storage
- GPS permission
- `watchPosition()`
- Location API
- Stop sharing

### Phase 3 — Live Dashboard

- `/Dashboard`
- Active reports
- SignalR hub
- Live status
- Real-time report list
- Notification system

### Phase 4 — Google Maps

- Google Maps integration
- Current marker
- Marker updates
- Movement trail
- Accuracy
- Speed/heading
- Auto-center

### Phase 5 — GIS

- MySQL spatial queries
- Distance calculation
- Risk zones
- Geofencing
- Spatial clustering
- Historical GIS

### Phase 6 — Early Warning

- Rule engine
- Risk assessment
- Alert generation
- Alert acknowledgement
- Escalation workflow

### Phase 7 — Analytics

- Historical reports
- Heat maps
- Trends
- Hotspots
- Response times
- Reports/export

---

## 27. MVP Acceptance Criteria

The MVP should not be considered complete until the following workflow works end-to-end:

### Reporter

- [ ] User opens `/Report`.
- [ ] User sees one primary report action.
- [ ] User clicks the report button.
- [ ] Terms and Conditions are displayed.
- [ ] User accepts location sharing.
- [ ] Browser requests location permission.
- [ ] Report session is created.
- [ ] GPS tracking begins.
- [ ] Coordinates are sent to ASP.NET Core.
- [ ] Coordinates are stored in MySQL.
- [ ] Reporter sees live-sharing status.
- [ ] Reporter can stop sharing.

### Dashboard

- [ ] Authorized operator signs in.
- [ ] Dashboard loads active reports.
- [ ] New report appears without page refresh.
- [ ] Latest coordinate appears in real time.
- [ ] Operator clicks **View Location**.
- [ ] Google Maps opens.
- [ ] Current reporter marker appears.
- [ ] New coordinates move the marker.
- [ ] Movement trail updates.
- [ ] Last update time changes.
- [ ] Operator can identify stale/offline sessions.

### Backend

- [ ] API validates incoming coordinates.
- [ ] SignalR broadcasts authorized updates.
- [ ] MySQL stores report/session/location history.
- [ ] Authentication is enforced for dashboard access.
- [ ] Authorization is enforced for live-location access.
- [ ] Audit logging records sensitive dashboard actions.

---

## 28. Recommended First Development Order

Do not begin with advanced predictive analytics. Build the system in this order:

```text
1. MySQL database
        ↓
2. Report session API
        ↓
3. Reporter GPS tracking
        ↓
4. Store coordinates
        ↓
5. SignalR
        ↓
6. Dashboard live report list
        ↓
7. Google Maps
        ↓
8. Live marker movement
        ↓
9. Movement trail
        ↓
10. Risk zones
        ↓
11. Early warning rules
        ↓
12. Alerts
        ↓
13. Historical GIS analytics
```

This sequence reduces project risk because the most important real-time workflow is proven before the more complex GIS intelligence is introduced.

---

## 29. Final Target Architecture

```text
                    REPORTER MOBILE BROWSER
                              |
                    HTML + CSS + JavaScript
                              |
                     Browser Geolocation
                              |
                       watchPosition()
                              |
                              v
                 ASP.NET CORE RAZOR PAGES
                              |
             +----------------+----------------+
             |                                 |
             v                                 v
       Report/Location API                 SignalR Hub
             |                                 |
             v                                 v
          MySQL                         OPERATIONS DASHBOARD
             |                                 |
             |                           +-----+-----+
             |                           |           |
             v                           v           v
       Location History            Live Reports  Google Maps
             |                                       |
             v                                       v
       GIS / Risk Engine                    Live Marker + Trail
             |
             v
       EARLY WARNING ALERTS
             |
             v
       RESPONSE / RESOLUTION
             |
             v
       HISTORICAL RECORD
```

## 30. Final Product Principle

The system should hide its technical complexity from the person reporting a potential conflict.

The reporter experience is:

> **Report → Consent → Share Location → Continue Moving → Stop Sharing**

The operational experience is:

> **Receive → Locate → Monitor → Detect Pattern → Alert → Respond → Resolve → Analyze**

That separation is central to the system design: **the public reporting experience must be extremely simple, while the authorized dashboard provides the sophisticated real-time GIS intelligence required for early warning and conflict response.**
