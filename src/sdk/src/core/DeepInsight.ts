import { DeepInsightConfig, BaseEvent } from "../types/events";
import { SessionManager } from "./SessionManager";
import { EventBuffer } from "./EventBuffer";
import { Transport } from "./Transport";
import { MouseTracker } from "../capture/MouseTracker";
import { ScrollTracker } from "../capture/ScrollTracker";
import { InputTracker } from "../capture/InputTracker";
import { DomObserver } from "../capture/DomObserver";
import { NavigationTracker } from "../capture/NavigationTracker";
import { FrustrationDetector } from "../capture/FrustrationDetector";

export class DeepInsightClient {
  private config: DeepInsightConfig | null = null;
  private sessionManager: SessionManager | null = null;
  private eventBuffer: EventBuffer | null = null;
  private transport: Transport | null = null;

  private mouseTracker: MouseTracker | null = null;
  private scrollTracker: ScrollTracker | null = null;
  private inputTracker: InputTracker | null = null;
  private domObserver: DomObserver | null = null;
  private navigationTracker: NavigationTracker | null = null;
  private frustrationDetector: FrustrationDetector | null = null;

  private initialized = false;
  private paused = false;
  private userId: string | null = null;

  init(config: DeepInsightConfig): void {
    if (this.initialized) {
      console.warn("[DeepInsight] Already initialized");
      return;
    }

    if (!config.projectId || !config.endpoint) {
      console.error("[DeepInsight] projectId and endpoint are required");
      return;
    }

    // Respect Do Not Track
    if (config.respectDoNotTrack && navigator.doNotTrack === "1") {
      return;
    }

    this.config = {
      maskAllInputs: false,
      sampleRate: 1.0,
      batchSize: 50,
      batchInterval: 2000,
      throttleMouseMove: 50,
      enableDomCapture: true,
      enableMouseTracking: true,
      enableScrollTracking: true,
      enableInputTracking: true,
      respectDoNotTrack: false,
      ...config,
    };

    this.sessionManager = new SessionManager(this.config);

    // Check sampling
    if (!this.sessionManager.getSampleDecision()) {
      return;
    }

    this.transport = new Transport(this.config);
    this.transport.setSessionId(this.sessionManager.getSessionId());

    this.eventBuffer = new EventBuffer(this.config);
    this.eventBuffer.setFlushHandler((events) => {
      this.transport!.send(events);
    });

    this.startTrackers();
    this.initialized = true;

    // Flush on page unload
    window.addEventListener("beforeunload", () => {
      this.eventBuffer?.flush();
    });
  }

  track(eventName: string, properties?: Record<string, unknown>): void {
    if (!this.initialized || this.paused) return;

    this.pushEvent({
      sessionId: this.sessionManager!.getSessionId(),
      timestamp: Date.now(),
      type: "custom",
      pageUrl: location.href,
      data: {
        name: eventName,
        properties: properties || {},
        userId: this.userId,
      },
    });
  }

  identify(userId: string): void {
    this.userId = userId;
    if (!this.initialized) return;

    this.pushEvent({
      sessionId: this.sessionManager!.getSessionId(),
      timestamp: Date.now(),
      type: "identify",
      pageUrl: location.href,
      data: { userId },
    });
  }

  pause(): void {
    this.paused = true;
  }

  resume(): void {
    this.paused = false;
  }

  destroy(): void {
    this.mouseTracker?.stop();
    this.scrollTracker?.stop();
    this.inputTracker?.stop();
    this.domObserver?.stop();
    this.navigationTracker?.stop();
    this.frustrationDetector?.stop();
    this.eventBuffer?.destroy();
    this.initialized = false;
  }

  getSessionId(): string | null {
    return this.sessionManager?.getSessionId() ?? null;
  }

  private startTrackers(): void {
    const emit = (event: BaseEvent) => this.pushEvent(event);
    const getSessionId = () => this.sessionManager!.getSessionId();

    if (this.config!.enableMouseTracking) {
      this.mouseTracker = new MouseTracker(
        emit,
        getSessionId,
        this.config!.throttleMouseMove,
      );
      this.mouseTracker.start();
    }

    if (this.config!.enableScrollTracking) {
      this.scrollTracker = new ScrollTracker(emit, getSessionId);
      this.scrollTracker.start();
    }

    if (this.config!.enableInputTracking) {
      this.inputTracker = new InputTracker(
        emit,
        getSessionId,
        this.config!.maskAllInputs!,
      );
      this.inputTracker.start();
    }

    if (this.config!.enableDomCapture) {
      this.domObserver = new DomObserver(
        emit,
        getSessionId,
        this.config!.maskAllInputs!,
      );
      this.domObserver.start();
    }

    this.navigationTracker = new NavigationTracker(emit, getSessionId);
    this.navigationTracker.start();

    this.frustrationDetector = new FrustrationDetector(emit, getSessionId);
    this.frustrationDetector.start();
  }

  private pushEvent(event: BaseEvent): void {
    if (this.paused) return;
    this.sessionManager?.touch();
    this.eventBuffer?.push(event);
  }
}
