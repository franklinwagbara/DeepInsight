import { DeepInsightConfig } from '../types/events';
export declare class DeepInsightClient {
    private config;
    private sessionManager;
    private eventBuffer;
    private transport;
    private mouseTracker;
    private scrollTracker;
    private inputTracker;
    private domObserver;
    private navigationTracker;
    private frustrationDetector;
    private initialized;
    private paused;
    private userId;
    init(config: DeepInsightConfig): void;
    track(eventName: string, properties?: Record<string, unknown>): void;
    identify(userId: string): void;
    pause(): void;
    resume(): void;
    destroy(): void;
    getSessionId(): string | null;
    private startTrackers;
    private pushEvent;
}
