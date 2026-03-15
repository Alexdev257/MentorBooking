const puppeteer = require('puppeteer');
const amqp = require('amqplib');
const { spawn } = require('child_process');
const admin = require('firebase-admin');
const fs = require('fs');
const path = require('path');

// NOTE: Replace with actual path to your service account key file
const serviceAccount = require('./mentorbookingproject-firebase-adminsdk-fbsvc-f8160d02d1.json');

// Initialize Firebase Admin
console.log("Initializing Firebase Admin with provided service account key.");
admin.initializeApp({
    credential: admin.credential.cert(serviceAccount),
    storageBucket: "mentorbookingproject.firebasestorage.app" // TODO: Verify if this is your exact Storage Bucket URL format
});

const RABBITMQ_URL = 'amqp://localhost';
const QUEUE_NAME = 'record-meeting-queue';

async function startWorker() {
    try {
        console.log(`Connecting to RabbitMQ at ${RABBITMQ_URL}...`);
        const conn = await amqp.connect(RABBITMQ_URL);
        const ch = await conn.createChannel();

        // Assert the queues and exchanges (match MassTransit)
        await ch.assertExchange('Shared.Contracts.Events:RecordMeetingCommand', 'fanout', { durable: true });
        await ch.assertQueue(QUEUE_NAME, { durable: true });
        await ch.bindQueue(QUEUE_NAME, 'Shared.Contracts.Events:RecordMeetingCommand', '');

        console.log(`[*] Waiting for messages in ${QUEUE_NAME}. To exit press CTRL+C`);

        ch.consume(QUEUE_NAME, async (msg) => {
            if (msg !== null) {
                try {
                    // MassTransit wraps the actual message in a JSON envelope.
                    const payload = JSON.parse(msg.content.toString());
                    const data = payload.message || payload; // Unwrap if using MassTransit envelope

                    console.log(`[x] Received record request for MeetingId: ${data.meetingId}`);

                    await processRecording(data);

                    ch.ack(msg);
                } catch (err) {
                    console.error('Error processing message:', err);
                    ch.nack(msg, false, true); // Requeue on error
                }
            }
        });
    } catch (err) {
        console.error("Failed to start worker:", err);
        // Retry connection in 5 seconds
        setTimeout(startWorker, 5000);
    }
}

