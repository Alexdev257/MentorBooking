// Generator — không import file .mjs vào Postman. Chạy: node tools/build_postman_collection.mjs
// Collection để import: MentorBooking.postman_collection.json
import fs from "fs";

const BASE = "https://api-gateway-lfnu.onrender.com";
const H = [BASE];

function u(segments, query) {
  const path = segments.join("/");
  let raw = `${BASE}/${path}`;
  const o = { raw, host: H, path: segments };
  if (query?.length) {
    const qs = query.map(([k, v]) => `${k}=${v}`).join("&");
    raw += "?" + qs;
    o.raw = raw;
    o.query = query.map(([key, value]) => ({ key, value }));
  }
  return o;
}

const collection = {
  info: {
    name: "MentorBooking API (Gateway)",
    schema: "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
    description:
      "## API Gateway — tất cả service\n\n**Base URL:** `https://api-gateway-lfnu.onrender.com`\n\n### Biến collection\nĐiền `mentor_id`, `student_id`, `slot_id`, `booking_id`, `meeting_id`, `user_id`, `review_id`, `recording_id` khi test từng nhóm. `token` / `refresh_token` được gán sau **Login**.\n\n### Auth — Refresh token\nServer chỉ chấp nhận refresh khi **access token đã hết hạn**. Ngay sau Login, request **Refresh** thường trả **400** — hành vi đúng với code hiện tại (không phải lỗi cấu hình Postman).\n\n### Thứ tự gợi ý\n1. Login\n2. Auth / Admin / Users / Mentors / Reviews / Test\n3. Booking: zoom-token → Slots → Booking\n4. Meeting\n5. AI Transcript\n6. Zoom webhook\n\n**Ghi chú:** EmailService không có REST public qua gateway.",
  },
  variable: [
    { key: "token", value: "", description: "JWT — sau Login" },
    { key: "refresh_token", value: "", description: "Sau Login" },
    { key: "mentor_id", value: "", description: "Guid mentor" },
    { key: "student_id", value: "", description: "Guid mentee" },
    { key: "slot_id", value: "", description: "Guid slot" },
    { key: "booking_id", value: "", description: "Guid booking" },
    { key: "meeting_id", value: "", description: "Guid meeting" },
    { key: "recording_id", value: "", description: "Guid recording" },
    { key: "user_id", value: "", description: "Guid — GET /api/users/{id}" },
    { key: "review_id", value: "", description: "Guid review" },
    { key: "transcript_id", value: "", description: "Sau upload/ingest" },
    { key: "video_url", value: "", description: "URL video public" },
    { key: "zoom_meeting_id", value: "", description: "Zoom = Booking.GoogleEventId" },
    { key: "_ts", value: "", description: "Auto Simulate Zoom" },
  ],
  auth: {
    type: "bearer",
    bearer: [{ key: "token", value: "{{token}}", type: "string" }],
  },
  item: [],
};

const loginTests = [
  "pm.test('Status 200', function () { pm.response.to.have.status(200); });",
  "var json = pm.response.json();",
  "var data = json.data || json;",
  "var token = (json.data && json.data.accessToken) || json.accessToken || (data && data.accessToken);",
  "if (token) { pm.collectionVariables.set('token', token); console.log('Token saved'); }",
  "var rt = (json.data && json.data.refreshToken) || (data && data.refreshToken);",
  "if (rt) { pm.collectionVariables.set('refresh_token', rt); }",
];

// AuthService JwtHelper: refresh chỉ thành công khi access token ĐÃ HẾT HẠN — ngay sau Login thường 400 (expected).
const refreshTests = [
  "pm.test('Refresh: 200 (đổi token) hoặc 400 khi access token vẫn còn hạn (server)', function () {",
  "    pm.expect([200, 400]).to.include(pm.response.code);",
  "});",
  "if (pm.response.code === 200) {",
  "    var json = pm.response.json();",
  "    var d = json.data || {};",
  "    if (d.accessToken) pm.collectionVariables.set('token', d.accessToken);",
  "    if (d.refreshToken) pm.collectionVariables.set('refresh_token', d.refreshToken);",
  "    console.log('Refresh OK — token đã cập nhật');",
  "} else if (pm.response.code === 400) {",
  "    var j = pm.response.json();",
  "    console.log('Refresh 400 (bình thường sau Login): ' + (j.message || JSON.stringify(j)));",
  "}",
];

