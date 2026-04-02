import { BaseEvent, DeepInsightConfig } from "../types/events";

export class EventBuffer {
  private buffer: BaseEvent[] = [];
  private flushTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly batchSize: number;
  private readonly batchInterval: number;
  private onFlush: ((events: BaseEvent[]) => void) | null = null;

  constructor(config: DeepInsightConfig) {
    this.batchSize = config.batchSize ?? 50;
    this.batchInterval = config.batchInterval ?? 2000;
  }

  setFlushHandler(handler: (events: BaseEvent[]) => void): void {
    this.onFlush = handler;
  }

  push(event: BaseEvent): void {
    this.buffer.push(event);

    if (this.buffer.length >= this.batchSize) {
      this.flush();
    } else if (!this.flushTimer) {
      this.flushTimer = setTimeout(() => this.flush(), this.batchInterval);
    }
  }

  flush(): void {
    if (this.flushTimer) {
      clearTimeout(this.flushTimer);
      this.flushTimer = null;
    }

    if (this.buffer.length === 0) return;

    const events = this.buffer.splice(0);
    if (this.onFlush) {
      this.onFlush(events);
    }
  }

  size(): number {
    return this.buffer.length;
  }

  destroy(): void {
    this.flush();
    if (this.flushTimer) {
      clearTimeout(this.flushTimer);
      this.flushTimer = null;
    }
  }
}