async function processRecording(data) {
    const { meetingId, joinUrl, durationMinutes } = data;
    const durationMs = (durationMinutes || 60) * 60 * 1000;
    const outputPath = path.join(__dirname, `${meetingId}.mp4`);

    console.log(`Starting Recorder for ${joinUrl}...`);

    // 1. Launch Puppeteer
    const browser = await puppeteer.launch({
        headless: false, // Must be false for screen recording. In Docker, run via Xvfb.
        args: [
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--use-fake-ui-for-media-stream",
            "--window-size=1920,1080",
            "--disable-notifications"
        ],
        defaultViewport: {
            width: 1920,
            height: 1080
        }
    });

    const page = await browser.newPage();

    // Attempt to override permissions to allow mic/camera without prompts
    const context = browser.defaultBrowserContext();
    await context.overridePermissions('https://meet.google.com', ['camera', 'microphone', 'notifications']);

    try {
        await page.goto(joinUrl, { waitUntil: 'networkidle2' });
        console.log('Navigated to Meet link.');

        // Enter name if asked
        try {
            await page.waitForSelector('input[type="text"]', { timeout: 10000 });
            await page.type('input[type="text"]', 'MentorBooking System Recorder');
            console.log('Entered bot name.');
        } catch (e) {
            console.log('No name input field found, skipping...');
        }

        // Mute mic and cam using keyboard shortcuts (Ctrl+D, Ctrl+E)
        await page.keyboard.down('Control');
        await page.keyboard.press('d'); // Mute Mic
        await page.keyboard.press('e'); // Turn off Cam
        await page.keyboard.up('Control');
        console.log('Muted mic and camera.');

        await new Promise(resolve => setTimeout(resolve, 2000));

        // Click Join / Ask to join
        const joined = await page.evaluate(() => {
            const buttons = Array.from(document.querySelectorAll('button'));
            const joinBtn = buttons.find(btn => btn.innerText.includes('Ask to join') || btn.innerText.includes('Join now'));
            if (joinBtn) {
                joinBtn.click();
                return true;
            }
            return false;
        });

        if (joined) {
            console.log('Clicked Join button. Waiting to be admitted...');
        } else {
            console.log('Could not find Join button. Meeting might be full or inactive.');
        }

        // 2. Start FFmpeg
        console.log(`Starting FFmpeg recording to ${outputPath}...`);

        // Note: these FFmpeg args are for Linux with Xvfb and PulseAudio
        // On Windows or Mac, these args need to be changed (e.g. gdigrab for Windows)
        // Here we use a generic placeholder command for demonstration logic
        const ffmpegArgs = process.platform === 'win32'
            ? ['-f', 'gdigrab', '-framerate', '30', '-i', 'desktop', outputPath]
            : ['-video_size', '1920x1080', '-framerate', '30', '-f', 'x11grab', '-i', ':99.0', '-f', 'pulse', '-i', 'default', outputPath];

        const ffmpeg = spawn('ffmpeg', ffmpegArgs);

        ffmpeg.stderr.on('data', (data) => {
            // Uncomment to debug FFmpeg output if needed
            // console.log(`ffmpeg err: ${data}`);
        });

        console.log(`Recording will run for ${durationMinutes} minutes...`);

        // Wait for the duration
        await new Promise(resolve => setTimeout(resolve, durationMs));

        console.log(`Duration reached. Stopping recording...`);
        ffmpeg.kill('SIGINT');

        // Wait a bit for FFmpeg to finalize the mp4 file
        await new Promise(resolve => setTimeout(resolve, 3000));

        // 3. Upload to Firebase
        console.log(`Uploading ${outputPath} to Firebase Storage...`);
        let storageUrl = `https://mock-firebase-url.com/${meetingId}.mp4`; // Mock URL

        const bucket = admin.storage().bucket();
        const destination = `recordings/${meetingId}.mp4`;

        await bucket.upload(outputPath, {
            destination: destination,
            metadata: { contentType: 'video/mp4' }
        });

        const file = bucket.file(destination);
        await file.makePublic();
        storageUrl = file.publicUrl();

        console.log(`Uploaded! URL: ${storageUrl}`);

        // 4. Publish MeetingRecordingCompletedEvent back to RabbitMQ
        await publishCompletedEvent({
            MeetingId: meetingId,
            StorageUrl: storageUrl,
            DurationSeconds: durationMinutes * 60,
            SizeBytes: 10485760 // Mock size
        });

        // 5. Cleanup
        await browser.close();
        if (fs.existsSync(outputPath)) {
            fs.unlinkSync(outputPath);
        }

        console.log(`Process for Meeting ${meetingId} completed successfully.`);

    } catch (error) {
        console.error('Error during recording process:', error);
        await browser.close().catch(e => { });
        throw error;
    }
}

async function publishCompletedEvent(payload) {
    try {
        const conn = await amqp.connect(RABBITMQ_URL);
        const ch = await conn.createChannel();

        const exchangeName = 'Shared.Contracts.Events:MeetingRecordingCompletedEvent';
        await ch.assertExchange(exchangeName, 'fanout', { durable: true });

        // Wrap payload in MassTransit envelope format
        const mtMessage = {
            messageId: require('crypto').randomUUID(),
            messageType: ["urn:message:Shared.Contracts.Events:MeetingRecordingCompletedEvent"],
            message: payload
        };

        const msgBuffer = Buffer.from(JSON.stringify(mtMessage));
        ch.publish(exchangeName, '', msgBuffer);
        console.log(`[x] Published MeetingRecordingCompletedEvent for ${payload.MeetingId}`);

        setTimeout(() => {
            conn.close();
        }, 500);
    } catch (err) {
        console.error('Failed to publish completion event:', err);
    }
}

// Start the worker
startWorker();
