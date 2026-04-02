import { BaseEvent, EventType } from "../types/events";
import { EventEmitter } from "./MouseTracker";

export class NavigationTracker {
  private emit: EventEmitter;
  private sessionId: () => string;
  private lastUrl: string = "";
  private popstateHandler: (() => void) | null = null;
  private originalPushState: typeof history.pushState | null = null;
  private originalReplaceState: typeof history.replaceState | null = null;

  constructor(emit: EventEmitter, sessionId: () => string) {
    this.emit = emit;
    this.sessionId = sessionId;
    this.lastUrl = location.href;
  }

  start(): void {
    // Track initial page view
    this.emitPageView();

    // Intercept pushState / replaceState
    this.originalPushState = history.pushState.bind(history);
    this.originalReplaceState = history.replaceState.bind(history);

    history.pushState = (...args: Parameters<typeof history.pushState>) => {
      this.originalPushState!(...args);
      this.onNavigation();
    };

    history.replaceState = (
      ...args: Parameters<typeof history.replaceState>
    ) => {
      this.originalReplaceState!(...args);
      this.onNavigation();
    };

    // Track back/forward
    this.popstateHandler = () => this.onNavigation();
    window.addEventListener("popstate", this.popstateHandler);
  }

  stop(): void {
    if (this.originalPushState) {
      history.pushState = this.originalPushState;
    }
    if (this.originalReplaceState) {
      history.replaceState = this.originalReplaceState;
    }
    if (this.popstateHandler) {
      window.removeEventListener("popstate", this.popstateHandler);
    }
  }

  private onNavigation(): void {
    const newUrl = location.href;
    if (newUrl === this.lastUrl) return;

    const from = this.lastUrl;
    this.lastUrl = newUrl;

    this.emit({
      sessionId: this.sessionId(),
      timestamp: Date.now(),
      type: EventType.Navigation,
      pageUrl: newUrl,
      data: { from, to: newUrl },
    });

    this.emitPageView();
  }

  private emitPageView(): void {
    this.emit({
      sessionId: this.sessionId(),
      timestamp: Date.now(),
      type: EventType.PageView,
      pageUrl: location.href,
      data: {
        title: document.title,
        referrer: document.referrer,
        href: location.href,
      },
    });
  }
}