const wakeupTests = [
  "pm.test('Wakeup 200', function () { pm.response.to.have.status(200); });",
];

const logoutTests = [
  "pm.test('Logout 200', function () { pm.response.to.have.status(200); });",
];

collection.item.push({
  name: "1. Auth",
  item: [
    {
      name: "Login",
      event: [{ listen: "test", script: { type: "text/javascript", exec: loginTests } }],
      request: {
        auth: { type: "noauth" },
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: {
          mode: "raw",
          raw: JSON.stringify({ email: "admin@gmail.com", password: "admin" }, null, 2),
        },
        url: u(["api", "auth", "login"]),
        description: "Đăng nhập — lưu token + refresh_token.",
      },
    },
    {
      name: "Wakeup (anti sleep)",
      event: [{ listen: "test", script: { type: "text/javascript", exec: wakeupTests } }],
      request: {
        auth: { type: "noauth" },
        method: "GET",
        header: [],
        url: u(["api", "auth"]),
        description: "GET /api/auth",
      },
    },
    {
      name: "Refresh token",
      event: [{ listen: "test", script: { type: "text/javascript", exec: refreshTests } }],
      request: {
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: {
          mode: "raw",
          raw: JSON.stringify({ accessToken: "{{token}}", refreshToken: "{{refresh_token}}" }, null, 2),
        },
        url: u(["api", "auth", "refresh"]),
        description:
          "Backend chỉ refresh khi access token **đã hết hạn** (JwtHelper). Ngay sau Login token còn ~1h → thường **400** `\"Access Token has not expired yet!\"` — đúng thiết kế hiện tại, không phải lỗi Postman.",
      },
    },
    {
      name: "Logout",
      event: [{ listen: "test", script: { type: "text/javascript", exec: logoutTests } }],
      request: {
        method: "POST",
        header: [],
        url: u(["api", "auth", "logout"]),
      },
    },
  ],
});

collection.item.push({
  name: "2. Admin",
  item: [
    { name: "List students", request: { method: "GET", header: [], url: u(["api", "admin", "students"], [["pageNumber", "1"], ["pageSize", "10"]]) } },
    {
      name: "Get student by id",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/admin/students/{{student_id}}`, host: H, path: ["api", "admin", "students", "{{student_id}}"] },
      },
    },
    { name: "List teachers", request: { method: "GET", header: [], url: u(["api", "admin", "teachers"], [["pageNumber", "1"], ["pageSize", "10"]]) } },
    {
      name: "Get teacher by id",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/admin/teachers/{{mentor_id}}`, host: H, path: ["api", "admin", "teachers", "{{mentor_id}}"] },
      },
    },
  ],
});

collection.item.push({
  name: "3. Users (internal)",
  item: [
    {
      name: "Get user by id",
      request: {
        auth: { type: "noauth" },
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/users/{{user_id}}`, host: H, path: ["api", "users", "{{user_id}}"] },
      },
    },
  ],
});

collection.item.push({
  name: "4. Mentors (public)",
  item: [
    {
      name: "List mentors",
      request: { auth: { type: "noauth" }, method: "GET", header: [], url: u(["api", "mentors"], [["pageNumber", "1"], ["pageSize", "10"]]) },
    },
    {
      name: "Get mentor by id",
      request: {
        auth: { type: "noauth" },
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/mentors/{{mentor_id}}`, host: H, path: ["api", "mentors", "{{mentor_id}}"] },
      },
    },
  ],
});

const reviewCreate =
  '{\n    "bookingId": "{{booking_id}}",\n    "mentorId": "{{mentor_id}}",\n    "rating": 5,\n    "comment": "Test review"\n}';
const reviewUpdate = '{\n    "rating": 5,\n    "comment": "Updated"\n}';

