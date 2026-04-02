import { BaseEvent, DeepInsightConfig } from '../types/events';
export declare class Transport {
    private config;
    private sessionId;
    private queue;
    private sending;
    constructor(config: DeepInsightConfig);
    setSessionId(sessionId: string): void;
    send(events: BaseEvent[]): Promise<void>;
    private processQueue;
    private sendWithRetry;
    private compress;
    sendBeacon(events: BaseEvent[]): void;
    private setupBeaconFallback;
    private sleep;
}
