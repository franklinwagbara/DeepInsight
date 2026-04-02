import { BaseEvent, EventType, ClickEventData } from "../types/events";

export type EventEmitter = (event: BaseEvent) => void;

export class MouseTracker {
  private emit: EventEmitter;
  private sessionId: () => string;
  private throttleMs: number;
  private lastMoveTime = 0;
  private lastX = -1;
  private lastY = -1;
  private moveHandler: ((e: MouseEvent) => void) | null = null;
  private clickHandler: ((e: MouseEvent) => void) | null = null;

  constructor(
    emit: EventEmitter,
    sessionId: () => string,
    throttleMs: number = 50,
  ) {
    this.emit = emit;
    this.sessionId = sessionId;
    this.throttleMs = throttleMs;
  }

  start(): void {
    this.moveHandler = (e: MouseEvent) => this.onMouseMove(e);
    this.clickHandler = (e: MouseEvent) => this.onClick(e);

    document.addEventListener("mousemove", this.moveHandler, {
      passive: true,
      capture: true,
    });
    document.addEventListener("click", this.clickHandler, { capture: true });
  }

  stop(): void {
    if (this.moveHandler) {
      document.removeEventListener("mousemove", this.moveHandler, {
        capture: true,
      });
    }
    if (this.clickHandler) {
      document.removeEventListener("click", this.clickHandler, {
        capture: true,
      });
    }
  }

  private onMouseMove(e: MouseEvent): void {
    const now = Date.now();
    if (now - this.lastMoveTime < this.throttleMs) return;

    const dx = Math.abs(e.clientX - this.lastX);
    const dy = Math.abs(e.clientY - this.lastY);
    if (dx < 3 && dy < 3) return;

    this.lastMoveTime = now;
    this.lastX = e.clientX;
    this.lastY = e.clientY;

    this.emit({
      sessionId: this.sessionId(),
      timestamp: now,
      type: EventType.MouseMove,
      pageUrl: location.href,
      data: {
        x: e.clientX,
        y: e.clientY,
        xPct: e.clientX / window.innerWidth,
        yPct: e.clientY / window.innerHeight,
      },
    });
  }

  private onClick(e: MouseEvent): void {
    const target = e.target as Element;
    if (!target) return;

    const data: ClickEventData = {
      x: e.clientX,
      y: e.clientY,
      xPct: e.clientX / window.innerWidth,
      yPct: e.clientY / window.innerHeight,
      element: this.getElementDescriptor(target),
      selector: this.getSelector(target),
      text: this.getElementText(target),
    };

    this.emit({
      sessionId: this.sessionId(),
      timestamp: Date.now(),
      type: EventType.Click,
      pageUrl: location.href,
      data: data as unknown as Record<string, unknown>,
    });
  }

  private getElementDescriptor(el: Element): string {
    const tag = el.tagName.toLowerCase();
    const id = el.id ? `#${el.id}` : "";
    const classes =
      el.className && typeof el.className === "string"
        ? "." + el.className.trim().split(/\s+/).slice(0, 3).join(".")
        : "";
    return `${tag}${id}${classes}`;
  }

  private getSelector(el: Element): string {
    if (el.id) return `#${el.id}`;

    const parts: string[] = [];
    let current: Element | null = el;
    let depth = 0;

    while (current && current !== document.body && depth < 5) {
      let selector = current.tagName.toLowerCase();
      if (current.id) {
        selector = `#${current.id}`;
        parts.unshift(selector);
        break;
      }
      const parent = current.parentElement;
      if (parent) {
        const siblings = Array.from(parent.children).filter(
          (c) => c.tagName === current!.tagName,
        );
        if (siblings.length > 1) {
          const index = siblings.indexOf(current) + 1;
          selector += `:nth-of-type(${index})`;
        }
      }
      parts.unshift(selector);
      current = parent;
      depth++;
    }

    return parts.join(" > ");
  }

  private getElementText(el: Element): string | undefined {
    const text = el.textContent?.trim();
    if (!text) return undefined;
    return text.substring(0, 50);
  }
}
