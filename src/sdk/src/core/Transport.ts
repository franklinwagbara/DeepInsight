import { BaseEvent, EventBatch, DeepInsightConfig } from "../types/events";

const SDK_VERSION = "1.0.0";
const MAX_RETRIES = 3;
const RETRY_BASE_DELAY = 1000;

export class Transport {
  private config: DeepInsightConfig;
  private sessionId: string = "";
  private queue: BaseEvent[][] = [];
  private sending = false;

  constructor(config: DeepInsightConfig) {
    this.config = config;
    this.setupBeaconFallback();
  }

  setSessionId(sessionId: string): void {
    this.sessionId = sessionId;
  }

  async send(events: BaseEvent[]): Promise<void> {
    this.queue.push(events);
    if (!this.sending) {
      await this.processQueue();
    }
  }

  private async processQueue(): Promise<void> {
    this.sending = true;
    while (this.queue.length > 0) {
      const events = this.queue.shift()!;
      await this.sendWithRetry(events, 0);
    }
    this.sending = false;
  }

  private async sendWithRetry(
    events: BaseEvent[],
    attempt: number,
  ): Promise<void> {
    const batch: EventBatch = {
      projectId: this.config.projectId,
      sessionId: this.sessionId,
      events,
      sentAt: Date.now(),
      sdkVersion: SDK_VERSION,
    };

    const payload = JSON.stringify(batch);

    try {
      const compressed = await this.compress(payload);
      const endpoint = `${this.config.endpoint}/api/v1/ingest`;

      const response = await fetch(endpoint, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Content-Encoding": compressed ? "gzip" : "identity",
          "X-DI-Project": this.config.projectId,
        },
        body: compressed || payload,
        keepalive: true,
      });

      if (!response.ok && attempt < MAX_RETRIES) {
        const delay = RETRY_BASE_DELAY * Math.pow(2, attempt);
        await this.sleep(delay);
        return this.sendWithRetry(events, attempt + 1);
      }
    } catch {
      if (attempt < MAX_RETRIES) {
        const delay = RETRY_BASE_DELAY * Math.pow(2, attempt);
        await this.sleep(delay);
        return this.sendWithRetry(events, attempt + 1);
      }
      // All retries exhausted — drop batch silently to not affect host page
    }
  }

  private async compress(data: string): Promise<Blob | null> {
    if (typeof CompressionStream === "undefined") return null;

    try {
      const stream = new Blob([data]).stream();
      const compressedStream = stream.pipeThrough(
        new CompressionStream("gzip"),
      );
      return new Response(compressedStream).blob();
    } catch {
      return null;
    }
  }

  sendBeacon(events: BaseEvent[]): void {
    const batch: EventBatch = {
      projectId: this.config.projectId,
      sessionId: this.sessionId,
      events,
      sentAt: Date.now(),
      sdkVersion: SDK_VERSION,
    };

    const payload = JSON.stringify(batch);
    const endpoint = `${this.config.endpoint}/api/v1/ingest/beacon`;

    try {
      const blob = new Blob([payload], { type: "application/json" });
      navigator.sendBeacon(endpoint, blob);
    } catch {
      // Best effort — page is unloading
    }
  }

  private setupBeaconFallback(): void {
    if (typeof window === "undefined") return;
    window.addEventListener("visibilitychange", () => {
      if (document.visibilityState === "hidden" && this.queue.length > 0) {
        const allEvents = this.queue.splice(0).flat();
        if (allEvents.length > 0) {
          this.sendBeacon(allEvents);
        }
      }
    });
  }

  private sleep(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}