collection.item.push({
  name: "5. Reviews",
  item: [
    {
      name: "List reviews",
      request: {
        auth: { type: "noauth" },
        method: "GET",
        header: [],
        url: u(["api", "reviews"], [["pageNumber", "1"], ["pageSize", "10"], ["mentorId", "{{mentor_id}}"]]),
      },
    },
    {
      name: "Get review by mentor (student)",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/reviews/{{mentor_id}}`, host: H, path: ["api", "reviews", "{{mentor_id}}"] },
        description: "Bearer — role Student.",
      },
    },
    {
      name: "Create review",
      request: {
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: { mode: "raw", raw: reviewCreate },
        url: u(["api", "reviews"]),
      },
    },
    {
      name: "Update review",
      request: {
        method: "PUT",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: { mode: "raw", raw: reviewUpdate },
        url: { raw: `${BASE}/api/reviews/{{review_id}}`, host: H, path: ["api", "reviews", "{{review_id}}"] },
      },
    },
    {
      name: "Delete review",
      request: {
        method: "DELETE",
        header: [],
        url: { raw: `${BASE}/api/reviews/{{review_id}}`, host: H, path: ["api", "reviews", "{{review_id}}"] },
      },
    },
  ],
});

collection.item.push({
  name: "6. Test (storage)",
  item: [
    {
      name: "Upload avatar (test)",
      request: {
        auth: { type: "noauth" },
        method: "POST",
        header: [],
        body: { mode: "formdata", formdata: [{ key: "file", type: "file", src: "", description: "Chọn ảnh" }] },
        url: u(["api", "test", "upload-avatar"]),
      },
    },
    {
      name: "Delete file by URL",
      request: {
        auth: { type: "noauth" },
        method: "DELETE",
        header: [],
        url: u(["api", "test", "delete-by-url"], [["fileUrl", "https://example.com/file.png"]]),
      },
    },
  ],
});

const createBooking =
  '{\n    "mentorId": "{{mentor_id}}",\n    "slotId": "{{slot_id}}",\n    "topic": "Test booking",\n    "notes": "",\n    "priceAmount": 0,\n    "currency": "VND",\n    "invitedEmails": []\n}';
const createSlot =
  '{\n    "startAt": "2026-12-01T09:00:00Z",\n    "endAt": "2026-12-01T10:00:00Z"\n}';

collection.item.push({
  name: "7. Booking",
  item: [
    { name: "Get Zoom access token (debug)", request: { auth: { type: "noauth" }, method: "GET", header: [], url: u(["api", "booking", "zoom-token"]) } },
    {
      name: "Create booking",
      request: {
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: { mode: "raw", raw: createBooking },
        url: u(["api", "booking", "bookings"]),
        description: "Mentee — JWT là student.",
      },
    },
    {
      name: "Get booking by id",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/booking/bookings/{{booking_id}}`, host: H, path: ["api", "booking", "bookings", "{{booking_id}}"] },
      },
    },
    {
      name: "List mentee bookings",
      request: {
        method: "GET",
        header: [],
        url: u(["api", "booking", "mentees", "{{student_id}}", "bookings"], [["pageNumber", "1"], ["pageSize", "10"]]),
        description: "JWT phải trùng student_id.",
      },
    },
    {
      name: "List mentor bookings",
      request: {
        method: "GET",
        header: [],
        url: u(["api", "booking", "mentors", "{{mentor_id}}", "bookings"], [["pageNumber", "1"], ["pageSize", "10"]]),
        description: "JWT phải trùng mentor_id.",
      },
    },
    {
      name: "Accept booking (mentor)",
      request: {
        method: "PATCH",
        header: [],
        url: { raw: `${BASE}/api/booking/bookings/{{booking_id}}/accept`, host: H, path: ["api", "booking", "bookings", "{{booking_id}}", "accept"] },
      },
    },
    {
      name: "Reject booking (mentor)",
      request: {
        method: "PATCH",
        header: [],
        url: { raw: `${BASE}/api/booking/bookings/{{booking_id}}/reject`, host: H, path: ["api", "booking", "bookings", "{{booking_id}}", "reject"] },
      },
    },
    {
      name: "Cancel booking",
      request: {
        method: "PATCH",
        header: [],
        url: { raw: `${BASE}/api/booking/bookings/{{booking_id}}/cancel`, host: H, path: ["api", "booking", "bookings", "{{booking_id}}", "cancel"] },
      },
    },
  ],
});

