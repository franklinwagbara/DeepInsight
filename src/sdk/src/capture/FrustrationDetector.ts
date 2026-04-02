import { BaseEvent, EventType } from "../types/events";
import { EventEmitter } from "./MouseTracker";

interface ClickRecord {
  x: number;
  y: number;
  time: number;
  target: Element;
}

const RAGE_CLICK_THRESHOLD = 3;
const RAGE_CLICK_WINDOW_MS = 500;
const RAGE_CLICK_RADIUS_PX = 30;
const DEAD_CLICK_TIMEOUT_MS = 1000;

export class FrustrationDetector {
  private emit: EventEmitter;
  private sessionId: () => string;
  private recentClicks: ClickRecord[] = [];
  private clickHandler: ((e: MouseEvent) => void) | null = null;

  constructor(emit: EventEmitter, sessionId: () => string) {
    this.emit = emit;
    this.sessionId = sessionId;
  }

  start(): void {
    this.clickHandler = (e: MouseEvent) => this.onClickCapture(e);
    document.addEventListener("click", this.clickHandler, { capture: true });
  }

  stop(): void {
    if (this.clickHandler) {
      document.removeEventListener("click", this.clickHandler, {
        capture: true,
      });
    }
  }

  private onClickCapture(e: MouseEvent): void {
    const target = e.target as Element;
    if (!target) return;

    const now = Date.now();
    const record: ClickRecord = {
      x: e.clientX,
      y: e.clientY,
      time: now,
      target,
    };

    this.recentClicks.push(record);
    this.pruneOldClicks(now);

    // Rage click detection
    this.detectRageClick(record);

    // Dead click detection
    this.detectDeadClick(record);
  }

  private detectRageClick(current: ClickRecord): void {
    const nearbyClicks = this.recentClicks.filter((click) => {
      const dx = Math.abs(click.x - current.x);
      const dy = Math.abs(click.y - current.y);
      const dist = Math.sqrt(dx * dx + dy * dy);
      return dist <= RAGE_CLICK_RADIUS_PX;
    });

    if (nearbyClicks.length >= RAGE_CLICK_THRESHOLD) {
      this.emit({
        sessionId: this.sessionId(),
        timestamp: Date.now(),
        type: EventType.RageClick,
        pageUrl: location.href,
        data: {
          x: current.x,
          y: current.y,
          xPct: current.x / window.innerWidth,
          yPct: current.y / window.innerHeight,
          clickCount: nearbyClicks.length,
          element: this.getDescriptor(current.target),
        },
      });
      // Clear to avoid duplicate detection
      this.recentClicks = [];
    }
  }

  private detectDeadClick(record: ClickRecord): void {
    const target = record.target;
    const isInteractive =
      target instanceof HTMLButtonElement ||
      target instanceof HTMLAnchorElement ||
      target instanceof HTMLInputElement ||
      target.getAttribute("role") === "button" ||
      target.hasAttribute("onclick") ||
      window.getComputedStyle(target).cursor === "pointer";

    if (!isInteractive) return;

    // Watch for DOM changes after click
    let mutationOccurred = false;
    const observer = new MutationObserver(() => {
      mutationOccurred = true;
      observer.disconnect();
    });

    observer.observe(document.body, {
      childList: true,
      subtree: true,
      attributes: true,
    });

    setTimeout(() => {
      observer.disconnect();
      if (!mutationOccurred) {
        this.emit({
          sessionId: this.sessionId(),
          timestamp: Date.now(),
          type: EventType.DeadClick,
          pageUrl: location.href,
          data: {
            x: record.x,
            y: record.y,
            xPct: record.x / window.innerWidth,
            yPct: record.y / window.innerHeight,
            element: this.getDescriptor(target),
          },
        });
      }
    }, DEAD_CLICK_TIMEOUT_MS);
  }

  private pruneOldClicks(now: number): void {
    this.recentClicks = this.recentClicks.filter(
      (c) => now - c.time <= RAGE_CLICK_WINDOW_MS,
    );
  }

  private getDescriptor(el: Element): string {
    const tag = el.tagName.toLowerCase();
    const id = el.id ? `#${el.id}` : "";
    const classes =
      el.className && typeof el.className === "string"
        ? "." + el.className.trim().split(/\s+/).slice(0, 2).join(".")
        : "";
    return `${tag}${id}${classes}`;
  }
}
