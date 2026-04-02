import { BaseEvent, EventType } from "../types/events";
import { EventEmitter } from "./MouseTracker";

export class ScrollTracker {
  private emit: EventEmitter;
  private sessionId: () => string;
  private throttleMs = 100;
  private lastEmitTime = 0;
  private maxDepthPct = 0;
  private handler: (() => void) | null = null;

  constructor(emit: EventEmitter, sessionId: () => string) {
    this.emit = emit;
    this.sessionId = sessionId;
  }

  start(): void {
    this.handler = () => this.onScroll();
    window.addEventListener("scroll", this.handler, {
      passive: true,
      capture: true,
    });
  }

  stop(): void {
    if (this.handler) {
      window.removeEventListener("scroll", this.handler, { capture: true });
    }
  }

  private onScroll(): void {
    const now = Date.now();
    if (now - this.lastEmitTime < this.throttleMs) return;
    this.lastEmitTime = now;

    const scrollTop = window.scrollY || document.documentElement.scrollTop;
    const scrollHeight = document.documentElement.scrollHeight;
    const viewportHeight = window.innerHeight;
    const depthPct = Math.min(
      ((scrollTop + viewportHeight) / scrollHeight) * 100,
      100,
    );

    if (depthPct > this.maxDepthPct) {
      this.maxDepthPct = depthPct;
    }

    this.emit({
      sessionId: this.sessionId(),
      timestamp: now,
      type: EventType.Scroll,
      pageUrl: location.href,
      data: {
        scrollTop,
        scrollHeight,
        viewportHeight,
        depthPct: Math.round(depthPct * 100) / 100,
      },
    });
  }

  getMaxDepth(): number {
    return this.maxDepthPct;
  }
}