collection.item.push({
  name: "8. Slots (mentor)",
  item: [
    {
      name: "List slots",
      request: { method: "GET", header: [], url: u(["api", "booking", "mentors", "{{mentor_id}}", "slots"], [["includeBooked", "false"]]) },
    },
    {
      name: "Get slot by id",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/booking/mentors/{{mentor_id}}/slots/{{slot_id}}`, host: H, path: ["api", "booking", "mentors", "{{mentor_id}}", "slots", "{{slot_id}}"] },
      },
    },
    {
      name: "Create slot",
      request: {
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: { mode: "raw", raw: createSlot },
        url: { raw: `${BASE}/api/booking/mentors/{{mentor_id}}/slots`, host: H, path: ["api", "booking", "mentors", "{{mentor_id}}", "slots"] },
        description: "JWT = mentor_id.",
      },
    },
    {
      name: "Update slot",
      request: {
        method: "PUT",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: { mode: "raw", raw: createSlot },
        url: { raw: `${BASE}/api/booking/mentors/{{mentor_id}}/slots/{{slot_id}}`, host: H, path: ["api", "booking", "mentors", "{{mentor_id}}", "slots", "{{slot_id}}"] },
      },
    },
    {
      name: "Delete slot",
      request: {
        method: "DELETE",
        header: [],
        url: { raw: `${BASE}/api/booking/mentors/{{mentor_id}}/slots/{{slot_id}}`, host: H, path: ["api", "booking", "mentors", "{{mentor_id}}", "slots", "{{slot_id}}"] },
      },
    },
  ],
});

collection.item.push({
  name: "9. Meeting",
  item: [
    { name: "List meetings", request: { method: "GET", header: [], url: u(["api", "meeting", "meetings"], [["pageNumber", "1"], ["pageSize", "10"]]) } },
    {
      name: "Get meeting by id",
      request: { method: "GET", header: [], url: { raw: `${BASE}/api/meeting/meetings/{{meeting_id}}`, host: H, path: ["api", "meeting", "meetings", "{{meeting_id}}"] } },
    },
    {
      name: "Get meeting by booking id",
      request: { method: "GET", header: [], url: { raw: `${BASE}/api/meeting/by-booking/{{booking_id}}`, host: H, path: ["api", "meeting", "by-booking", "{{booking_id}}"] } },
    },
    {
      name: "Recordings by booking id",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/meeting/by-booking/{{booking_id}}/recordings`, host: H, path: ["api", "meeting", "by-booking", "{{booking_id}}", "recordings"] },
      },
    },
    {
      name: "Recordings by meeting id",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/meeting/meetings/{{meeting_id}}/recordings`, host: H, path: ["api", "meeting", "meetings", "{{meeting_id}}", "recordings"] },
      },
    },
    {
      name: "Get recording by id",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/meeting/recordings/{{recording_id}}`, host: H, path: ["api", "meeting", "recordings", "{{recording_id}}"] },
      },
    },
    {
      name: "Join links",
      request: {
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/meeting/meetings/{{meeting_id}}/join-links`, host: H, path: ["api", "meeting", "meetings", "{{meeting_id}}", "join-links"] },
      },
    },
  ],
});

const trUploadTests = [
  "pm.test('202', function () { pm.response.to.have.status(202); });",
  "var json = pm.response.json();",
  "if (json.data && json.data.id) { pm.collectionVariables.set('transcript_id', json.data.id); }",
];
const trFileTests = [
  "pm.test('202', function () { pm.response.to.have.status(202); });",
  "var json = pm.response.json();",
  "if (json.data && json.data.id) { pm.collectionVariables.set('transcript_id', json.data.id); }",
];
const trGetTests = [
  "pm.test('200', function () { pm.response.to.have.status(200); });",
  "var json = pm.response.json();",
  "if (json.isSuccess && json.data) console.log('status', json.data.status);",
];
const trListTests = ["pm.test('200', function () { pm.response.to.have.status(200); });"];
const sumTests = ["pm.test('202', function () { pm.response.to.have.status(202); });"];
const ingestTests = [
  "pm.test('201', function () { pm.response.to.have.status(201); });",
  "var json = pm.response.json();",
  "if (json.data && json.data.transcriptId) pm.collectionVariables.set('transcript_id', json.data.transcriptId);",
];

collection.item.push({
  name: "10. AI — Transcript",
  item: [
    {
      name: "Upload from URL",
      event: [{ listen: "test", script: { type: "text/javascript", exec: trUploadTests } }],
      request: {
        auth: { type: "noauth" },
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: {
          mode: "raw",
          raw: JSON.stringify({ url: "{{video_url}}", title: "Test Zoom Recording", sourceType: 3, contentType: "video/mp4" }, null, 2),
        },
        url: u(["api", "transcripts", "upload-from-url"]),
      },
    },
    {
      name: "Upload file",
      event: [{ listen: "test", script: { type: "text/javascript", exec: trFileTests } }],
      request: {
        auth: { type: "noauth" },
        method: "POST",
        header: [],
        body: {
          mode: "formdata",
          formdata: [
            { key: "file", type: "file", src: "" },
            { key: "title", value: "Test Transcript", type: "text" },
            { key: "sourceType", value: "1", type: "text" },
          ],
        },
        url: u(["api", "transcripts", "upload"]),
      },
    },
    {
      name: "Get transcript by id",
      event: [{ listen: "test", script: { type: "text/javascript", exec: trGetTests } }],
      request: {
        auth: { type: "noauth" },
        method: "GET",
        header: [],
        url: { raw: `${BASE}/api/transcripts/{{transcript_id}}`, host: H, path: ["api", "transcripts", "{{transcript_id}}"] },
      },
    },
    {
      name: "List transcripts",
      event: [{ listen: "test", script: { type: "text/javascript", exec: trListTests } }],
      request: { method: "GET", header: [], url: u(["api", "transcripts"], [["pageNumber", "1"], ["pageSize", "20"]]) },
    },
    {
      name: "Re-summarize",
      event: [{ listen: "test", script: { type: "text/javascript", exec: sumTests } }],
      request: {
        auth: { type: "noauth" },
        method: "POST",
        header: [],
        url: { raw: `${BASE}/api/transcripts/{{transcript_id}}/summarize`, host: H, path: ["api", "transcripts", "{{transcript_id}}", "summarize"] },
      },
    },
    {
      name: "Ingest zoom-audio (debug)",
      event: [{ listen: "test", script: { type: "text/javascript", exec: ingestTests } }],
      request: {
        auth: { type: "noauth" },
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: {
          mode: "raw",
          raw: JSON.stringify(
            {
              bookingId: "00000000-0000-0000-0000-000000000001",
              meetingId: "test-meeting-001",
              title: "Test Ingest",
              rawText: "Hello world.",
              cleanText: "Hello world.",
              segments: [{ startSeconds: 0, endSeconds: 2, text: "Hello world." }],
            },
            null,
            2
          ),
        },
        url: u(["api", "transcripts", "ingest", "zoom-audio"]),
      },
    },
  ],
});

const zoomPre = ["pm.collectionVariables.set('_ts', Date.now());"];
const zoomTest = ["pm.test('200', function () { pm.response.to.have.status(200); });"];

collection.item.push({
  name: "11. Zoom webhook",
  item: [
    { name: "Health GET /api/zoom/wh", request: { auth: { type: "noauth" }, method: "GET", header: [], url: u(["api", "zoom", "wh"]) } },
    {
      name: "Simulate recording.completed",
      event: [
        { listen: "prerequest", script: { type: "text/javascript", exec: zoomPre } },
        { listen: "test", script: { type: "text/javascript", exec: zoomTest } },
      ],
      request: {
        auth: { type: "noauth" },
        method: "POST",
        header: [{ key: "Content-Type", value: "application/json" }],
        body: {
          mode: "raw",
          raw:
            '{\n    "event": "recording.completed",\n    "event_ts": {{_ts}},\n    "payload": {\n        "object": {\n            "id": "{{zoom_meeting_id}}",\n            "uuid": "test-uuid-simulation",\n            "topic": "Test",\n            "recording_files": [\n                {\n                    "id": "sim-001",\n                    "meeting_id": "{{zoom_meeting_id}}",\n                    "file_type": "MP4",\n                    "download_url": "{{video_url}}",\n                    "play_url": "{{video_url}}",\n                    "file_size": 52428800,\n                    "recording_start": "2026-04-01T10:00:00Z",\n                    "recording_end": "2026-04-01T10:30:00Z"\n                }\n            ]\n        }\n    }\n}',
        },
        url: u(["api", "zoom", "wh"]),
      },
    },
  ],
});

collection.item.push({
  name: "12. Sample (BookingService)",
  item: [
    {
      name: "WeatherForecast",
      request: {
        auth: { type: "noauth" },
        method: "GET",
        header: [],
        url: u(["WeatherForecast"]),
        description: "Template API — smoke test.",
      },
    },
  ],
});

const out = "MentorBooking.postman_collection.json";
fs.writeFileSync(out, JSON.stringify(collection, null, 2), "utf8");
console.log("Wrote", out, "(import file này vào Postman)");
