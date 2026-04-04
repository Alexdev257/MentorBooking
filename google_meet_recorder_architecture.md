# Google Meet Automatic Recording Bot -- System Design & Implementation Guide (MentorBooking)

> **Deprecated in repo:** The Node.js recorder bot (`MeetingRecordingBot`), `RecordMeetingCommand`, and `MeetingRecordingCompletedEvent` have been **removed**. Meeting status is now driven by **Zoom webhooks** + `ZoomMeetingLifecycleEvent` (see `instruction_booking_meeting.md`). This file is kept as historical design notes only.

## Overview

This document explains the architecture for building a system that can automatically join and record a Google Meet session for the **MentorBooking** project without requiring a premium Google Workspace account.

Instead of relying on Google's native auto-record feature, the system uses a **headless browser bot** to join the meeting as a participant, record the session, upload it directly to **Firebase Storage**, and sync the data back to the `MeetingService` and `AIService`.

------------------------------------------------------------------------

# 1. System Architecture

```text
    BookingService 
       │ (1) Accept Booking -> Event Published
       ▼
    RabbitMQ (Message Bus)
       │ (2) Consume Event
       ▼
    MeetingService (.NET 9 Web API)
       │ (3) Creates `Meeting` Entity
       │ (4) Dispatch Recording Job when schedule reached
       ▼
    RabbitMQ Queue (e.g., "record-meeting-queue")
       │ (5) Pop Job
       ▼
    Recorder Worker (Node.js + Puppeteer)
       │ (6) Launch headless Chrome
       │ (7) Join Google Meet (`JoinUrl`)
       │ (8) Record audio/video using FFmpeg
       ▼
    Firebase Storage
       │ (9) Upload .mp4 file
       ▼
    Recorder Worker (Node.js + Puppeteer)
       │ (10) Publish Success Event + Firebase `StorageUrl`
       ▼
    MeetingService (.NET 9 Web API)
       │ (11) Create `MeetingRecording` Entity
       ▼
    AIService (.NET Core)
       │ (12) Extract audio & Generate `MeetingSummary` (Optional)
```

Each meeting recording runs as a separate worker instance, allowing the system to scale and record multiple Mentor/Mentee sessions simultaneously.

------------------------------------------------------------------------

# 2. Required Technologies

*   **Backend:** ASP.NET Core Web API 9.0 (`MeetingService`, `AIService`, `BookingService`)
*   **Database:** PostgreSQL / SQL Server (Storing `Meeting`, `MeetingRecording`, `Booking`)
*   **Message Broker:** RabbitMQ 
*   **Bot Worker:** Node.js + Puppeteer (Browser automation)
*   **Recording Engine:** FFmpeg + Xvfb (Virtual display for headless servers)
*   **Storage:** Firebase Storage (Bucket)
*   **Deployment:** Docker (Required for Xvfb and FFmpeg dependencies)

------------------------------------------------------------------------

# 3. Backend (.NET MeetingService) Implementation

## 3.1 Entities

The `MeetingService` utilizes the following entities to track the recording state:

```csharp
public class Meeting : AuditableEntity
{
    public Guid BookingId { get; set; }
    public int Status { get; set; }     
    public string Provider { get; set; } // e.g., "GoogleMeet"
    public string? JoinUrl { get; set; } // Google Meet Link
    public string? HostUrl { get; set; } 
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public virtual ICollection<MeetingRecording> Recordings { get; set; } = new List<MeetingRecording>();
}

public class MeetingRecording : AuditableEntity
{
    public Guid MeetingId { get; set; }
    public virtual Meeting Meeting { get; set; }
    public int Status { get; set; } 
    public string StorageUrl { get; set; } // Firebase public URL
    public string? ContentType { get; set; } 
    public int? DurationSeconds { get; set; }
    public long? SizeBytes { get; set; } 
}
```

## 3.2 Triggering the Bot

Instead of an API endpoint, the system uses a Background Service (like Hangfire/Quartz) inside `MeetingService` to publish a command to RabbitMQ exactly when the meeting is scheduled to start:

```csharp
// Example using MassTransit / RabbitMQ
var recordCommand = new RecordMeetingCommand
{
    MeetingId = meeting.Id,
    JoinUrl = meeting.JoinUrl, // e.g., "https://meet.google.com/abc-defg-hij"
    DurationMinutes = 60 // Max duration just in case
};

await _publishEndpoint.Publish(recordCommand);
```

------------------------------------------------------------------------

# 4. Message Broker (RabbitMQ)

`MeetingService` pushes the recording jobs to a RabbitMQ queue (`record-meeting-queue`). This allows the heavy Node.js bot workers to pull jobs asynchronously and process them without blocking the .NET backend.

------------------------------------------------------------------------

# 5. Bot Worker Implementation (Node.js)

The Worker is a separate Node.js application running inside a Docker container equipped with FFmpeg and Chrome.

Install dependencies:
```bash
npm init -y
npm install puppeteer amqplib firebase-admin
```

## 5.1 Worker Loop (Consuming RabbitMQ)

The worker listens for new `RecordMeetingCommand` messages.

```javascript
const amqp = require('amqplib');

async function startWorker() {
    const conn = await amqp.connect('amqp://localhost'); // MentorBooking RabbitMQ
    const ch = await conn.createChannel();
    await ch.assertQueue('record-meeting-queue');

    ch.consume('record-meeting-queue', async (msg) => {
        if (msg !== null) {
            const data = JSON.parse(msg.content.toString());
            await startRecorder(data.JoinUrl, data.MeetingId);
            ch.ack(msg);
        }
    });
}
```

------------------------------------------------------------------------

# 6. Launch Chrome Bot

Use Puppeteer to automate Chrome. 
*Note: In Docker/Linux, you need Xvfb to simulate a display for screen recording.*

```javascript
const puppeteer = require("puppeteer");

async function startRecorder(meetUrl, meetingId) {
   const browser = await puppeteer.launch({
      headless: false, // In Docker, this is running inside Xvfb
      args:[
        "--no-sandbox",
        "--use-fake-ui-for-media-stream",
        "--window-size=1920,1080"
      ]
   });

   const page = await browser.newPage();
   await page.goto(meetUrl);
   // Proceed to join and record...
}
```

------------------------------------------------------------------------

# 7. Join Google Meet & Bypass Constraints

The bot must navigate the Google Meet UI.

1.  Open the `JoinUrl`.
2.  Type a predefined name (e.g., "MentorBooking Recorder").
3.  Mute Microphone and disable Camera.
4.  Click "Ask to join" or "Join now".
5.  Wait for the Mentor (Host) to admit the bot into the room.

```javascript
await page.type('input[type="text"]', 'MentorBooking System');

// Mute mic and cam using keyboard shortcuts
await page.keyboard.down('Control');
await page.keyboard.press('d'); // Mute Mic
await page.keyboard.press('e'); // Turn off Cam
await page.keyboard.up('Control');

// Find and click the Join button
const buttons = await page.$$('button');
for(const btn of buttons){
    const text = await page.evaluate(el => el.innerText, btn);
    if(text.includes("Ask to join") || text.includes("Join now")){
        await btn.click();
        break;
    }
}
```

------------------------------------------------------------------------

# 8. Recording Meeting

Use **FFmpeg** to record the virtual screen and system pulse audio.

Execute a shell command from Node.js:
```javascript
const { spawn } = require('child_process');

// Start FFmpeg recording
const outputPath = `/tmp/${meetingId}.mp4`;
const ffmpeg = spawn('ffmpeg', [
    '-video_size', '1920x1080',
    '-framerate', '30',
    '-f', 'x11grab',
    '-i', ':99.0', // Xvfb display
    '-f', 'pulse',
    '-i', 'default', // System audio
    outputPath
]);
```

------------------------------------------------------------------------

# 9. Detect Meeting End

The bot should stop FFmpeg when the meeting finishes to save resources.

Strategies:
1.  **Participant Detection**: Read the DOM periodically. If participant count drops to 1 (only the bot left), kill FFmpeg and close the browser.
2.  **Timeout limit**: Automatically stop after `DurationMinutes` + grace period.

```javascript
ffmpeg.kill('SIGINT'); // Gracefully stop recording and finalize mp4
await browser.close();
```

------------------------------------------------------------------------

# 10. Upload Recording to Firebase Storage

After FFmpeg creates the `.mp4` file, the bot uses `firebase-admin` to upload it directly to your Firebase bucket.

```javascript
const admin = require('firebase-admin');
admin.initializeApp({
  credential: admin.credential.cert(serviceAccount),
  storageBucket: "mentorbooking-xxx.appspot.com"
});

const bucket = admin.storage().bucket();
const destination = `recordings/${meetingId}.mp4`;

await bucket.upload(`/tmp/${meetingId}.mp4`, {
    destination: destination,
    metadata: { contentType: 'video/mp4' }
});

// Configure bucket to allow reading and get public URL
const file = bucket.file(destination);
await file.makePublic();
const storageUrl = file.publicUrl();
```

------------------------------------------------------------------------

# 11. Complete the Cycle (MeetingService & AIService)

Once the upload is successful, the Worker publishes an event back to RabbitMQ:

```json
{
 "MeetingId": "123e4567-e89b-12d3...",
 "Status": 1, 
 "StorageUrl": "https://storage.googleapis.com/.../recordings/123.mp4",
 "DurationSeconds": 3600,
 "SizeBytes": 150485760
}
```

1.  **`MeetingService`** consumes this event and creates the `MeetingRecording` database entity. The Mentee can now view the recorded video in the app via the `StorageUrl`.
2.  **(Optional) `AIService`** consumes this same event, downloads the audio from the video URL, runs speech-to-text, and saves a `MeetingSummary` to the database.

------------------------------------------------------------------------

# 12. Deployment Summary

To run this in staging/production, it's highly recommended to use **Docker Compose** or Kubernetes:

```text
    Docker Environment
         │
         ├ -> MeetingService (.NET 9)
         ├ -> RabbitMQ
         ├ -> PostgreSQL
         ├ -> Recorder Bot Worker (Node.js + Puppeteer + Xvfb + FFmpeg)
```

The Dockerfile for the Recorder Bot must inherit from a Linux image that installs:
`chromium-browser`, `ffmpeg`, `xvfb`, `pulseaudio`.

------------------------------------------------------------------------

# Conclusion

By combining .NET 9, RabbitMQ, Firebase Storage, and a Node+Puppeteer worker, the MentorBooking system achieves a fully automated Google Meet recording pipeline. 
This bypasses the strict Google Workspace premium requirements and provides complete control over video storage and AI processing.
